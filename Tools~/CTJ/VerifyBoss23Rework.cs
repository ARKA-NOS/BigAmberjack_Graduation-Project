using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Testing.Enemy;
using _Scripts.UI.Enemy;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;
using Random = UnityEngine.Random;

public static class VerifyBoss23Rework
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string Path = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        var skills = root.GetComponentInChildren<EnemySkillModule>(true).Skills;
        Check(skills.Count == 5 && skills.All(e => e.Skill != null && e.Skill.SkillSo != null && !e.UseForAutomaticSelection) &&
            skills.Select(e => e.Skill.SkillSo).Distinct().Count() == 5, "Five unique specified skills required");
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script: " + node.name);
        Check(root.GetComponentsInChildren<DroneSalvoSkill>(true).Length == 1 && root.GetComponentInChildren<EnemyAfterImageModule>(true) != null,
            "Obsolete strong salvo or missing afterimage module");
        var data = new SerializedObject(root.GetComponent<ProductionControlDrone>());
        Check(data.FindProperty("lastStandHealthRatio").floatValue == 0.35f, "Wrong Last Stand threshold");
        var bomb = new SerializedObject(root.GetComponentInChildren<DroneBombSkill>(true));
        Check(bomb.FindProperty("warningPrefab").objectReferenceValue != null && bomb.FindProperty("telegraphPrefab").objectReferenceValue == null,
            "Target warning missing or obsolete circle warning remains");
        return new { skills = skills.Select(e => e.Skill.name).ToArray(), dependencies = AssetDatabase.GetDependencies(Path).Length,
            lastStandAt = 0.35f, airHeight = data.FindProperty("airAttackAltitude").floatValue,
            active = root.activeSelf, scale = root.transform.localScale.ToString() };
    }

    public static Task<object> Run() => RunInternal(false);

    public static Task<object> RunEntryOnly() => RunInternal(true);

    private static async Task<object> RunInternal(bool entryOnly)
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
        var originals = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(o => o.activeSelf && o.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var original in originals) original.SetActive(false);
        bool background = Application.runInBackground;
        Application.runInBackground = true;
        var randomState = Random.state;
        Random.InitState(231135);
        var results = new List<object>();

        using (var rig = new Rig())
        {
            try
            {
                for (int mode = 0; !entryOnly && mode < 2; mode++)
                {
                    bool enhanced = mode == 1;
                    await rig.NewBoss(enhanced);
                    results.Add(await Charge(rig, enhanced));
                    results.Add(await Barrage(rig, enhanced, false));
                    if (!enhanced) results.Add(await Barrage(rig, false, true));
                    results.Add(await Air(rig, enhanced));
                    results.Add(await Laser(rig, enhanced));
                    results.Add(await Bomb(rig, enhanced));
                }

                await rig.NewBoss(false);
                if (!entryOnly) results.Add(RandomChoices(rig));
                results.Add(await LastStandEntry(rig));
                return new { results, preview = "Tools~/CTJ/Boss23TargetBombPreview.png" };
            }
            catch (Exception exception)
            {
                var health = rig.Root.GetComponentInChildren<NewHealthModule>();
                throw new Exception(exception.Message + " | HP=" + health.CurrentHealth.ToString("R") +
                    ", threshold=" + (health.MaxHealth * 0.35f).ToString("R") + ", phase=" + rig.Boss.IsLastStand +
                    ", state=" + Read(rig.Boss, "_state"), exception);
            }
            finally
            {
                Random.state = randomState;
                Application.runInBackground = background;
                foreach (var original in originals) if (original != null) original.SetActive(true);
            }
        }
    }

    private static async Task<object> Charge(Rig r, bool enhanced)
    {
        r.Prepare(false, 1.5f);
        int hits = r.Probe.ReceivedHitCount;
        Check(r.Skills.TryUseSpecifiedSkill(r.Charge), "Charge did not start");
        int ghosts = 0;
        await Until(() =>
        {
            ghosts = Mathf.Max(ghosts, Object.FindObjectsByType<AfterImageGhost>(FindObjectsSortMode.None).Length);
            return !r.Charge.IsUsing;
        }, 3f, "Charge stalled");
        var starts = r.Events.Where(e => e.skill == r.Charge).Select(e => e.position.x).ToArray();
        Check(starts.Length == (enhanced ? 2 : 1) && ghosts > 1, "Wrong dash count or no pooled ghosts");
        float last = r.Root.transform.position.x;
        Check(Mathf.Abs(starts[0]) < 0.01f && Mathf.Abs((starts.Length > 1 ? starts[1] : last) - 3f) < 0.03f &&
            (!enhanced || Mathf.Abs(last) < 0.03f), "Dash did not travel three units per leg");
        Check(r.Probe.ReceivedHitCount - hits == (enhanced ? 2 : 1), "Charge contact damage count changed");
        Check(Mathf.Abs(ReadFloat(r.Flight, "_peakSpeed") - (enhanced ? 12f : 8f)) < 0.01f, "Wrong dash speed multiplier");
        return new { pattern = "Charge", enhanced, dashes = starts.Length, lastX = last, ghostPeak = ghosts, hits = r.Probe.ReceivedHitCount - hits };
    }

    private static async Task<object> Barrage(Rig r, bool enhanced, bool wall)
    {
        r.Prepare(false, 4f);
        GameObject blocker = null;
        if (wall)
        {
            Bounds body = (Bounds)typeof(ProductionControlDrone).GetProperty("ContactBounds", Private).GetValue(r.Boss);
            blocker = new GameObject("Retreat blocked test") { layer = LayerMask.NameToLayer("Ground") };
            blocker.transform.position = new Vector3(-body.extents.x * 0.95f - 0.11f, 1.5f, 0f);
            blocker.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 3f);
            Physics2D.SyncTransforms();
        }

        try
        {
            Check(r.Skills.TryUseSpecifiedSkill(r.Barrage), "Barrage did not start");
            await Until(() => !r.Barrage.IsUsing, 5f, "Barrage stalled");
            Check(r.Barrage.FiredVolleys == (enhanced ? 12 : 7) && r.Barrage.ProjectilesFired == (enhanced ? 36 : 7), "Wrong barrage count");
            var events = r.Events.Where(e => e.skill == r.Barrage).ToArray();
            Check(Mathf.Abs(events.Last().time - events.First().time - 2.5f) < 0.09f, "Barrage firing duration changed");
            Check(r.Shots.Count == (enhanced ? 36 : 7), "Spawned projectile count mismatch");
            Check(r.Shots.All(s => Mathf.Abs(s.origin.y - s.muzzleY) <= 0.301f) &&
                r.Shots.Max(s => s.origin.y - s.muzzleY) - r.Shots.Min(s => s.origin.y - s.muzzleY) > 0.1f,
                "Random vertical firing offset missing");
            Check(enhanced ? r.Shots.Select(s => Mathf.RoundToInt(Mathf.Atan2(s.direction.y, s.direction.x) * Mathf.Rad2Deg)).Distinct().Count() == 3 :
                r.Shots.All(s => Mathf.Abs(s.direction.y) < 0.001f), "Wrong forward shot directions");
            float retreat = -r.Root.transform.position.x;
            Check(wall ? Mathf.Abs(retreat) < 0.02f : Mathf.Abs(retreat - 3f) < 0.04f, "Retreat distance or blocked-fire rule failed");
            return new { pattern = "Barrage", enhanced, wall, volleys = r.Barrage.FiredVolleys, shots = r.Barrage.ProjectilesFired, retreat };
        }
        finally { if (blocker != null) Object.DestroyImmediate(blocker); }
    }

    private static async Task<object> Air(Rig r, bool enhanced)
    {
        r.Prepare(false, 4f);
        Check(r.Skills.TryUseSpecifiedSkill(r.Air), "Air transition did not start");
        await Until(() => !r.Air.IsUsing, 2.5f, "Ascent stalled");
        Check(r.Air.DidArrive && Mathf.Abs(r.Root.transform.position.x) < 0.02f && Mathf.Abs(r.Root.transform.position.y - 6.5f) < 0.02f,
            "Air transition did not reach unified position");
        Check(r.Shots.Count == (enhanced ? 5 : 0), "Wrong ascent shotgun count");
        if (enhanced) Check(r.Shots.Select(s => Mathf.RoundToInt(Mathf.Atan2(s.direction.y, s.direction.x) * Mathf.Rad2Deg)).Distinct().Count() == 5,
            "Shotgun directions are not distinct");
        return new { pattern = "AirTransition", enhanced, height = r.Root.transform.position.y, shots = r.Shots.Count };
    }

    private static async Task<object> Laser(Rig r, bool enhanced)
    {
        r.Prepare(true, 0f);
        int hits = r.Probe.ReceivedHitCount;
        Check(r.Skills.TryUseSpecifiedSkill(r.Laser), "Laser did not start");
        int legs = (int)Read(r.Laser, "_sweepCount");
        float speed = ReadFloat(r.Laser, "_startSpeed");
        Check(legs == (enhanced ? 2 : 1) && Mathf.Abs(speed - (enhanced ? 750f : 600f)) < 0.01f, "Laser enhancement mismatch");
        var shake = r.Laser.GetComponent<BossLaserShake>();
        bool continuousShake = false;
        await Until(() =>
        {
            if (r.Laser.DidFire)
            {
                float elapsed = Time.time - ReadFloat(r.Laser, "_beamStartedAt");
                if (elapsed < ReadFloat(r.Laser, "_totalSweepDuration"))
                {
                    Check(shake.IsShaking && shake.Strength == 1f, "Laser shake stopped during a sweep");
                    continuousShake = true;
                }
            }
            return !r.Laser.IsUsing;
        }, 2.5f, "Laser stalled");
        Check(continuousShake && !shake.IsShaking, "Laser shake did not start or clean up");
        Check(r.Probe.ReceivedHitCount - hits == (enhanced ? 2 : 1), "Laser did not damage once per crossing");
        float final = Mathf.DeltaAngle(0f, r.Root.transform.Find("LaserBeam").eulerAngles.z);
        Check(enhanced ? final < -55f : final > 55f, "Laser did not return on Last Stand");
        return new { pattern = "Laser", enhanced, crossings = legs, speed, finalAngle = final, hits = r.Probe.ReceivedHitCount - hits };
    }

    private static async Task<object> Bomb(Rig r, bool enhanced)
    {
        r.Prepare(true, 4f);
        Check(r.Skills.TryUseSpecifiedSkill(r.Bomb), "Bomb did not start");
        var markers = new Dictionary<int, (DroneBombTargetGraphic graphic, Vector3 point, EnemyLobProjectile bomb)>();
        bool ringShrank = false, impactHidden = false;
        var sheet = enhanced ? null : new Texture2D(1536, 384, TextureFormat.RGBA32, false);
        bool[] captured = new bool[3];
        DroneBombTargetGraphic firstMarker = null;
        float deadline = Time.realtimeSinceStartup + 6f;
        int previousDrops = 0;

        while (r.Bomb.IsUsing && Time.realtimeSinceStartup < deadline)
        {
            foreach (var marker in Object.FindObjectsByType<DroneBombTargetGraphic>(FindObjectsSortMode.None))
            {
                if (markers.ContainsKey(marker.GetInstanceID())) continue;
                var projectile = marker.GetComponentInParent<EnemyLobProjectile>();
                markers[marker.GetInstanceID()] = (marker, marker.transform.position, projectile);
                if (firstMarker == null) firstMarker = marker;
                Canvas.ForceUpdateCanvases();
                Check(marker.canvasRenderer.GetMesh().vertexCount > 400, "Bomb warning mesh missing ellipse/cross/rings");
            }

            foreach (var marker in markers.Values)
            {
                if (marker.graphic == null) continue;
                Check(Vector3.Distance(marker.graphic.transform.position, marker.point) < 0.001f, "Bomb warning followed player");
                ringShrank |= marker.graphic.CountdownProgress > 0.2f && marker.graphic.CountdownProgress < 0.95f;
                if (marker.bomb != null && !marker.bomb.IsInFlight && !marker.graphic.gameObject.activeSelf)
                {
                    Check(marker.graphic.CountdownProgress == 1f, "Warning did not finish at impact");
                    impactHidden = true;
                }
            }

            if (sheet != null && firstMarker != null && firstMarker.gameObject.activeSelf)
                for (int i = 0; i < 3; i++)
                    if (!captured[i] && firstMarker.CountdownProgress >= new[] { 0.025f, 0.5f, 0.9f }[i])
                    { Capture(r.Camera, sheet, i, firstMarker.transform.position); captured[i] = true; }

            if (r.Bomb.DroppedCount != previousDrops)
            {
                previousDrops = r.Bomb.DroppedCount;
                r.Player.transform.position = new Vector3(previousDrops % 2 == 0 ? 2f : -3f, 0f, 0f);
                Physics2D.SyncTransforms();
            }
            await Awaitable.NextFrameAsync();
        }

        Check(!r.Bomb.IsUsing && r.Bomb.DroppedCount == (enhanced ? 6 : 3) && markers.Count == r.Bomb.DroppedCount,
            "Bomb drop count or completion mismatch");
        var times = r.Events.Where(e => e.skill == r.Bomb).Select(e => e.time).ToArray();
        for (int i = 1; i < times.Length; i++) Check(Mathf.Abs(times[i] - times[i - 1] - (enhanced ? 0.5f : 1f)) < 0.07f, "Bomb cadence mismatch");
        Check(ringShrank && impactHidden, "Shrinking countdown/impact synchronization failed");
        if (sheet != null)
        {
            Check(captured.All(x => x), "Warning preview capture missed a timing");
            System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23TargetBombPreview.png", sheet.EncodeToPNG());
            Object.Destroy(sheet);
        }
        return new { pattern = "Bomb", enhanced, drops = r.Bomb.DroppedCount, interval = enhanced ? 0.5f : 1f, fixedMarkers = markers.Count, ringShrank, impactHidden };
    }

    private static object RandomChoices(Rig r)
    {
        int[] ground = new int[3];
        for (int i = 0; i < 240; i++)
        {
            r.Prepare(false, 4f);
            Call(r.Boss, "ChooseGroundPattern");
            var active = r.Skills.ActiveSkill;
            Check(active == r.Charge || active == r.Barrage || active == r.Air, "Invalid ground candidate");
            ground[active == r.Charge ? 0 : active == r.Barrage ? 1 : 2]++;
        }
        int[] air = new int[2];
        for (int i = 0; i < 160; i++)
        {
            r.Prepare(true, 4f);
            Call(r.Boss, "ChooseAirPattern");
            var active = r.Skills.ActiveSkill;
            Check(active == r.Laser || active == r.Bomb, "Invalid air candidate");
            air[active == r.Laser ? 0 : 1]++;
        }
        Check(ground.All(x => x > 45 && x < 115) && air.All(x => x > 45 && x < 115), "Random groups are biased or unavailable");
        return new { pattern = "RandomSelection", ground, air };
    }

    private static async Task<object> LastStandEntry(Rig r)
    {
        r.Prepare(false, 4f);
        r.SetHealth(0.36f);
        await Awaitable.NextFrameAsync();
        Check(!r.Boss.IsLastStand, "Last Stand started above 35%");
        r.SetHealth(0.35f);
        await Until(() => r.Boss.IsLastStand, 0.3f, "Last Stand did not latch at 35%");
        Check((bool)Read(r.Boss, "_lastStandEntryPending"), "Entry reservation missing");
        r.SetState("ChooseGround");
        await Until(() => r.Air.IsUsing, 1f, "Entry did not ascend");
        await Until(() => r.Laser.IsPreparing, 2.5f, "Air transition did not force entry laser");
        Check((int)Read(r.Laser, "_sweepCount") == 1 && ReadFloat(r.Laser, "_startSpeed") == 600f && r.Shots.Count == 0,
            "Entry should preserve normal ascent and one-way laser");
        await Until(() => !r.Laser.IsUsing, 2f, "Entry laser stalled");
        await Until(() => !(bool)Read(r.Boss, "_lastStandEntryPending"), 0.3f, "Entry did not finish");
        return new { pattern = "LastStandEntry", boundary = 0.35f, oneWayPreserved = true, noEntryShotgun = true };
    }

    private static void Capture(Camera camera, Texture2D sheet, int index, Vector3 point)
    {
        Vector3 previousPosition = camera.transform.position;
        float previousSize = camera.orthographicSize;
        var target = RenderTexture.GetTemporary(512, 384, 24);
        var previous = RenderTexture.active;
        try
        {
            camera.transform.position = point + new Vector3(0f, 0.7f, -10f);
            camera.orthographicSize = 1.3f;
            camera.targetTexture = target;
            Canvas.ForceUpdateCanvases(); camera.Render(); RenderTexture.active = target;
            sheet.ReadPixels(new Rect(0, 0, 512, 384), index * 512, 0); sheet.Apply();
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            camera.transform.position = previousPosition; camera.orthographicSize = previousSize;
            RenderTexture.ReleaseTemporary(target);
        }
    }

    private sealed class Rig : IDisposable
    {
        public GameObject Root;
        public readonly GameObject Player;
        public readonly Camera Camera;
        public readonly EnemyDamageProbe Probe;
        public ProductionControlDrone Boss;
        public EnemySkillModule Skills;
        public BossFlightModule Flight;
        public DroneChargeSkill Charge;
        public DroneSalvoSkill Barrage;
        public DroneAirTransitionSkill Air;
        public DroneDownwardLaserSkill Laser;
        public DroneBombSkill Bomb;
        public readonly List<(EnemyAttackSkill skill, Vector3 position, float time)> Events = new();
        public readonly List<(Vector3 origin, Vector2 direction, float muzzleY)> Shots = new();
        private readonly HashSet<int> _seenShots = new();
        private readonly GameObject _floor;
        private readonly Sprite _white;

        public Rig()
        {
            _floor = new GameObject("Rework test floor") { layer = LayerMask.NameToLayer("Ground") };
            _floor.transform.position = Vector3.down * 0.1f;
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.2f);
            _white = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
            var floorVisual = new GameObject("Floor visual");
            floorVisual.transform.SetParent(_floor.transform, false);
            floorVisual.transform.localScale = new Vector3(40f, 0.2f, 1f);
            var floorRenderer = floorVisual.AddComponent<SpriteRenderer>();
            floorRenderer.sprite = _white; floorRenderer.color = new Color(0.2f, 0.25f, 0.28f); floorRenderer.sortingOrder = -1;

            Player = new GameObject("Rework player probe");
            var collider = Player.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; collider.size = new Vector2(0.35f, 1.35f); collider.offset = Vector2.up * 0.675f;
            Probe = Player.AddComponent<EnemyDamageProbe>();
            var cameraRoot = new GameObject("Rework camera");
            Camera = cameraRoot.AddComponent<Camera>();
            cameraRoot.AddComponent<AudioListener>();
            Camera.orthographic = true; Camera.orthographicSize = 4.8f;
            cameraRoot.transform.position = new Vector3(0f, 3.5f, -10f);
            Camera.clearFlags = CameraClearFlags.SolidColor; Camera.backgroundColor = new Color(0.05f, 0.065f, 0.08f);
        }

        public async Task NewBoss(bool enhanced)
        {
            ClearProjectiles();
            if (Root != null) { Root.SetActive(false); Object.Destroy(Root); await Awaitable.NextFrameAsync(); }
            Root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path), Vector3.up * 1.05f, Quaternion.identity);
            Root.SetActive(true);
            Boss = Root.GetComponent<ProductionControlDrone>();
            Skills = Root.GetComponentInChildren<EnemySkillModule>(); Flight = Root.GetComponentInChildren<BossFlightModule>();
            Charge = Root.GetComponentInChildren<DroneChargeSkill>(); Barrage = Root.GetComponentInChildren<DroneSalvoSkill>();
            Air = Root.GetComponentInChildren<DroneAirTransitionSkill>(); Laser = Root.GetComponentInChildren<DroneDownwardLaserSkill>();
            Bomb = Root.GetComponentInChildren<DroneBombSkill>();
            Boss.SetTarget(Player.transform);
            SetState(null);
            foreach (var entry in Skills.Skills)
                typeof(EnemyAttackSkill).GetEvent("AttackPerformed", Private).GetAddMethod(true).Invoke(entry.Skill,
                    new object[] { new Action<EnemyAttackSkill, Vector3>(OnAttack) });
            if (enhanced)
            {
                SetHealth(0.34f);
                await Until(() => Boss.IsLastStand, 0.3f, "Enhanced fixture did not latch phase");
                typeof(ProductionControlDrone).GetField("_lastStandEntryPending", Private).SetValue(Boss, false);
            }
            else await Awaitable.NextFrameAsync();
            Check(Boss.IsLastStand == enhanced, "Controlled phase setup failed");
        }

        public void Prepare(bool airborne, float playerX)
        {
            Skills.StopAttack(); ClearProjectiles();
            SetState(null);
            float y = new SerializedObject(Boss).FindProperty(airborne ? "airAttackAltitude" : "lowAltitude").floatValue;
            Flight.Place(new Vector2(0f, y));
            Player.transform.position = Vector3.right * playerX;
            foreach (var entry in Skills.Skills)
                typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime").SetValue(entry.Skill, float.NegativeInfinity);
            Events.Clear(); Shots.Clear(); _seenShots.Clear();
            Physics2D.SyncTransforms();
        }

        public void SetState(string state)
        {
            var field = typeof(ProductionControlDrone).GetField("_state", Private);
            field.SetValue(Boss, state == null ? Enum.ToObject(field.FieldType, -1) : Enum.Parse(field.FieldType, state));
        }

        public void SetHealth(float ratio)
        {
            var health = Root.GetComponentInChildren<NewHealthModule>();
            health.ApplyHeal(health.MaxHealth);
            health.ApplyDamage(health.MaxHealth * 0.9f);
            health.ApplyHeal(health.MaxHealth * ratio - health.CurrentHealth);
        }

        private void OnAttack(EnemyAttackSkill skill, Vector3 position)
        {
            Events.Add((skill, position, Time.time));
            float muzzleY = skill is DroneSalvoSkill || skill is DroneAirTransitionSkill
                ? ((Transform)new SerializedObject(skill).FindProperty("muzzle").objectReferenceValue).position.y : position.y;
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
            {
                if (!ReferenceEquals(Read(projectile, "_dealer"), Boss) || !_seenShots.Add(projectile.GetInstanceID())) continue;
                Shots.Add((projectile.transform.position, (Vector2)Read(projectile, "_direction"), muzzleY));
            }
        }

        private void ClearProjectiles()
        {
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if (ReferenceEquals(Read(projectile, "_dealer"), Boss)) Object.DestroyImmediate(projectile.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None))
                if (ReferenceEquals(Read(projectile, "_dealer"), Boss)) Object.DestroyImmediate(projectile.gameObject);
        }

        public void Dispose()
        {
            ClearProjectiles();
            if (Root != null) Object.Destroy(Root);
            Object.Destroy(Player); Object.Destroy(Camera.gameObject); Object.Destroy(_floor); Object.Destroy(_white);
        }
    }

    private static object Read(object target, string field) => target.GetType().GetField(field, Private).GetValue(target);
    private static float ReadFloat(object target, string field) => (float)Read(target, field);
    private static object Call(object target, string method) => target.GetType().GetMethod(method, Private).Invoke(target, null);

    private static async Task Until(Func<bool> condition, float seconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
