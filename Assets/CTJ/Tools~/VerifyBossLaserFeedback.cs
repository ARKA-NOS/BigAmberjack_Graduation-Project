using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent.StatSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // Task.Yield는 Update 중 재개될 수 있습니다. 실제 렌더링에 쓰이는 LateUpdate 이후 위치를 기록합니다.
    [DefaultExecutionOrder(10000)]
    public sealed class LaserCameraSample : MonoBehaviour
    {
        public Vector3 Position { get; private set; }
        private void LateUpdate() => Position = transform.position;
    }

    public static class VerifyBossLaserFeedback
    {
        private const string Root = "Assets/CTJ/Prototypes/Bosses/";
        private static int _checks;

        public static string BeginSceneEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.sceneCount == 1 &&
                SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity" && !SceneManager.GetActiveScene().isDirty, "Only clean isolated scene");
            AssetDatabase.DisallowAutoRefresh();
            SessionState.SetBool("CTJ.Laser.SceneEdit", true);
            return "Temporarily deferring refresh while adding CTJ test walls.";
        }

        public static string EndSceneEdit()
        {
            Check(SessionState.GetBool("CTJ.Laser.SceneEdit", false), "Not our scene edit");
            try
            {
                Check(!EditorApplication.isPlayingOrWillChangePlaymode && !SceneManager.GetActiveScene().isDirty &&
                    SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Do not overwrite user scene edits");
                EditorSceneManager.OpenScene(Root + "BossPatternTest.unity", OpenSceneMode.Single);
                return "Reloaded clean CTJ boss test scene with walls.";
            }
            finally { SessionState.EraseBool("CTJ.Laser.SceneEdit"); AssetDatabase.AllowAutoRefresh(); }
        }

        public static object Validate()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            foreach (string path in new[] { "BossLaserDelay.asset", "RobotEye_Stats.asset", "RobotEyeBoss.prefab" })
                AssetDatabase.ImportAsset(Root + path, ImportAssetOptions.ForceUpdate);
            RobotEyeBoss boss = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Root + "RobotEyeBoss.prefab");
            BossLaserSkill laser = boss.GetComponentInChildren<BossLaserSkill>();
            SerializedObject so = new SerializedObject(laser);
            StatData key = AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset");
            Check(so.FindProperty("attackDelayStat").objectReferenceValue == key, "Separate laser delay stat");
            Check(AssetDatabase.LoadAssetAtPath<StatGroup>(Root + "RobotEye_Stats.asset").Stats.Single(s => s.StatData == key).BaseValue == 0.65f, "Laser delay 0.65");
            Check(so.FindProperty("cameraShake").objectReferenceValue == laser.GetComponent<BossLaserShake>(), "Shake connection");
            Check(laser.PostBlinkDelay == 0.15f, "Keep readable warning hold");
            var warning = (AttackTelegraphUI)so.FindProperty("telegraphPrefab").objectReferenceValue;
            Check(warning.GetComponentInChildren<ConvexPolygonGraphic>() != null && warning.GetComponent<Canvas>().sortingOrder == -5, "Polygon warning behind boss");
            Check(boss.transform.Find("Visual").GetComponentsInChildren<SpriteRenderer>().All(r => r.sortingOrder > -5 && r.sortingOrder < 0), "Walls 0 > boss > warning -5");
            Check(new SerializedObject(boss).FindProperty("bodyVisual").objectReferenceValue != null, "Offscreen size reference");
            Check(so.FindProperty("beamLength").floatValue == 18f, "Configurable fixed length 18");
            Check(Mathf.Abs(new SerializedObject(warning).FindProperty("areaColor").colorValue.a - 25f / 255f) < 0.0001f, "Laser warning alpha 25");
            return new { valid = true, preparation = 0.65f, hold = 0.15f,
                traversalSpeed = new SerializedObject(boss).FindProperty("laserSweepSpeed").floatValue,
                length = 18f, warningAlpha = 25, safeSide = "left / right / left triangle" };
        }

        private sealed class Fixture : IDisposable
        {
            public readonly GameObject RootObject;
            public readonly RobotEyeBoss Boss;
            public readonly EnemyDamageProbe Probe;
            public BossArenaModule Arena => Boss.GetModule<BossArenaModule>();
            public BossFlightModule Flight => Boss.GetModule<BossFlightModule>();
            public BossLaserSkill Laser => Boss.GetComponentInChildren<BossLaserSkill>();
            public EnemySkillModule Skills => Boss.GetModule<EnemySkillModule>();
            public Fixture(float x)
            {
                RootObject = new GameObject("Laser feedback verification"); RootObject.SetActive(false);
                GameObject target = new GameObject("Probe"); target.transform.SetParent(RootObject.transform);
                target.transform.position = new Vector3(x + 30f, 1f, 0f);
                Probe = target.AddComponent<EnemyDamageProbe>();
                target.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
                target.AddComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
                GameObject extra = new GameObject("Second collider"); extra.transform.SetParent(target.transform, false);
                extra.AddComponent<BoxCollider2D>().size = new Vector2(0.2f, 0.2f);
                Boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Root + "RobotEyeBoss.prefab"), new Vector3(x, 4f, 0f), Quaternion.identity, RootObject.transform);
                Set(Boss, "target", target.transform); Set(Boss, "firstVolleyDelay", 1000f);
                Set(Boss.GetComponentInChildren<BossArenaModule>(), "introHeight", 0f);
                RootObject.SetActive(true);
                Set(Laser, "blinkBeforeAttack", false); Set(Laser, "postBlinkDelay", 0.02f);
                Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset")).SetBaseValue(0.03f);
            }
            public async Task Ready() => await Until(() => Boss.CurrentState == RobotEyeBossState.Waiting);
            public async Task Fire(int pass, Vector2 start, Vector2 end)
            {
                Flight.Place(start); Laser.ConfigureTraversal(Arena.LaserBounds, start, end);
                Check(Skills.TryUseSpecifiedSkill(Laser), "Specified skill entry");
                Check(Probe.ReceivedHitCount == 0 && !Laser.GetComponent<BossLaserShake>().IsShaking, "No damage/shake before release");
                await Until(() => Laser.IsFiring);
            }
            public void Dispose() { RootObject.SetActive(false); Object.Destroy(RootObject); }
        }

        public static async Task<object> Run()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Use isolated boss test scene");
            _checks = 0;
            for (int pass = 0; pass < 3; pass++)
            {
                using (Fixture f = new Fixture(3000f + pass * 100f))
                {
                    await f.Ready();
                    Vector2 start = f.Arena.GetLaserStart(pass), end = f.Arena.GetLaserEnd(pass);
                    Bounds danger = f.Arena.LaserBounds;
                    float safeX = pass % 2 == 0 ? f.Arena.LaserLeft.x + 1f : f.Arena.LaserRight.x - 1f;
                    f.Probe.transform.position = new Vector3(safeX, 0.7f, 0f);
                    await f.Fire(pass, start, end);
                    Check(f.Laser.DangerPolygon.Count == 5, "Rectangle minus right triangle produces five vertices");
                    Check(!Contains(f.Laser.DangerPolygon, f.Probe.transform.position), "Wall below firing boss is safe");
                    Check(Contains(f.Laser.DangerPolygon, new Vector2(end.x + (pass % 2 == 0 ? -1f : 1f), 0.7f)), "Opposite bottom corner is dangerous");
                    AttackTelegraphUI warning = f.Boss.GetComponentsInChildren<AttackTelegraphUI>().Single(t => t.IsVisible);
                    RectTransform rect = warning.GetComponent<RectTransform>();
                    Check(Vector2.Distance(rect.position, danger.center) < 0.001f && Vector2.Distance(rect.sizeDelta, danger.size) < 0.001f,
                        "Canvas uses arena coordinates and stays visible during firing");
                    Check(warning.GetComponentInChildren<ConvexPolygonGraphic>() != null, "Uses polygon Canvas graphic");
                    Vector2 direction = f.Laser.BeamDirection;
                    Check(Mathf.Abs(Mathf.Abs(direction.x) - 0.7071068f) < 0.001f && direction.y < 0f, "45-degree inward/down beam");
                    Check(Mathf.Abs(warning.GetComponentInChildren<ConvexPolygonGraphic>().color.a - 25f / 255f) < 0.0001f, "Runtime warning uses lower alpha");
                    bool fixedAngle = true, contained = true, fixedLength = true;
                    SpriteRenderer beamVisual = (SpriteRenderer)new SerializedObject(f.Laser).FindProperty("beamVisual").objectReferenceValue;
                    for (int i = 0; i <= 80; i++)
                    {
                        f.Flight.Place(Vector2.Lerp(start, end, i / 80f)); f.Laser.TickBeam();
                        fixedAngle &= Vector2.Distance(f.Laser.BeamDirection, direction) < 0.0001f;
                        fixedLength &= beamVisual.enabled && Mathf.Abs(VisualLength(beamVisual) - 18f) < 0.002f;
                        if (f.Laser.BeamSize.y <= 0f) continue;
                        Bounds beam = f.Laser.BeamBounds;
                        contained &= beam.min.x >= danger.min.x - 0.001f && beam.max.x <= danger.max.x + 0.001f &&
                            beam.min.y >= danger.min.y - 0.001f && beam.max.y <= danger.max.y + 0.001f;
                        Vector2 along = direction * f.Laser.BeamSize.y * 0.5f;
                        Vector2 across = new Vector2(-direction.y, direction.x) * f.Laser.BeamSize.x * 0.5f;
                        foreach (Vector2 corner in new[] { along + across, along - across, -along + across, -along - across })
                            contained &= Contains(f.Laser.DangerPolygon, f.Laser.BeamCenter + corner);
                    }
                    Check(contained, "Damage beam including thickness stays inside warned area");
                    Check(fixedLength, "Visual length stays 18 at floor and opposite wall, including last tick");
                    Check(fixedAngle, "Angle never rotates during traversal");
                    Check(f.Probe.ReceivedHitCount == 0, "Safe triangle takes zero damage for entire pass");
                    f.Laser.FinishTraversal();
                    Check(!warning.IsVisible && !f.Laser.GetComponent<BossLaserShake>().IsShaking, "Finish hides warning and stops shake");
                }
            }
            using (Fixture f = new Fixture(3400f))
            {
                await f.Ready();
                Vector2 start = f.Arena.GetLaserStart(0), end = f.Arena.GetLaserEnd(0);
                f.Probe.transform.position = new Vector3(3400f, 0.7f, 0f);
                await f.Fire(0, start, end);
                Check(f.Probe.ReceivedHitCount == 0, "Target not in initial beam");
                f.Flight.Place(end); f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1 && f.Probe.TotalDamage == 30f, "Whole traversal in one tick must not tunnel");
                for (int i = 0; i < 10; i++) f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1, "One hit per traversal despite multiple colliders/ticks");
                f.Boss.SetTarget(null);
                Check(!f.Laser.IsFiring && !f.Laser.GetComponent<BossLaserShake>().IsShaking, "Target loss cancels shake and beam");
            }
            using (Fixture f = new Fixture(3500f))
            {
                await f.Ready();
                Vector2 start = f.Arena.GetLaserStart(0), end = f.Arena.GetLaserEnd(0);
                f.Flight.Place(Vector2.Lerp(start, end, 0.25f)); f.Laser.ConfigureTraversal(f.Arena.LaserBounds, start, end);
                Bounds beam = f.Laser.BeamBounds;
                // 빔 AABB의 위쪽 먼 모서리: 사각형 전체를 피격 범위로 사용하면 오판정하는 위치입니다.
                f.Probe.transform.position = new Vector3(beam.max.x - 0.15f, beam.max.y - 0.25f, 0f);
                Check(f.Skills.TryUseSpecifiedSkill(f.Laser), "Start diagonal beam");
                await Until(() => f.Laser.IsFiring);
                Check(f.Probe.ReceivedHitCount == 0, "AABB corner outside actual diagonal beam must not hit");
                f.Probe.transform.position = f.Laser.BeamCenter; f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1, "Actual rotated box hits");
                f.Boss.gameObject.SetActive(false);
                Check(!f.Laser.GetComponent<BossLaserShake>().IsShaking, "Disable cleanup");
            }
            await ShortBeam();
            await ProductionPhase();
            await ProductionPhase(50f); // 현재 테스트 씬의 빠른 발사 설정에서도 벽 감속/복귀 템포를 확인합니다.
            await Shake();
            return new { passed = true, checks = _checks, scope = "fixed length / short-range warning and damage / wall pass-through / offscreen exit / shortened gaps and return at speeds 20 and 50 / shake" };
        }

        private static float VisualLength(SpriteRenderer visual) =>
            visual.transform.TransformVector(Vector3.up * visual.sprite.bounds.size.y).magnitude;

        // 씬의 사용자 발사 속도(프리팹 오버라이드 포함)로 실제 재공격/복귀 시간을 측정합니다.
        public static async Task<object> MeasureTempo()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Use isolated boss scene");
            RobotEyeBoss sceneBoss = Object.FindFirstObjectByType<RobotEyeBoss>();
            float speed = new SerializedObject(sceneBoss).FindProperty("laserSweepSpeed").floatValue;
            bool wasActive = sceneBoss.gameObject.activeSelf;
            sceneBoss.gameObject.SetActive(false);
            try
            {
                using (Fixture f = new Fixture(0f))
                {
                    await f.Ready();
                    Set(f.Boss, "laserSweepSpeed", speed);
                    Set(f.Laser, "blinkBeforeAttack", true); Set(f.Laser, "postBlinkDelay", 0.15f);
                    f.Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset")).SetBaseValue(0.65f);
                    f.Boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, f.Boss.transform.position, Vector2.right, Vector2.left);
                    float lastEnd = 0f;
                    List<float> gaps = new List<float>();
                    for (int pass = 0; pass < 3; pass++)
                    {
                        await Until(() => f.Laser.IsFiring && f.Boss.CompletedTraversals == pass);
                        if (pass > 0) gaps.Add(Time.time - lastEnd);
                        await Until(() => f.Boss.CompletedTraversals > pass);
                        lastEnd = Time.time;
                    }
                    await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                    float returnedAt = Time.time;
                    Check(!f.Boss.IsInvulnerable && Vector2.Distance(f.Boss.transform.position, f.Arena.Center) < 0.01f, "Tempo run returns to center");
                    await Until(() => f.Boss.CurrentState == RobotEyeBossState.RadialVolley);
                    return new { sweepSpeed = speed, laserGaps = gaps.ToArray(),
                        thirdLaserToCenter = returnedAt - lastEnd, centerToNextWarning = Time.time - returnedAt };
                }
            }
            finally { sceneBoss.gameObject.SetActive(wasActive); }
        }

        private static async Task ShortBeam()
        {
            using (Fixture f = new Fixture(3550f))
            {
                await f.Ready();
                Vector2 start = f.Arena.GetLaserStart(0), end = f.Arena.GetLaserEnd(0);
                Set(f.Laser, "beamLength", 3f);
                f.Probe.transform.position = new Vector3(3550f, 0.7f, 0f);
                await f.Fire(0, start, end);
                Check(!Contains(f.Laser.DangerPolygon, f.Probe.transform.position), "Short beam warning does not extend to floor");
                SpriteRenderer visual = (SpriteRenderer)new SerializedObject(f.Laser).FindProperty("beamVisual").objectReferenceValue;
                f.Flight.Place(end); f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 0 && Mathf.Abs(VisualLength(visual) - 3f) < 0.001f, "Short beam cannot damage beyond configured length");
                f.Laser.FinishTraversal();
                Set(f.Laser, "beamLength", 18f);
                await f.Fire(0, start, end);
                Check(Contains(f.Laser.DangerPolygon, f.Probe.transform.position), "Next cast rebuilds warning for changed length");
                f.Flight.Place(end); f.Laser.TickBeam();
                Check(f.Probe.ReceivedHitCount == 1 && Mathf.Abs(VisualLength(visual) - 18f) < 0.002f, "Longer beam reaches the same floor target");
            }
        }

        private static async Task ProductionPhase(float? speedOverride = null)
        {
            RobotEyeBoss sceneBoss = Object.FindFirstObjectByType<RobotEyeBoss>();
            sceneBoss.gameObject.SetActive(false);
            try
            {
            using (Fixture f = new Fixture(0f))
            {
                await f.Ready();
                if (speedOverride.HasValue) Set(f.Boss, "laserSweepSpeed", speedOverride.Value);
                Set(f.Laser, "blinkBeforeAttack", true); Set(f.Laser, "postBlinkDelay", 0.15f);
                f.Boss.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Root + "BossLaserDelay.asset")).SetBaseValue(0.65f);
                f.Boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, f.Boss.transform.position, Vector2.right, Vector2.left);
                float lastEndedAt = 0f;
                for (int pass = 0; pass < 3; pass++)
                {
                    await Until(() => f.Boss.CurrentState == RobotEyeBossState.LaserSweep && f.Boss.CompletedTraversals == pass);
                    float warningAt = Time.time;
                    Check(Vector2.Distance(f.Boss.transform.position, f.Arena.GetLaserStart(pass)) < 0.01f, "Prepare with body center exactly on alternating wall");
                    Vector3 preparationPosition = f.Boss.transform.position;
                    await Wait(0.2f);
                    Check(f.Laser.IsPreparing && f.Boss.transform.position == preparationPosition, "Hold still before firing");
                    await Until(() => f.Laser.IsFiring);
                    Check(Time.time - warningAt >= 0.78f && Time.time - warningAt < 1.1f, "Production warning about 0.8 seconds");
                    if (pass > 0) Check(Time.time - lastEndedAt < 2.8f, "Next laser fires within 2.8 seconds of previous wall crossing");
                    float releaseAt = Time.time;
                    Check(f.Laser.GetComponent<BossLaserShake>().IsShaking, "Production beam starts shake");
                    float speed = new SerializedObject(f.Boss).FindProperty("laserSweepSpeed").floatValue;
                    float ramp = new SerializedObject(f.Boss).FindProperty("laserMoveEaseDuration").floatValue;
                    float wallX = f.Arena.GetLaserEnd(pass).x;
                    float sign = pass % 2 == 0 ? 1f : -1f;
                    await Until(() => (f.Boss.transform.position.x - wallX) * sign >= -1f);
                    Vector2 beforeWall = f.Boss.transform.position;
                    float beforeWallTime = Time.fixedTime;
                    await Until(() => f.Boss.CompletedTraversals > pass);
                    lastEndedAt = Time.time;
                    float expectedWallTime = 16f / speed + ramp * 0.5f;
                    Check(Mathf.Abs(Time.time - releaseAt - expectedWallTime) < 0.12f, "No deceleration overhead before crossing wall");
                    Check(f.Flight.IsMoving && f.Boss.CurrentState == RobotEyeBossState.LaserExiting, "Wall crossing keeps existing movement active");
                    await Until(() => (f.Boss.transform.position.x - wallX) * sign >= 1f);
                    float crossingSpeed = Vector2.Distance(beforeWall, f.Boss.transform.position) / (Time.fixedTime - beforeWallTime);
                    Check(Mathf.Abs(crossingSpeed - speed) < 0.1f, "Same cruising speed before and after wall; no restart or pause");
                    Check(!f.Laser.GetComponent<BossLaserShake>().IsShaking, "No shake between passes");
                    var body = (SpriteRenderer)new SerializedObject(f.Boss).FindProperty("bodyVisual").objectReferenceValue;
                    Vector2 previousPosition = f.Boss.transform.position;
                    float previousTime = Time.fixedTime;
                    bool noVisibleBraking = true;
                    await Until(() =>
                    {
                        float dt = Time.fixedTime - previousTime;
                        if (dt > 0f)
                        {
                            float actualSpeed = Vector2.Distance(previousPosition, f.Boss.transform.position) / dt;
                            if (InCamera(body.bounds)) noVisibleBraking &= actualSpeed >= speed - 0.1f;
                            previousPosition = f.Boss.transform.position;
                            previousTime = Time.fixedTime;
                        }
                        return f.Boss.CurrentState == RobotEyeBossState.LaserHidden;
                    });
                    Check(noVisibleBraking, "Deceleration starts only after whole body leaves camera");
                    Check(!InCamera(body.bounds), "Entire boss leaves camera after each firing");
                    Check(!f.Laser.IsUsing && !f.Boss.GetComponentsInChildren<AttackTelegraphUI>().Any(t => t.IsVisible), "No beam or warning outside arena");
                }
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.ReturnAbove);
                var visual = (SpriteRenderer)new SerializedObject(f.Boss).FindProperty("bodyVisual").objectReferenceValue;
                bool hiddenAbove = true;
                while (f.Boss.CurrentState == RobotEyeBossState.ReturnAbove)
                {
                    hiddenAbove &= !InCamera(visual.bounds);
                    await Task.Yield();
                }
                Check(hiddenAbove, "Horizontal return stays above camera");
                Check(f.Boss.CurrentState == RobotEyeBossState.ReturnCenter, "Return from overhead after third pass");
                Check(Mathf.Abs(f.Boss.transform.position.x - f.Arena.Center.x) < 0.01f && !InCamera(visual.bounds), "Descent starts above screen center");
                await Wait(0.2f);
                Check(Mathf.Abs(f.Boss.transform.position.x - f.Arena.Center.x) < 0.01f, "Return is vertical, not diagonal");
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.Waiting);
                Check(f.Boss.CompletedTraversals == 3 && !f.Boss.IsInvulnerable, "Production phase completes exactly three passes");
                Check(Time.time - lastEndedAt < 4.5f, "Third laser returns to center within 4.5 seconds");
                float returnedAt = Time.time;
                await Until(() => f.Boss.CurrentState == RobotEyeBossState.RadialVolley);
                Check(Time.time - returnedAt >= 1.48f && Time.time - returnedAt < 1.7f, "Next normal warning starts after 1.5 second rest");
            }
            }
            finally { sceneBoss.gameObject.SetActive(true); }
        }

        private static bool InCamera(Bounds bounds)
        {
            Vector3 min = Camera.main.WorldToViewportPoint(bounds.min), max = Camera.main.WorldToViewportPoint(bounds.max);
            return min.x <= 1f && max.x >= 0f && min.y <= 1f && max.y >= 0f;
        }

        private static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            for (int i = 0; i < polygon.Count; i++)
            {
                Vector2 edge = polygon[(i + 1) % polygon.Count] - polygon[i], relative = point - polygon[i];
                if (edge.x * relative.y - edge.y * relative.x > 0.02f) return false; // 시계 방향, float 오차 허용
            }
            return true;
        }

        private static async Task Shake()
        {
            Camera camera = Camera.main;
            CinemachineExternalImpulseListener listener = camera.GetComponent<CinemachineExternalImpulseListener>();
            Check(listener != null && listener.enabled && listener.ChannelMask == BossLaserShake.ImpulseChannel, "CTJ camera listener wired");
            GameObject go = new GameObject("Shake verification");
            BossLaserShake shake = go.AddComponent<BossLaserShake>();
            LaserCameraSample sample = camera.gameObject.AddComponent<LaserCameraSample>();
            Vector3 before = camera.transform.position;
            try
            {
                shake.StartShake(new Bounds(new Vector3(before.x, before.y, 0f), Vector3.one * 10f));
                await Wait(0.1f);
                Check(shake.IsShaking && Vector3.Distance(before, sample.Position) > 0.001f, "Live camera actually moves");
                Vector3 impulse; Quaternion rotation;
                Check(CinemachineImpulseManager.Instance.GetImpulseAt(new Vector3(before.x, before.y, 0f), true,
                    BossLaserShake.ImpulseChannel, out impulse, out rotation), "Own channel signal is active");
                shake.StopShake(); await Wait(0.05f);
                Check(!shake.IsShaking && Vector3.Distance(before, sample.Position) < 0.0001f, "No camera drift after stop");
                Check(!CinemachineImpulseManager.Instance.GetImpulseAt(new Vector3(before.x, before.y, 0f), true,
                    BossLaserShake.ImpulseChannel, out impulse, out rotation), "Signal removed after stop");
                shake.StartShake(new Bounds(before, Vector3.one));
                var cancelled = (CinemachineImpulseManager.ImpulseEvent)typeof(BossLaserShake)
                    .GetField("_impulse", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).GetValue(shake);
                shake.StopShake();
                Check(cancelled.SignalSource == null && cancelled.Envelope.Duration > 0f, "Same-frame cancel silences signal and permits event recycling");
                shake.enabled = false; shake.StartShake(new Bounds(before, Vector3.one));
                Check(!shake.IsShaking, "Inspector opt-out");
            }
            finally { shake.StopShake(); Object.Destroy(go); Object.Destroy(sample); }
        }

        public static async Task<object> Capture()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == Root + "BossPatternTest.unity", "Use isolated boss scene");
            RobotEyeBoss boss = Object.FindFirstObjectByType<RobotEyeBoss>();
            bool wasActive = boss.gameObject.activeSelf;
            boss.gameObject.SetActive(false);
            try
            {
                using (Fixture f = new Fixture(0f))
                {
                    await f.Ready();
                    foreach (int pass in new[] { 0, 1 })
                    {
                        Vector2 start = f.Arena.GetLaserStart(pass), end = f.Arena.GetLaserEnd(pass);
                        f.Flight.Place(start);
                        f.Laser.ConfigureTraversal(f.Arena.LaserBounds, start, end);
                        Check(f.Skills.TryUseSpecifiedSkill(f.Laser), "Preview skill start");
                        Canvas.ForceUpdateCanvases();
                        CaptureCamera("Assets/CTJ/Tools~/LaserWallWarning" + pass + ".png");
                        await Until(() => f.Laser.IsFiring);
                        f.Laser.TickBeam(); Canvas.ForceUpdateCanvases();
                        CaptureCamera("Assets/CTJ/Tools~/LaserSafeSide" + pass + ".png");
                        f.Skills.StopAttack();
                    }
                }
            }
            finally { boss.gameObject.SetActive(wasActive); }
            return new { paths = new[] { "Assets/CTJ/Tools~/LaserSafeSide0.png", "Assets/CTJ/Tools~/LaserSafeSide1.png" } };
        }

        private static void CaptureCamera(string path)
        {
            Camera camera = Camera.main;
            RenderTexture previous = camera.targetTexture, active = RenderTexture.active;
            RenderTexture rt = new RenderTexture(1280, 720, 24);
            Texture2D texture = new Texture2D(1280, 720, TextureFormat.RGB24, false);
            try
            {
                camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                texture.ReadPixels(new Rect(0, 0, 1280, 720), 0, 0); texture.Apply();
                System.IO.File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally { camera.targetTexture = previous; RenderTexture.active = active; Object.Destroy(rt); Object.Destroy(texture); }
        }
        private static Task Wait(float seconds) { float end = Time.time + seconds; return Until(() => Time.time >= end); }
        private static async Task Until(Func<bool> condition)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > end) throw new InvalidOperationException("Laser test timeout");
                await Task.Yield();
            }
        }
        private static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field);
            if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else p.objectReferenceValue = (Object)value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
    }
}
