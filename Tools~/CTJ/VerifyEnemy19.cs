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
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemy19
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Enemy/Enemy19/Enemy19_OverheatedWorker.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Check(root != null, "Missing Enemy19 prefab");
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
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/")), "Old enemy dependencies");
        var entries = root.GetComponentInChildren<EnemySkillModule>().Skills;
        Check(entries.Count == 2 && entries.All(entry => entry.Skill is EnemyTimedMeleeSkill) &&
            entries.Select(entry => entry.Skill.SkillSo).Distinct().Count() == 2, "Incorrect skill slots");
        var controller = (AnimatorController)root.GetComponentInChildren<Animator>().runtimeAnimatorController;
        var clips = controller.animationClips.Distinct().ToArray();
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 8 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, binding)
            .All(frame => frame.value is Sprite)), "Missing animation sprites");
        var states = controller.layers[0].stateMachine.states;
        Check(states.Single(item => item.state.name == "Recovery").state.motion ==
            states.Single(item => item.state.name == "Idle").state.motion, "Recovery needs the effect-free motion");
        foreach (var entry in entries)
        {
            var skill = new SerializedObject(entry.Skill);
            Check(skill.FindProperty("recoveryStateName").stringValue == "Base Layer.Recovery" &&
                skill.FindProperty("telegraphPrefab").objectReferenceValue == null, "Wrong melee presentation");
        }
        return new { dependencies = dependencies.Length, skills = entries.Count, animations = clips.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use isolated Play Mode");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = Terrain("Floor", new Vector2(0f, -0.1f), new Vector2(32f, 0.2f));
        var wall = Terrain("Wall", Vector2.zero, Vector2.one);
        wall.enabled = false;
        var target = new GameObject("Player probe");
        var collider = target.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.4f, 0.75f);
        collider.offset = Vector2.up * 0.375f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        target.transform.position = Vector3.right * 13f;
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), Vector3.up * 0.025f, Quaternion.identity);
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var body = root.GetComponent<Rigidbody2D>();
            var mover = root.GetComponentInChildren<EnemyMovementModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            var animator = root.GetComponentInChildren<Animator>();
            var spriteRenderer = animator.GetComponent<SpriteRenderer>();
            var forwardHitbox = root.GetComponentsInChildren<BoxCollider2D>().Single(box => box.name == "ForwardHitbox");
            var skills = root.GetComponentInChildren<EnemySkillModule>().Skills;
            var upward = (EnemyTimedMeleeSkill)skills[0].Skill;
            var forward = (EnemyTimedMeleeSkill)skills[1].Skill;
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            Check(health.CurrentHealth == 70f && enemy.DetectionRange == 8.5f && enemy.ChaseReleaseRange == 12f, "Wrong stats");
            await Until(() => mover.IsGrounded, 2f, "No ground contact");
            await Until(() => Mathf.Abs(body.position.x) > 0.25f, 2f, "No patrol");
            target.transform.position = Vector3.right * 6f;
            await Until(() => body.position.x > 1.1f && Mathf.Abs(body.linearVelocityX - 2.8f) < 0.1f, 3f, "No chase");
            target.transform.position = Vector3.right * 18f;
            await Until(() => Mathf.Abs(body.position.x) < 0.08f, 3f, "No return to spawn");

            PositionTarget(0.7f, 1.3f);
            Check(!upward.CanUseSkill(), "Target outside the upward horizontal radius was accepted");
            PositionTarget(0f, 2.5f);
            Check(!upward.CanUseSkill(), "Target above the upward height was accepted");
            PositionTarget(0f, 0f);
            Check(!upward.CanUseSkill(), "Grounded target was accepted for upward attack");

            PositionTarget(-1.05f, 0f);
            await Until(() => forward.IsPreparing, 2f, "No forward preparation");
            float prepared = Time.time;
            Physics2D.SyncTransforms();
            Check(forwardHitbox.bounds.center.x < body.position.x - 0.6f &&
                root.GetComponentInChildren<AttackTelegraphUI>(true) == null, "Wrong forward facing or unexpected range warning");
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.ForwardPreparation"), 0.4f, "Wrong forward pose");
            await Until(() => probe.ReceivedHitCount == 1, 1f, "No forward strike");
            float firstStrike = forward.CoolTimeStartTime;
            Check(Mathf.Abs(firstStrike - prepared - 0.35f) < 0.08f && probe.TotalDamage == 10f &&
                Mathf.Abs(forward.CoolTime - 1.8f) < 0.01f, "Wrong forward timing, damage or cooldown");

            PositionTarget(0f, 1.7f);
            await CheckRecovery(forward, "ForwardStrike");
            float recovery = Time.time;
            Check(!upward.IsUsing, "Upward attack interrupted recovery");
            await Until(() => upward.IsPreparing, 2f, "No upward strike after recovery");
            Check(Time.time - recovery >= 0.62f, "Next attack cut off the recovery");
            prepared = Time.time;
            Check(root.GetComponentInChildren<AttackTelegraphUI>(true) == null, "Unexpected upward range warning");
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.UpwardPreparation"), 0.4f, "Wrong upward pose");
            await Until(() => probe.ReceivedHitCount == 2, 1f, "No upward hit");
            Check(probe.TotalDamage == 22f && Mathf.Abs(upward.CoolTimeStartTime - prepared - 0.35f) < 0.08f &&
                Mathf.Abs(upward.CoolTime - 2.4f) < 0.01f, "Wrong upward timing, damage or cooldown");
            await CheckRecovery(upward, "UpwardStrike");

            PositionTarget(1.15f, 0f);
            await Until(() => forward.IsPreparing, 3f, "Forward never became ready again");
            Physics2D.SyncTransforms();
            Check(Time.time - firstStrike >= 1.799f && forwardHitbox.bounds.center.x > body.position.x,
                "Forward cooldown or right facing is wrong");
            wall.transform.position = new Vector2(body.position.x + 0.6f, 0.48f);
            wall.size = new Vector2(0.15f, 0.92f);
            wall.enabled = true;
            Physics2D.SyncTransforms();
            await Until(() => forward.IsRecovering, 1.5f, "Blocked forward strike did not recover");
            Check(probe.ReceivedHitCount == 2, "Forward strike damaged player through a wall");
            wall.enabled = false;

            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (70f - 1000f / 103f)) < 0.01f &&
                reaction.RemainingReactionTime >= 0.249f && Mathf.Abs(Mathf.Abs(body.linearVelocityX) - 2.125f) < 0.02f,
                "Wrong defense, stagger or knockback");
            var rewards = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>());
            var gold = (CurrencySO)rewards.FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(enemy, 999f);
            Check(enemy.IsDead && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "No death animation");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No coins");
            int dropped = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(dropped >= 6 && dropped <= 10 && counter.GetKillCount() == 1, "Wrong reward or kill count");
            await Until(() => root == null && currency.Data.Count == dropped, 8f, "No death fade or gold collection");
            return new { forwardDamage = 10, upwardDamage = 12, fullStrikeClips = true, recoveryWithoutStrikeSprites = true,
                noRangeWarning = true, blockedByWall = true, gold = currency.Data.Count };

            async Task CheckRecovery(EnemyTimedMeleeSkill skill, string strikeState)
            {
                AnimationClip clip = animator.runtimeAnimatorController.animationClips
                    .Single(item => item.name.EndsWith("_" + strikeState, StringComparison.Ordinal));
                var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
                var lastFrame = AnimationUtility.GetObjectReferenceCurve(clip, binding).Last().value;
                bool sawLastFrame = false;
                float deadline = Time.realtimeSinceStartup + 1f;
                do
                {
                    await Awaitable.EndOfFrameAsync();
                    var state = animator.GetCurrentAnimatorStateInfo(0);
                    sawLastFrame |= state.IsName("Base Layer." + strikeState) && spriteRenderer.sprite == lastFrame;
                } while (!skill.IsRecovering && Time.realtimeSinceStartup < deadline);
                Check(skill.IsRecovering && sawLastFrame && Time.time - skill.CoolTimeStartTime >= clip.length - 0.015f,
                    strikeState + " ended before the full strike animation played");
                Check(skill.IsUsing && animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Recovery") &&
                    spriteRenderer.sprite.name.StartsWith("idle_", StringComparison.Ordinal), "Strike effect remained in recovery");
            }

            void PositionTarget(float x, float y)
            {
                target.transform.position = new Vector3(body.position.x + x, y);
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
