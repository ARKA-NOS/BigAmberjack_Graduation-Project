using System;
using System.Linq;
using System.Threading.Tasks;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
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

public static class VerifyEnemy18
{
    private const string PrefabPath = "Assets/_Prefabs/Enemies/Enemy/Enemy18/Enemy18_EffluentLeaper.prefab";

    public static object CheckAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "No Enemy18 prefab");
        var modules = prefab.GetComponentsInChildren<Transform>(true);
        foreach (var node in modules)
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0, "Missing script on " + node.name);
            foreach (var component in node.GetComponents<Component>())
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                            $"Broken reference {node.name}/{property.propertyPath}");
            }
        }

        var dependencies = AssetDatabase.GetDependencies(PrefabPath);
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/")), "Old enemy dependencies");
        var modulesSkills = prefab.GetComponentInChildren<EnemySkillModule>();
        var leap = prefab.GetComponentInChildren<EnemyLeapSkill>();
        Check(modulesSkills.Skills.Single().Skill == leap && leap.SkillSo != null && leap.CoolTime == 3f &&
            modulesSkills.Skills[0].MaxRange == 3.5f, "Incorrect skill setup");
        var settings = new SerializedObject(leap);
        Check(settings.FindProperty("landingParticlePrefab").objectReferenceValue is ParticleSystem splash &&
            !splash.main.loop && splash.GetComponent<ParticleSystemRenderer>().sharedMaterial != null, "Invalid splash prefab");
        Check(settings.FindProperty("telegraphPrefab").objectReferenceValue is AttackTelegraphUI warning &&
            warning.GetComponentsInChildren<UnityEngine.UI.Graphic>().Single() is EnemyLeapLandingGraphic,
            "Warning must contain only the landing area");
        var leapEntry = new SerializedObject(modulesSkills).FindProperty("skills").GetArrayElementAtIndex(0);
        var keepDistance = prefab.GetComponentInChildren<EnemyKeepDistanceModule>();
        var retreat = new SerializedObject(keepDistance);
        Check(leapEntry.FindPropertyRelative("minRange").floatValue == 2f &&
            !leapEntry.FindPropertyRelative("allowCloseAttackWhenRetreatBlocked").boolValue &&
            keepDistance.MinimumDistance == 2f && retreat.FindProperty("retreatDuringCooldownOf").objectReferenceValue == leap,
            "Minimum leap distance and retreat do not match");
        var clips = prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 7 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, binding)
            .All(frame => frame.value is Sprite)), "Missing animation sprites");
        return new { dependencies = dependencies.Length, animations = clips.Length, modules = modules.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use an isolated Play Mode scene");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = MakeTerrain("Ground", new Vector2(0f, -0.1f), new Vector2(24f, 0.2f));
        var target = new GameObject("Player probe");
        target.transform.position = new Vector3(13f, 0f);
        var collider = target.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.55f, 1.5f);
        collider.offset = Vector2.up * 0.75f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), new Vector3(0f, 0.025f),
            Quaternion.identity);
        BoxCollider2D ceiling = null;
        AttackTelegraphUI warning = null;

        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var move = root.GetComponentInChildren<EnemyMovementModule>();
            var patrol = root.GetComponentInChildren<EnemyPatrolModule>();
            var leap = root.GetComponentInChildren<EnemyLeapSkill>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var body = root.GetComponent<Rigidbody2D>();
            var animator = root.GetComponentInChildren<Animator>();
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            Check(enemy.DetectionRange == 8f && enemy.ChaseReleaseRange == 11f && health.CurrentHealth == 65f, "Incorrect stats");
            await Until(() => move.IsGrounded, 2f, "Not grounded");
            await Until(() => Mathf.Abs(body.position.x - patrol.Center.x) > 0.25f, 2f, "No patrol");
            target.transform.position = new Vector3(6f, 0f);
            await Until(() => body.linearVelocityX > 3.3f, 2f, "Failed to chase");
            await Until(() => body.position.x > 0.75f, 2f, "Chase did not move away from spawn");
            target.transform.position = new Vector3(18f, 0f);
            await Until(() => Mathf.Abs(body.position.x - patrol.Center.x) < 0.07f, 3f, "Failed to return to spawn");

            // Capacity checks can be observed before the FSM consumes the next frame.
            body.position = new Vector2(9.8f, 0.025f);
            body.linearVelocity = Vector2.zero;
            target.transform.position = new Vector3(12.55f, 0f);
            Physics2D.SyncTransforms();
            Check(!skills.CanAttackTarget(), "Leap accepted a void");

            body.position = new Vector2(0f, 0.025f);
            target.transform.position = new Vector3(0.8f, 0f);
            Physics2D.SyncTransforms();
            Check(leap.RemainingCoolTime == 0f && !skills.CanAttackTarget(), "Leap accepted a nearby target");
            await Until(() => body.linearVelocityX < -4f, 2f, "Enemy did not retreat with a ready leap");
            Check(!leap.IsPreparing, "Leap prepared before the minimum gap");
            await Until(() => leap.IsPreparing, 2f, "No leap after retreating");
            float firstPreparationDistance = Vector2.Distance(target.transform.position, root.transform.position);
            Check(firstPreparationDistance >= 2f && firstPreparationDistance <= 3.5f, "Leap prepared outside its allowed range");
            enemy.SetTarget(null);
            Check(!leap.IsUsing, "Clearing the target did not cancel preparation");
            warning = Object.FindAnyObjectByType<AttackTelegraphUI>();

            body.position = new Vector2(0f, 0.025f);
            body.linearVelocity = Vector2.zero;
            target.transform.position = new Vector3(2.75f, 0f);
            ceiling = MakeTerrain("Low ceiling", new Vector2(1.35f, 0.99f), new Vector2(3.2f, 0.14f));
            enemy.SetTarget(target.transform);
            Physics2D.SyncTransforms();
            Check(!skills.CanAttackTarget(), "Leap accepted a blocked path");
            ceiling.enabled = false;

            await Until(() => leap.IsPreparing, 2f, "No preparation on a valid path");
            warning = Object.FindAnyObjectByType<AttackTelegraphUI>();
            Check(warning != null && warning.IsVisible, "No warning at preparation start");
            ceiling.enabled = true;
            await Until(() => !leap.IsUsing, 1.2f, "New terrain did not cancel the leap");
            Check(!leap.ControlsMovement && float.IsNegativeInfinity(leap.CoolTimeStartTime) && probe.ReceivedHitCount == 0,
                "Cancelled leap started a cooldown or did damage");
            Check(!warning.IsVisible, "Cancelled leap left the warning visible");
            ceiling.enabled = false;

            target.transform.position = new Vector3(2.5f, 0f);
            float lockedX = target.transform.position.x;
            await Until(() => leap.IsPreparing, 2f, "No preparation after the cancelled leap");
            float preparationAt = Time.time;
            float crouchX = body.position.x;
            float gravity = body.gravityScale;
            var warningRect = warning.GetComponent<RectTransform>();
            Check(Mathf.Abs(warningRect.position.x - lockedX) < 0.05f &&
                Mathf.Abs(warningRect.sizeDelta.x - 1.7f) < 0.01f, "Landing warning bounds are incorrect");
            Canvas.ForceUpdateCanvases();
            var graphic = warning.GetComponentsInChildren<UnityEngine.UI.Graphic>().Single();
            Mesh circle = graphic.canvasRenderer.GetMesh();
            Check(graphic is EnemyLeapLandingGraphic && circle != null &&
                Mathf.Abs(circle.bounds.size.x - 1.7f) < 0.01f, "Landing circle did not render");
            target.transform.position = new Vector3(-9f, 0f);
            await Until(() => Time.time >= preparationAt + 0.24f, 1f, "Preparation too short");
            Check(Mathf.Abs(body.position.x - crouchX) < 0.07f &&
                animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LeapPreparation"), "Missing crouch or horizontal stop");
            Check(warning.IsVisible && Mathf.Abs(warningRect.position.x - lockedX) < 0.05f,
                "Warning followed the player during windup");
            await Until(() => leap.ControlsMovement, 2f, "No leap after crouch");
            float windup = Time.time - preparationAt;
            Check(Mathf.Abs(windup - 0.6f) < 0.08f && float.IsNegativeInfinity(leap.CoolTimeStartTime),
                "Wrong preparation or premature cooldown");
            target.transform.position = new Vector3(4.2f, 0f);
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.LeapFall"), 1f, "No fall animation");
            Check(warning.IsVisible && Mathf.Abs(warningRect.position.x - lockedX) < 0.05f,
                "Warning disappeared or moved during flight");
            await Until(() => leap.IsRecovering, 2f, "No actual landing");
            float firstLanding = body.position.x;
            float firstImpactAt = leap.CoolTimeStartTime;
            Check(Mathf.Abs(firstLanding - lockedX) < 0.18f && !warning.IsVisible && probe.ReceivedHitCount == 0 &&
                Mathf.Abs(body.gravityScale - gravity) < 0.01f && Time.time - firstImpactAt < 0.09f,
                $"Incorrect landing after player movement ({firstLanding:F3} vs {lockedX:F3})");
            Check(Object.FindObjectsByType<EnemyHitParticle>(FindObjectsSortMode.None)
                .Any(item => item.name.StartsWith("Enemy18_EffluentSplash", StringComparison.Ordinal)), "No landing effect");
            await Until(() => !leap.IsUsing, 2f, "No recovery exit");
            Check(Mathf.Abs(Time.time - firstImpactAt - 0.75f) < 0.09f, "Wrong recovery");

            await Until(() => body.linearVelocityX < -4f && body.position.x < firstLanding - 0.2f, 1f,
                "Enemy did not sprint away on cooldown");
            await Until(() => target.transform.position.x - body.position.x >= 7.9f, 2f, "No retreat distance reached");
            Check(target.transform.position.x - body.position.x <= 8.1f && leap.RemainingCoolTime > 0f,
                "Retreat left its configured distance or stopped early");
            await Until(() => leap.RemainingCoolTime <= 0f && body.linearVelocityX > 3.3f, 2f,
                "Enemy did not resume chasing when cooldown ended");
            await Until(() => leap.IsPreparing, 3f, "No second leap");
            await Until(() => leap.IsRecovering, 2f, "Second leap did not land");
            Check(probe.ReceivedHitCount == 1 && probe.TotalDamage == 12f, "Wrong landing hit");
            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (65f - 1000f / 102f)) < 0.01f &&
                reaction.RemainingReactionTime >= 0.249f && reaction.IsKnockedBack, "Wrong defense or hit reaction");

            var rewards = root.GetComponentInChildren<EnemyCurrencyRewardModule>();
            var gold = (CurrencySO)new SerializedObject(rewards).FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(enemy, 999f);
            Check(enemy.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "No death animation");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No death reward");
            int dropped = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(dropped >= 5 && dropped <= 8 && counter.GetKillCount() == 1, "Wrong gold or kill count");
            await Until(() => root == null && currency.Data.Count == dropped, 8f, "Death fade or gold collection failed");
            return new { hit = probe.TotalDamage, gold = currency.Data.Count, firstPreparationDistance, fixedAtWindup = true,
                warningThroughFlight = true, cooldownRetreat = true, blockedCeilingAndVoid = true, windup };
        }
        finally
        {
            foreach (GameObject item in new[] { root, target, counter.gameObject, floor.gameObject, ceiling?.gameObject })
                if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
            if (warning != null) Object.Destroy(warning.gameObject);
            ServiceLocator.Register<ICurrencyManager>(previousCurrency);
            Time.maximumDeltaTime = maxDelta;
            Application.runInBackground = background;
        }
    }

    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }

    private static BoxCollider2D MakeTerrain(string name, Vector2 position, Vector2 size)
    {
        var item = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        item.transform.position = position;
        var collider = item.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collider;
    }

    private static void Hit(EnemyBase enemy, float damage) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage }, (Vector2)enemy.transform.position + Vector2.up * 0.4f,
        Vector2.right, Vector2.left);

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
