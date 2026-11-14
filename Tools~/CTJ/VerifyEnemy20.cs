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
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemy20
{
    private const string PrefabPath = "Assets/_Prefabs/Enemies/Enemy/Enemy20/Enemy20_CeilingPatrol.prefab";

    public static object CheckAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "Missing Enemy20 prefab");
        foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
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
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/")), "Enemy20 references another prototype");
        var dash = prefab.GetComponentInChildren<EnemyFlightDashSkill>();
        var entry = prefab.GetComponentInChildren<EnemySkillModule>().Skills.Single();
        Check(entry.Skill == dash && entry.MaxRange == 2.4f && dash.AttackDelay == 0.6f && dash.CoolTime == 2.6f &&
            prefab.GetComponentInChildren<EnemyFlightModule>() != null &&
            prefab.GetComponentInChildren<EnemyMovementModule>() == null, "Invalid flight or dash setup");
        var clips = prefab.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Distinct().ToArray();
        var bodyBinding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 6 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, bodyBinding)
            .All(frame => frame.value is Sprite)), "Missing animation frames");
        var preparation = clips.Single(clip => clip.name.EndsWith("_DashPreparation", StringComparison.Ordinal));
        var sensorFrames = AnimationUtility.GetObjectReferenceCurve(preparation,
            EditorCurveBinding.PPtrCurve("Sensor", typeof(SpriteRenderer), "m_Sprite"));
        Check(sensorFrames.Length == 8 && sensorFrames[0].value is Sprite && sensorFrames[1].value == null,
            "Sensor does not blink");
        Check(new SerializedObject(dash).FindProperty("telegraphPrefab").objectReferenceValue == null, "Unexpected range UI");
        return new { animations = clips.Length, dependencies = dependencies.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use isolated Play Mode");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = Terrain("Floor", new Vector2(0f, -0.1f), new Vector2(32f, 0.2f));
        var wall = Terrain("Wall", Vector2.zero, new Vector2(0.18f, 2f));
        wall.enabled = false;
        var target = new GameObject("Player probe");
        var collider = target.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.42f, 1f);
        collider.offset = Vector2.up * 0.5f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        target.transform.position = Vector3.right * 18f;
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), Vector3.up * 3f, Quaternion.identity);
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var body = root.GetComponent<Rigidbody2D>();
            var animator = root.GetComponentInChildren<Animator>();
            var sensor = animator.transform.Find("Sensor").GetComponent<SpriteRenderer>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var dash = root.GetComponentInChildren<EnemyFlightDashSkill>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            Check(health.CurrentHealth == 55f && enemy.DetectionRange == 9f && enemy.ChaseReleaseRange == 12f &&
                root.GetComponentInChildren<EnemyMovementModule>() == null, "Wrong stats or movement module");
            await Until(() => Mathf.Abs(body.position.x) >= 0.3f, 2f, "No flying patrol");
            Check(Mathf.Abs(body.position.y - 3f) < 0.02f, "Patrol lost spawn altitude");
            target.transform.position = new Vector3(5f, 1f);
            await Until(() => body.position.x > 1f && body.position.y < 2.7f, 3f, "No altitude matching during chase");
            target.transform.position = Vector3.right * 18f;
            await Until(() => Vector2.Distance(body.position, Vector2.up * 3f) < 0.13f, 4f, "No return to spawn");

            body.position = new Vector2(0f, 3f);
            body.linearVelocity = Vector2.zero;
            PlaceTarget(1.8f, 3.72f);
            Check(!skills.CanAttackTarget(), "Dash started at the wrong height");
            PlaceTarget(2.55f, 3f);
            Check(!skills.CanAttackTarget(), "Dash started outside 2.4 units");
            PlaceTarget(1.8f, 3f);
            wall.transform.position = new Vector3(0.75f, 3f);
            wall.enabled = true;
            Physics2D.SyncTransforms();
            Check(!skills.CanAttackTarget(), "Dash accepted a blocked path");
            wall.enabled = false;
            await Until(() => dash.IsPreparing, 2f, "No dash preparation");
            float preparingAt = Time.time;
            Vector2 start = body.position;
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.DashPreparation") &&
                sensor.sprite != null, 0.25f, "Sensor never lit");
            await Until(() => sensor.sprite == null && dash.IsPreparing, 0.35f, "Sensor never blinked");
            Check(Vector2.Distance(start, body.position) < 0.06f &&
                root.GetComponentInChildren<AttackTelegraphUI>(true) == null, "Preparation moved or drew a range warning");
            PlaceTarget(start.x - 1.85f, start.y);
            await Until(() => dash.ControlsMovement, 1f, "Dash did not release");
            Check(Mathf.Abs(Time.time - preparingAt - 0.6f) < 0.08f && animator.transform.parent.localScale.x < 0f,
                "Dash did not use the direction at release");
            await Until(() => probe.ReceivedHitCount == 1, 1f, "No dash hit");
            float lastHit = dash.CoolTimeStartTime;
            PlaceTarget(start.x + 0.9f, start.y + 1.0f);
            var renderer = animator.GetComponent<SpriteRenderer>();
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Dash") &&
                renderer.sprite.name == "attack-fly_8", 0.8f, "Dash did not play through its last frame");
            await Until(() => dash.IsRecovering, 1f, "No dash recovery");
            float recoveryStartedAt = Time.time;
            float travel = start.x - body.position.x;
            Check(Mathf.Abs(travel - 2.4f) < 0.06f && Mathf.Abs(body.position.y - start.y) < 0.02f &&
                probe.TotalDamage == 11f, "Dash travel, height or damage was wrong");
            await Awaitable.EndOfFrameAsync();
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Recovery"), "No effect-free recovery pose");
            await Until(() => !dash.IsUsing, 1f, "Recovery did not finish");
            Check(Mathf.Abs(Time.time - recoveryStartedAt - 0.65f) < 0.08f &&
                Mathf.Abs(body.position.x - (start.x - travel)) < 0.06f, "Movement interrupted recovery");
            await Until(() => body.linearVelocity.x > 0.1f && body.linearVelocity.y > 0.1f, 1f, "No flight after recovery");
            Check(Time.time - lastHit < 2.6f && dash.RemainingCoolTime > 0f, "Cooldown did not start at strike");

            PlaceTarget(body.position.x + 1.85f, body.position.y);
            await Until(() => dash.IsPreparing, 4f, "No second dash after cooldown");
            Check(Time.time - lastHit >= 2.599f, "Cooldown ended too early");
            start = body.position;
            wall.transform.position = new Vector3(start.x + 0.7f, start.y);
            wall.enabled = true;
            Physics2D.SyncTransforms();
            await Until(() => dash.IsRecovering, 2f, "Dash hitting a wall did not recover");
            Check(body.position.x > start.x + 0.15f && body.position.x < start.x + 0.55f &&
                probe.ReceivedHitCount == 1, "Dash crossed a wall or hit through it");
            wall.enabled = false;

            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (55f - 1000f / 101f)) < 0.01f &&
                reaction.RemainingReactionTime >= 0.199f && Mathf.Abs(Mathf.Abs(body.linearVelocityX) - 2.25f) < 0.02f,
                "Wrong defense, stagger or knockback");
            var rewards = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>());
            var gold = (CurrencySO)rewards.FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(enemy, 999f);
            Check(enemy.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "No death animation");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No coins");
            int dropped = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(dropped >= 5 && dropped <= 8 && counter.GetKillCount() == 1, "Wrong reward or kill count");
            await Until(() => root == null && currency.Data.Count == dropped, 8f, "No death fade or gold collection");
            return new { damage = probe.TotalDamage, travel, keptAltitude = true, sensorBlink = true,
                wallStoppedDash = true, resumedFlight = true, gold = currency.Data.Count };

            void PlaceTarget(float centerX, float centerY)
            {
                target.transform.position = new Vector3(centerX, centerY - collider.offset.y);
                Physics2D.SyncTransforms();
            }
        }
        finally
        {
            foreach (var item in new[] { root, target, floor.gameObject, wall.gameObject, counter.gameObject })
                if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
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

    private static BoxCollider2D Terrain(string name, Vector2 position, Vector2 size)
    {
        var item = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        item.transform.position = position;
        var collider = item.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collider;
    }

    private static void Hit(EnemyBase enemy, float damage) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage }, enemy.transform.position, Vector2.right, Vector2.left);

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
