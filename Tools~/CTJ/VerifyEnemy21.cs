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

public static class VerifyEnemy21
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Enemy/Enemy21/Enemy21_CutterAutomaton.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Check(root != null, "Missing Enemy21 prefab");
        foreach (var node in root.GetComponentsInChildren<Transform>(true))
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
        var dependencies = AssetDatabase.GetDependencies(Prefab);
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/")), "Prototype dependency");
        var skill = root.GetComponentInChildren<EnemyGroundDashSlashSkill>();
        var slot = root.GetComponentInChildren<EnemySkillModule>().Skills.Single();
        var settings = new SerializedObject(skill);
        Check(slot.Skill == skill && slot.MaxRange == 3.8f && EnemyGroundDashSlashSkill.AttackDistance == 3.8f &&
            skill.AttackDelay == 0.65f && skill.CoolTime == 3.4f &&
            settings.FindProperty("maximumConsecutiveDashes").intValue == 1 &&
            settings.FindProperty("telegraphPrefab").objectReferenceValue is AttackTelegraphUI, "Invalid dash or warning setup");
        Check(dependencies.Any(path => path.EndsWith("/HitMetalFragments.prefab")) &&
            dependencies.Any(path => path.EndsWith("/DeathMetalBurst.prefab")), "Missing metal effects");
        var clips = root.GetComponentInChildren<Animator>().runtimeAnimatorController.animationClips.Distinct().ToArray();
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 6 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, binding)
            .All(frame => frame.value is Sprite)), "Missing animation frames");
        return new { dependencies = dependencies.Length, clips = clips.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use isolated Play Mode");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = Terrain("Floor", new Vector2(0f, -0.1f), new Vector2(28f, 0.2f));
        var wall = Terrain("Wall", Vector2.zero, new Vector2(0.18f, 2f));
        wall.enabled = false;
        var target = new GameObject("Player probe");
        target.layer = LayerMask.NameToLayer("Player");
        var collider = target.AddComponent<BoxCollider2D>();
        collider.size = new Vector2(0.42f, 1f);
        collider.offset = Vector2.up * 0.5f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        target.transform.position = Vector3.right * 18f;
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), Vector3.up * 0.025f, Quaternion.identity);
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var body = root.GetComponent<Rigidbody2D>();
            var animator = root.GetComponentInChildren<Animator>();
            var dash = root.GetComponentInChildren<EnemyGroundDashSlashSkill>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            Check(health.CurrentHealth == 100f && enemy.DetectionRange == 9f && enemy.ChaseReleaseRange == 13f, "Wrong stats");
            await Until(() => Mathf.Abs(body.position.x) > 0.2f, 2f, "No patrol");
            target.transform.position = Vector3.right * 7f;
            await Until(() => body.position.x > 0.85f && Mathf.Abs(body.linearVelocityX - 3.4f) < 0.1f, 3f, "No chase");
            target.transform.position = Vector3.right * 18f;
            await Until(() => Mathf.Abs(body.position.x) < 0.08f, 4f, "No return to spawn");

            PlaceTarget(body.position.x + 2.2f, 0.9f);
            Check(!skills.CanAttackTarget(), "Attack accepted a high target");
            PlaceTarget(body.position.x + 3.85f, 0f);
            Check(!skills.CanAttackTarget(), "Attack accepted a target outside 3.8 units");
            PlaceTarget(body.position.x + 2.5f, 0f);
            wall.transform.position = new Vector3(body.position.x + 1.1f, 0.6f);
            wall.enabled = true;
            Physics2D.SyncTransforms();
            Check(!skills.CanAttackTarget(), "Attack accepted a blocked path");
            wall.enabled = false;
            await Until(() => dash.IsPreparing, 2f, "No preparation");
            float prepared = Time.time;
            float startX = body.position.x;
            var warning = root.GetComponentsInChildren<AttackTelegraphUI>().Single();
            var warningRect = warning.GetComponent<RectTransform>();
            Vector3 warningCenter = warningRect.position;
            float warningWidth = warningRect.sizeDelta.x;
            Check(warning.IsVisible && warningCenter.x > startX && Mathf.Abs(warningWidth - 3.8f) < 0.001f, "Wrong warning lane");
            float originalTargetX = target.transform.position.x;
            PlaceTarget(startX - 0.7f, 0f);
            await Until(() => Time.time >= prepared + 0.2f, 0.6f, "Preparation did not hold");
            Check(warning.IsVisible && Vector3.Distance(warningCenter, warningRect.position) < 0.05f &&
                Mathf.Abs(startX - body.position.x) < 0.05f, "Warning or facing moved during preparation");
            await Until(() => dash.ControlsMovement, 1f, "No locked dash");
            Check(!warning.IsVisible && Mathf.Abs(Time.time - prepared - 0.65f) < 0.08f, "Warning or windup timing wrong");
            PlaceTarget(originalTargetX, 1.05f);
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Slash"), 1f, "No slash animation");
            float firstEnd = body.position.x;
            Check(Mathf.Abs(firstEnd - startX - 3.8f) < 0.03f && probe.ReceivedHitCount == 0,
                "Dash changed direction or hit the jumping target");
            await Until(() => dash.IsRecovering, 1f, "No recovery");
            float recoveryStart = Time.time;
            await Awaitable.EndOfFrameAsync();
            Check(animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Recovery"), "Attack effect remained in recovery");
            await Until(() => !dash.IsUsing, 1.2f, "Recovery did not finish");
            Check(Mathf.Abs(Time.time - recoveryStart - 0.85f) < 0.08f && !warning.IsVisible, "Wrong single dash recovery");

            var settings = new SerializedObject(dash);
            settings.FindProperty("maximumConsecutiveDashes").intValue = 2;
            settings.ApplyModifiedPropertiesWithoutUndo();
            float previousSlash = dash.CoolTimeStartTime;
            PlaceTarget(body.position.x + 0.9f, 0f);
            await Until(() => dash.IsPreparing, 4.5f, "No combo after cooldown");
            Check(Time.time - previousSlash >= 3.399f, "Cooldown ended too early");
            startX = body.position.x;
            await Until(() => probe.ReceivedHitCount == 1, 2f, "No first combo strike");
            float firstHitAt = dash.CoolTimeStartTime;
            Check(dash.ControlsMovement && body.position.x < startX + 1.2f, "No early strike during movement");
            PlaceTarget(startX + 3.8f - 2.2f, 0f);
            await Until(() => dash.IsPreparing, 1f, "No second warning");
            Check(Mathf.Abs(body.position.x - startX - 3.8f) < 0.03f && warning.IsVisible &&
                Mathf.Abs(warningRect.sizeDelta.x - 3.8f) < 0.001f && warningRect.position.x < body.position.x,
                "First hit shortened travel or second warning did not use its new direction");
            float secondStart = body.position.x;
            await Until(() => probe.ReceivedHitCount == 2, 2f, "No second combo strike");
            Check(probe.TotalDamage == 32f && dash.CoolTimeStartTime > firstHitAt, "Wrong combo damage or last strike clock");
            await Until(() => dash.IsRecovering, 1f, "Combo did not end after two strikes");
            Check(Mathf.Abs(secondStart - body.position.x - 3.8f) < 0.03f && !warning.IsVisible,
                "Reverse hit shortened travel or left the warning visible");

            await Until(() => !dash.IsUsing, 1.2f, "Combo recovery stalled");
            settings.Update();
            settings.FindProperty("maximumConsecutiveDashes").intValue = 1;
            settings.ApplyModifiedPropertiesWithoutUndo();
            // 쿨타임 동안 위치를 유지해 벽을 추가하는 순간의 돌진 거리를 일정하게 합니다.
            PlaceTarget(18f, 0f);
            await Until(() => dash.RemainingCoolTime <= 0f, 4.5f, "No cooldown after combo");
            PlaceTarget(body.position.x + 2.5f, 0f);
            await Until(() => dash.IsPreparing, 4.5f, "No wall test preparation");
            startX = body.position.x;
            Check(warningRect.sizeDelta.x > 2f && warningRect.position.x > startX, "Wall test did not start with a forward dash");
            wall.transform.position = new Vector3(startX + 1.1f, 0.6f);
            wall.enabled = true;
            Physics2D.SyncTransforms();
            await Until(() => dash.IsRecovering || !dash.IsUsing, 2f, "Wall block did not finish attack");
            Check(dash.IsRecovering, "Wall block skipped recovery");
            Check(body.position.x > startX && body.position.x < startX + 0.82f &&
                probe.ReceivedHitCount == 2, $"Wrong wall stop: travelled {body.position.x - startX:F3}, hits {probe.ReceivedHitCount}");
            wall.enabled = false;

            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (100f - 1000f / 104f)) < 0.01f &&
                reaction.RemainingReactionTime >= 0.199f && Mathf.Abs(Mathf.Abs(body.linearVelocityX) - 1.625f) < 0.02f,
                "Wrong defense, stagger or knockback");
            var reward = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>());
            var gold = (CurrencySO)reward.FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(enemy, 999f);
            Check(enemy.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "No death pose");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No coins");
            int total = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(total >= 9 && total <= 14 && counter.GetKillCount() == 1, "Wrong reward");
            await Until(() => root == null && currency.Data.Count == total, 8f, "No death fade or gold collection");
            return new { lockedWarning = true, jumpDodged = true, dashStoppedAtWall = true, damage = probe.TotalDamage, gold = total };

            void PlaceTarget(float x, float y)
            {
                target.transform.position = new Vector3(x, y);
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
