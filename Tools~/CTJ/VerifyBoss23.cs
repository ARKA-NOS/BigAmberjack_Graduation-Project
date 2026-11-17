using System;
using System.Linq;
using System.Threading.Tasks;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Currency;
using _Scripts.DevLib.BattleSystem;
using _Scripts.Testing.Enemy;
using _Scripts.UI.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyBoss23
{
    private const string Folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";
    private const string Prefab = Folder + "Boss23_ProductionControlDrone.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Check(root != null && root.GetComponent<ProductionControlDrone>() != null, "Boss prefab not found");
        var dependencies = AssetDatabase.GetDependencies(Prefab);
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/") &&
            !path.Contains("Enemy17_IntakeBiter") && !path.Contains("/Enemy18/")), "Obsolete enemy dependency");
        foreach (string path in dependencies.Where(path => path.StartsWith(Folder) && path.EndsWith(".prefab")))
        foreach (var node in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script on " + node.name);
            foreach (var component in node.GetComponents<Component>())
            {
                var field = new SerializedObject(component).GetIterator();
                while (field.Next(true))
                    if (field.propertyType == SerializedPropertyType.ObjectReference)
                        Check(field.objectReferenceValue != null || field.objectReferenceInstanceIDValue == 0,
                            $"Broken reference {node.name}/{field.propertyPath}");
            }
        }
        var slots = root.GetComponentInChildren<EnemySkillModule>().Skills;
        Check(slots.Count == 4 && slots.All(entry => entry.Skill != null && entry.Skill.enabled &&
            entry.Skill.SkillSo != null && !entry.UseForAutomaticSelection) &&
            slots.Select(entry => entry.Skill.SkillSo).Distinct().Count() == 4, "Missing boss skill registration");
        var animator = root.GetComponentInChildren<Animator>();
        Check(animator.transform.Find("ProcessNumber") != null && animator.transform.Find("AlarmLamp") != null,
            "Body markings are outside the Animator hierarchy");
        var clips = animator.runtimeAnimatorController.animationClips;
        var sprite = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 11 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, sprite)
            .All(key => key.value is Sprite)), "Missing Voidrone frames");
        foreach (var clip in clips.Where(clip => !clip.name.EndsWith("_Death", StringComparison.Ordinal)))
        {
            var frames = AnimationUtility.GetObjectReferenceCurve(clip, sprite);
            foreach (string path in new[] { "ProcessNumber", "AlarmLamp" })
            foreach (string axis in new[] { "x", "y" })
            {
                var curve = AnimationUtility.GetEditorCurve(clip,
                    EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition." + axis));
                Check(curve != null && curve.length == frames.Length && curve.keys.Zip(frames,
                    (mark, frame) => Mathf.Abs(mark.time - frame.time) < 0.001f).All(matches => matches),
                    $"Markings do not follow sprite frames in {clip.name}");
            }
        }
        Check(LastSprite("Laser").name == "robot only_19" && LastSprite("Bomb").name == "robot only_20",
            "Missing final frames of the source attacks");

        Sprite LastSprite(string state) => (Sprite)AnimationUtility.GetObjectReferenceCurve(
            clips.Single(clip => clip.name.EndsWith("_" + state, StringComparison.Ordinal)), sprite).Last().value;
        var bomb = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Boss23_Bomb.prefab").GetComponent<EnemyLobProjectile>();
        var blast = new SerializedObject(bomb).FindProperty("impactFrames");
        Check(blast.arraySize == 5 && Enumerable.Range(0, blast.arraySize)
            .All(i => blast.GetArrayElementAtIndex(i).objectReferenceValue is Sprite frame && frame.rect.width == 86f),
            "Invalid bomb explosion slices");
        return new { dependencies = dependencies.Length, clips = clips.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use isolated Play Mode");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = new GameObject("Ground") { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = Vector3.down * 0.2f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(46f, 0.4f);
        var player = new GameObject("Player probe");
        var body = player.AddComponent<BoxCollider2D>();
        body.isTrigger = true;
        body.size = new Vector2(0.35f, 1.35f);
        body.offset = Vector2.up * 0.675f;
        var probe = player.AddComponent<EnemyDamageProbe>();
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), Vector3.up * 1.05f, Quaternion.identity);
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var boss = root.GetComponent<ProductionControlDrone>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            Check(skills.Skills.All(entry => skills.IsSkillEnabled(entry.Skill)), "Boss skills were not registered");
            var salvos = root.GetComponentsInChildren<DroneSalvoSkill>();
            var normal = salvos.Single(skill => skill.name == "RegularSalvo");
            var rapid = salvos.Single(skill => skill.name == "RapidSalvo");
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>();
            var bomb = root.GetComponentInChildren<DroneBombSkill>();
            var flight = root.GetComponentInChildren<BossFlightModule>();
            var animator = root.GetComponentInChildren<Animator>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var flash = root.transform.Find("Visual/GunFlash").GetComponent<SpriteRenderer>();
            var beam = root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>();
            var number = animator.transform.Find("ProcessNumber").GetComponent<SpriteRenderer>();
            var lamp = animator.transform.Find("AlarmLamp").GetComponent<SpriteRenderer>();
            PlacePlayer(18f);
            boss.SetTarget(player.transform);
            Check(health.CurrentHealth == 550f && boss.DetectionRange == 12f && boss.ChaseReleaseRange == 16f, "Wrong boss stats");
            await Until(() => Object.FindAnyObjectByType<BossHealthBarUI>() != null, 1f, "Missing health bar");
            Check(!skills.IsBusy && !flight.IsMoving, "Boss started outside detection");
            float numberY = number.transform.localPosition.y;
            float lampY = lamp.transform.localPosition.y;
            await Until(() => Mathf.Abs(number.transform.localPosition.y - numberY) > 0.04f, 1f, "No Idle marking motion");
            Check(number.transform.localPosition.x < -0.6f && lamp.transform.localPosition.x < -0.35f &&
                Mathf.Abs((number.transform.localPosition.y - numberY) - (lamp.transform.localPosition.y - lampY)) < 0.001f,
                "Markings are misplaced or move independently of the body");
            PlacePlayer(10f);
            await Until(() => flight.IsMoving, 1f, "Boss did not engage before entering firing range");
            PlacePlayer(22f);
            await Until(() => !flight.IsMoving && Mathf.Abs(root.transform.position.x) < 0.03f, 5f, "Boss did not return to center");
            PlacePlayer(5f);
            await Until(() => normal.IsPreparing, 2f, "No normal shot");
            float prepared = Time.time;
            await Until(() => flash.enabled, 0.4f, "No muzzle warning");
            await Until(() => normal.DidFire, 1.2f, "Normal shot did not fire");
            float normalFire = normal.CoolTimeStartTime;
            Check(Mathf.Abs(normalFire - prepared - 0.55f) < 0.08f, "Wrong normal windup");
            await CheckSalvoRecovery(normal, "Shot", normalFire);
            await Until(() => probe.TotalDamage == 14f, 1.5f, "Normal shot missed");
            await Until(() => !normal.IsUsing, 1.1f, "Normal recovery stalled");
            PlacePlayer(-5f);
            await Until(() => rapid.IsPreparing, 3.5f, "Salvos did not alternate");
            Check(Time.time - normalFire >= 2.39f && root.transform.Find("Visual").localScale.x < 0f, "Early or backwards rapid shot");
            await Until(() => rapid.DidFire, 1.2f, "Rapid shot did not fire");
            await CheckSalvoRecovery(rapid, "RapidShot", rapid.CoolTimeStartTime);
            Check(number.flipX && number.transform.position.x > animator.transform.position.x,
                "Reversed boss did not keep the number readable on the rear");
            await Until(() => probe.ReceivedHitCount == 4, 1.4f, "Rapid shot count");
            Check(probe.TotalDamage == 38f, "Wrong rapid damage");

            Hit(boss, 300f);
            Check(Mathf.Abs(health.CurrentHealth - (550f - 30000f / 108f)) < 0.05f && !reaction.IsReacting, "Wrong defense or hit reaction");
            await Until(() => laser.IsPreparing, 6f, "No laser at half HP");
            Check(boss.IsAirPhase && !rapid.IsUsing && root.transform.position.y > 4.5f, "Air phase did not rise first");
            var warning = root.GetComponentsInChildren<AttackTelegraphUI>().Single(view => view.IsVisible);
            var rect = warning.GetComponent<RectTransform>();
            Vector2 lockPoint = rect.position;
            Check(Mathf.Abs(rect.sizeDelta.x - 0.8f) < 0.01f && rect.sizeDelta.y <= 4.5f &&
                Mathf.Abs(rect.position.y - rect.sizeDelta.y * 0.5f) < 0.04f, "Wrong laser column");
            prepared = Time.time;
            await Until(() => beam.enabled, 1.5f, "Laser did not fire");
            Check(Mathf.Abs(laser.CoolTimeStartTime - prepared - 0.9f) < 0.08f && !warning.IsVisible,
                "Wrong laser timing");
            await Until(() => probe.ReceivedHitCount == 5, 0.8f, "Laser did not hit once");
            Check(probe.TotalDamage == 56f, "Wrong laser damage");
            await Until(() => !beam.enabled, 1f, "Laser beam stayed on");
            Check(laser.IsUsing && InState("Laser"), "Beam expiry cut the laser animation short");
            await Until(() => InState("Descend"), 1f, "No descent after full laser animation");
            Check(Time.time - laser.CoolTimeStartTime >= 0.53f, "Laser descent started before the last frame");
            await Until(() => !beam.enabled && root.transform.position.y < 1.08f && flight.HasArrived, 3f, "Drone did not descend");
            await Until(() => InState("Idle"), 0.6f, "Air recovery did not start in Idle");
            float landedAt = Time.time;
            PlacePlayer(lockPoint.x + 2f);
            await Until(() => bomb.IsPreparing, 5f, "No bomb");
            Check(Time.time - landedAt >= 1.19f && !laser.IsUsing && probe.ReceivedHitCount == 5,
                "Air attacks overlapped or recovery was too short");
            warning = root.GetComponentsInChildren<AttackTelegraphUI>().Single(view => view.IsVisible);
            rect = warning.GetComponent<RectTransform>();
            lockPoint = rect.position;
            Check(Mathf.Abs(rect.sizeDelta.x - 2.6f) < 0.01f &&
                Mathf.Abs(lockPoint.x - player.transform.position.x) < 0.03f, "Wrong bomb circle");
            prepared = Time.time;
            PlacePlayer(lockPoint.x + 2.2f);
            await Until(() => Object.FindAnyObjectByType<EnemyLobProjectile>() != null, 1.4f, "Bomb did not launch");
            var projectile = Object.FindAnyObjectByType<EnemyLobProjectile>();
            Check(warning.IsVisible && Mathf.Abs(bomb.CoolTimeStartTime - prepared - 0.8f) < 0.08f, "Wrong bomb release");
            await Until(() => Time.time - bomb.CoolTimeStartTime >= 0.18f, 0.7f, "Bomb did not move");
            Check(projectile.GetComponentInChildren<SpriteRenderer>().transform.position.y > projectile.transform.position.y + 0.8f &&
                Vector2.Distance(rect.position, lockPoint) < 0.03f, "Bomb did not launch upward or retargeted");
            await Until(() => InState("Descend"), 1.2f, "No descent after full bomb animation");
            Check(Time.time - bomb.CoolTimeStartTime >= 0.70f, "Bomb descent cut the launch animation short");
            PlacePlayer(lockPoint.x);
            await Until(() => probe.ReceivedHitCount == 6, 1.6f, "Bomb did not impact at its original circle");
            Check(probe.TotalDamage == 76f && !warning.IsVisible &&
                projectile.GetComponentInChildren<SpriteRenderer>().sprite.name.StartsWith("explosion_"),
                "Wrong bomb damage or no explosion animation");
            PlacePlayer(22f);
            await Until(() => !flight.IsMoving && Mathf.Abs(root.transform.position.x) < 0.03f, 8f, "No center return after air attack");
            Check(probe.ReceivedHitCount == 6, "Repeated laser or bomb damage");

            var reward = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>());
            var currency = new TestCurrency { Data = new CurrencyData((CurrencySO)reward.FindProperty("rewardCurrency").objectReferenceValue, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(boss, 999f);
            Check(boss.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death") &&
                Object.FindAnyObjectByType<BossHealthBarUI>() == null, "Death animation or health bar cleanup failed");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No gold drop");
            int total = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(total >= 45 && total <= 65 && counter.GetKillCount() == 1, "Wrong reward or kill count");
            await Until(() => root == null && currency.Data.Count == total, 8f, "Case did not clean up or grant coins");
            return new { hits = probe.ReceivedHitCount, damage = probe.TotalDamage, gold = total,
                centerReturn = true, recoveryInIdle = true, animatedMarkings = true };

            bool InState(string state) => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer." + state);

            async Task CheckSalvoRecovery(DroneSalvoSkill skill, string state, float firstFire)
            {
                await Until(() => InState(state), 0.3f, "Missing " + state);
                float clipDuration = animator.GetCurrentAnimatorStateInfo(0).length;
                Check(!skill.IsRecovering, "Recovery started while the firing clip was playing");
                await Until(() => skill.IsRecovering, 1.2f, "No recovery after " + state);
                float recoveryAt = Time.time;
                Check(recoveryAt - firstFire >= clipDuration - 0.02f, "Recovery interrupted " + state);
                await Until(() => InState("Idle"), 0.15f, "Recovery did not show Idle");
                await Until(() => Time.time - recoveryAt >= 0.45f, 0.7f, "Recovery clock stalled");
                Check(skill.IsRecovering && InState("Idle"), "Idle was interrupted during recovery");
                await Until(() => !skill.IsUsing, 0.7f, "Recovery did not end");
                Check(Mathf.Abs(Time.time - recoveryAt - 0.8f) < 0.09f, "Wrong recovery duration");
            }

            void PlacePlayer(float x)
            {
                player.transform.position = Vector3.right * x;
                Physics2D.SyncTransforms();
            }
        }
        finally
        {
            foreach (var item in new[] { root, player, floor, counter.gameObject })
                if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)) Object.Destroy(projectile.gameObject);
            foreach (var projectile in Object.FindObjectsByType<EnemyLobProjectile>(FindObjectsSortMode.None)) Object.Destroy(projectile.gameObject);
            ServiceLocator.Register<ICurrencyManager>(previousCurrency);
            Application.runInBackground = background;
            Time.maximumDeltaTime = maxDelta;
        }
    }

    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }

    private static void Hit(EnemyBase boss, float damage) => boss.ApplyDamage(
        new DamageData { DamageAmount = damage }, boss.transform.position, Vector2.right, Vector2.left);

    private static async Task Until(Func<bool> condition, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
