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
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemy17
{
    private const string PrefabPath = "Assets/_Prefabs/Enemies/Enemy/Enemy17/Enemy17_IntakeBiter.prefab";

    public static object CheckAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(prefab != null, "Prefab not found");
        int references = 0;
        foreach (var node in prefab.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(node.gameObject) == 0,
                "Missing script on " + node.name);
            foreach (var component in node.GetComponents<Component>())
            {
                var properties = new SerializedObject(component).GetIterator();
                while (properties.Next(true))
                {
                    if (properties.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Check(properties.objectReferenceValue != null || properties.objectReferenceInstanceIDValue == 0,
                        $"Broken reference {node.name}/{properties.propertyPath}");
                    if (properties.objectReferenceValue != null) references++;
                }
            }
        }
        Check(AssetDatabase.GetDependencies(PrefabPath).All(path => !path.Contains("/Legacy/")), "Active prefab depends on Legacy");
        var skill = prefab.GetComponentInChildren<EnemyTimedMeleeSkill>();
        var module = prefab.GetComponentInChildren<EnemySkillModule>();
        Check(module.Skills.Count == 1 && module.Skills[0].Skill == skill && skill.SkillSo != null &&
            Mathf.Approximately(module.Skills[0].MaxRange, 1.2f), "Incorrect bite registration");
        var animator = prefab.GetComponentInChildren<Animator>();
        var controller = (AnimatorController)animator.runtimeAnimatorController;
        var states = controller.layers[0].stateMachine.states.Select(item => item.state.name).ToArray();
        Check(states.Length == 6 && controller.animationClips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip,
            EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite")).All(frame => frame.value is Sprite)),
            "Missing frames or states");
        var grille = animator.transform.Find("IntakeGrille").GetComponent<SpriteRenderer>();
        Check(grille.sprite != null && grille.sprite.texture != null, "Missing intake grille sprite");
        return new { references, states, dependencies = AssetDatabase.GetDependencies(PrefabPath).Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode scene required");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var originalCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = new GameObject("Verification floor") { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = new Vector3(0f, -0.1f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(60f, 0.2f);
        var target = new GameObject("Player probe");
        target.transform.position = Vector3.right * 12f;
        var collider = target.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.6f, 1.6f);
        collider.offset = Vector2.up * 0.8f;
        collider.isTrigger = true;
        var probe = target.AddComponent<EnemyDamageProbe>();
        var killCount = new GameObject("Verification kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath), new Vector3(0f, 0.03f), Quaternion.identity);

        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var body = root.GetComponent<Rigidbody2D>();
            var patrol = root.GetComponentInChildren<EnemyPatrolModule>();
            var movement = root.GetComponentInChildren<EnemyMovementModule>();
            var skill = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var animator = root.GetComponentInChildren<Animator>();
            var grille = animator.transform.Find("IntakeGrille").GetComponent<SpriteRenderer>();
            var facing = animator.transform.parent;
            root.GetComponentInChildren<EnemyHitCameraShakeModule>()?.gameObject.SetActive(false);
            enemy.SetTarget(target.transform);
            Check(health.CurrentHealth == 45f && health.MaxHealth == 45f, "Incorrect max/start HP");
            Check(enemy.DetectionRange == 7f && enemy.ChaseReleaseRange == 10f, "Incorrect chase thresholds");
            await Until(() => movement.IsGrounded, 3f, "No ground contact");

            target.transform.position = Vector3.right * 5f;
            await Until(() => body.position.x > 2f, 4f, "Failed to chase inside detection distance");
            Check(Mathf.Abs(body.linearVelocityX - 2.8f) < 0.08f, "Incorrect chase speed");
            target.transform.position = Vector3.right * 11f;
            float chaseX = body.position.x;
            await Until(() => body.position.x > chaseX + 0.18f, 1.5f, "Lost target before the release threshold");
            target.transform.position = Vector3.right * 15f;
            await Until(() => Mathf.Abs(body.position.x - patrol.Center.x) <= 0.07f, 4f, "Did not return to the spawn point");
            await Until(() => Mathf.Abs(body.position.x - patrol.Center.x) >= 0.4f, 5f, "Did not resume patrol");

            target.transform.position = new Vector3(body.position.x + 0.96f, 0f);
            await Until(() => skill.IsPreparing, 2f, "Bite did not start in range");
            Check(facing.localScale.x > 0f && float.IsNegativeInfinity(skill.CoolTimeStartTime), "Incorrect initial facing or premature cooldown");
            float preparationAt = Time.time;
            target.transform.position = new Vector3(body.position.x - 0.82f, 0f);
            await Until(() => skill.IsRecovering, 2f, "Missed bite did not recover");
            float strikeAt = skill.CoolTimeStartTime;
            Check(Mathf.Abs(strikeAt - preparationAt - 0.45f) < 0.075f && facing.localScale.x > 0f &&
                probe.ReceivedHitCount == 0 && Mathf.Abs(body.linearVelocityX) < 0.01f,
                "Windup was incorrect or moving behind did not avoid the hit");
            float recoveryAt = Time.time;
            await Until(() => !skill.IsUsing, 2f, "Bite did not finish recovery");
            Check(Mathf.Abs(Time.time - recoveryAt - 0.65f) < 0.08f, "Incorrect recovery duration");
            await Until(() => skill.IsPreparing, 3f, "Bite did not restart after cooldown");
            Check(Time.time - strikeAt >= 1.6f && facing.localScale.x < 0f, "Cooldown or left-facing failed");
            await Until(() => skill.IsRecovering, 2f, "Second bite did not recover");
            Check(probe.ReceivedHitCount == 1 && probe.TotalDamage == 8f, "Bite did not hit exactly once for 8 damage");

            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (45f - 1000f / 101f)) < 0.01f && reaction.IsBlinking &&
                reaction.IsKnockedBack && reaction.RemainingReactionTime >= 0.249f, "Defense/stagger/knockback/blink failed");
            await Until(() => !enemy.IsHitReacting && !skill.IsUsing, 2f, "Attack or reaction never ended");
            Hit(enemy, 1f);
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Hurt"), 0.7f, "Hurt state did not play");
            Check(grille.sprite == null, "The intake grille covered the hurt sprite");

            var reward = root.GetComponentInChildren<EnemyCurrencyRewardModule>();
            var gold = (CurrencySO)new SerializedObject(reward).FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            int deaths = 0;
            enemy.DeathStarted += () => deaths++;
            Hit(enemy, 999f);
            Check(enemy.IsDead && deaths == 1 && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"),
                "Death did not play on the lethal frame");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No coins after death reaction");
            var coins = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
            int dropped = coins.Sum(coin => coin.Amount);
            Check(dropped >= 3 && dropped <= 5 && coins.Length == (dropped + 2) / 3, "Incorrect coin value or split");
            Check(killCount.GetKillCount() == 1, "Incorrect kill count");
            await Until(() => root == null && currency.Data.Count == dropped, 9f, "Death fade or coin pickup failed");
            return new { hp = 45, chased = true, returned = true, facingLocked = true, damage = probe.TotalDamage,
                cooldown = 1.6f, gold = currency.Data.Count, deaths };
        }
        finally
        {
            Application.runInBackground = background;
            Time.maximumDeltaTime = maxDelta;
            ServiceLocator.Register<ICurrencyManager>(originalCurrency);
            foreach (var item in new[] { root, target, floor, killCount.gameObject }) if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
        }
    }

    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }

    private static void Hit(EnemyBase enemy, float damage) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage }, (Vector2)enemy.transform.position + Vector2.up * 0.4f,
        Vector2.right, Vector2.left);

    private static async Task Until(Func<bool> condition, float seconds, string error)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), error);
    }

    private static void Check(bool condition, string error)
    {
        if (!condition) throw new InvalidOperationException(error);
    }
}
