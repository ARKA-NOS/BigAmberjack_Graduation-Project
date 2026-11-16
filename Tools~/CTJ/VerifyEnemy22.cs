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

public static class VerifyEnemy22
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Enemy/Enemy22/Enemy22_LoadingCrusher.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Prefab);
        Check(root != null, "Missing Enemy22 prefab");
        var dependencies = AssetDatabase.GetDependencies(Prefab);
        Check(dependencies.All(path => !path.Contains("/Legacy/") && !path.Contains("/Enemy17/") &&
            !path.Contains("/Enemy18/") && !path.Contains("Enemy17_IntakeBiter")), "Prototype dependency");
        foreach (string path in dependencies.Where(path => path.Contains("/Enemy22/") && path.EndsWith(".prefab")))
        foreach (var node in AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponentsInChildren<Transform>(true))
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
        var skills = root.GetComponentInChildren<EnemySkillModule>().Skills;
        Check(skills.Count == 2 && skills[0].Skill is EnemyTimedMeleeSkill crush && skills[1].Skill is EnemyLobSkill toss &&
            crush.AttackDelay == 0.75f && toss.AttackDelay == 0.75f &&
            crush.CoolTime == 2.8f && toss.CoolTime == 4.6f && crush.SkillSo != toss.SkillSo, "Invalid skill slots");
        var animator = root.GetComponentInChildren<Animator>();
        Check(!animator.GetComponent<SpriteRenderer>().flipX, "Duskclaw's right-facing sprite was mirrored");
        var clips = animator.runtimeAnimatorController.animationClips;
        var binding = EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");
        Check(clips.Length == 8 && clips.All(clip => AnimationUtility.GetObjectReferenceCurve(clip, binding)
            .All(key => key.value is Sprite)), "Missing animation frames");
        var scrap = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemies/Enemy/Enemy22/Enemy22_Scrap.prefab");
        var scrapSprite = scrap.GetComponentInChildren<SpriteRenderer>();
        Check(scrapSprite.sprite != null && scrapSprite.transform.localScale == new Vector3(1.3f, 1.3f, 1f),
            "Missing or incorrectly sized scrap sprite");
        return new { dependencies = dependencies.Length, clips = clips.Length };
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && string.IsNullOrEmpty(SceneManager.GetActiveScene().path), "Use isolated Play Mode");
        bool background = Application.runInBackground;
        float maxDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var floor = Terrain("Floor", new Vector2(0f, -0.1f), new Vector2(48f, 0.2f));
        var wall = Terrain("Wall", new Vector2(2f, 1.8f), new Vector2(0.2f, 3.6f));
        var platform = Terrain("Platform", new Vector2(-4f, 0.25f), new Vector2(2f, 0.2f));
        platform.gameObject.layer = LayerMask.NameToLayer("Platform");
        wall.enabled = platform.enabled = false;
        var target = new GameObject("Player probe");
        var targetBody = target.AddComponent<BoxCollider2D>();
        targetBody.isTrigger = true;
        targetBody.size = new Vector2(0.42f, 1f);
        targetBody.offset = Vector2.up * 0.5f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        var counter = new GameObject("Kill counter").AddComponent<EnemyKillCountManager>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Prefab), Vector3.up * 0.025f, Quaternion.identity);
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.04f;
            var enemy = root.GetComponent<EnemyBase>();
            var body = root.GetComponent<Rigidbody2D>();
            var animator = root.GetComponentInChildren<Animator>();
            var crush = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
            var toss = root.GetComponentInChildren<EnemyLobSkill>();
            var throwPoint = (Transform)new SerializedObject(toss).FindProperty("throwPoint").objectReferenceValue;
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            var health = root.GetComponentInChildren<NewHealthModule>();
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            PlaceTarget(20f);
            Check(health.CurrentHealth == 170f && enemy.DetectionRange == 10f && enemy.ChaseReleaseRange == 14f, "Wrong stats");
            await Until(() => Mathf.Abs(body.position.x) > 0.2f, 2f, "No patrol");
            PlaceTarget(8.5f);
            await Until(() => body.position.x > 0.8f && Mathf.Abs(body.linearVelocityX - 2.2f) < 0.1f, 3f, "No chase");
            PlaceTarget(20f);
            await Until(() => Mathf.Abs(body.position.x) < 0.07f, 4f, "No return to spawn");

            wall.transform.position = new Vector3(body.position.x + 2f, 1.8f);
            wall.enabled = true;
            PlaceTarget(body.position.x + 4f);
            Check(!skills.CanAttackTarget(), "Arc crossed wall");
            wall.enabled = false;
            PlaceTarget(body.position.x + 2.1f);
            Check(!skills.CanAttackTarget(), "Attack accepted the gap between crush and throw");
            PlaceTarget(body.position.x + 1.2f);
            await Until(() => crush.IsPreparing, 2f, "No crush preparation");
            float prepared = Time.time;
            var crushWarning = root.GetComponentsInChildren<AttackTelegraphUI>().Single();
            var crushRect = crushWarning.GetComponent<RectTransform>();
            Check(crushWarning.IsVisible && crushRect.position.x > body.position.x &&
                Mathf.Abs(crushRect.sizeDelta.x - 1.7f) < 0.01f &&
                animator.transform.TransformVector(Vector3.right).x > 0f && !animator.GetComponent<SpriteRenderer>().flipX,
                "Wrong crush area or visual direction");
            await Until(() => probe.ReceivedHitCount == 1, 1.2f, "No crush damage");
            Check(probe.TotalDamage == 20f && Mathf.Abs(crush.CoolTimeStartTime - prepared - 0.75f) < 0.08f &&
                !crushWarning.IsVisible, "Wrong crush damage or release time");
            await Until(() => crush.IsRecovering, 1f, "No crush recovery");
            Check(Time.time - crush.CoolTimeStartTime >= 0.35f, "Crush clip was cut short");
            float recoveryAt = Time.time;
            float landingX = body.position.x + 4.1f;
            PlaceTarget(landingX);
            await Until(() => toss.IsPreparing, 2f, "Throw did not follow crush recovery");
            Check(Time.time - recoveryAt >= 0.99f && !crush.IsUsing && probe.ReceivedHitCount == 1, "Attacks overlapped");

            prepared = Time.time;
            var marker = root.GetComponentsInChildren<AttackTelegraphUI>().Single(item => item.IsVisible);
            var rect = marker.GetComponent<RectTransform>();
            Vector2 locked = rect.position;
            Check(Mathf.Abs(locked.x - landingX) < 0.06f && Mathf.Abs(locked.y) < 0.01f &&
                Mathf.Abs(rect.sizeDelta.x - 2.4f) < 0.01f, "Wrong landing warning");
            PlaceTarget(landingX + 2f);
            await Until(() => Object.FindAnyObjectByType<EnemyLobProjectile>() != null, 1.2f, "Throw did not launch");
            var projectile = Object.FindAnyObjectByType<EnemyLobProjectile>();
            Check(marker.IsVisible && Vector2.Distance(rect.position, locked) < 0.03f &&
                Mathf.Abs(toss.CoolTimeStartTime - prepared - 0.75f) < 0.08f, "Throw retargeted or started cooldown early");
            var visual = projectile.GetComponentInChildren<SpriteRenderer>().transform;
            await Until(() => Time.time - toss.CoolTimeStartTime >= 0.23f, 1f, "Projectile did not move");
            Check(visual.position.x > projectile.transform.position.x + 0.7f &&
                visual.position.y > projectile.transform.position.y + 0.25f &&
                Vector2.Distance(rect.position, locked) < 0.03f, "Scrap did not arc independently of its warning");
            await Until(() => toss.IsRecovering, 1.2f, "No throw recovery");
            Check(Time.time - toss.CoolTimeStartTime >= 0.49f, "Throw clip was cut short");
            await Until(() => projectile == null, 1.2f, "Projectile did not land");
            Check(probe.ReceivedHitCount == 1 && marker == null, "Throw hit outside its warning or left the marker");

            PlaceTarget(20f);
            await Until(() => !toss.IsUsing && toss.RemainingCoolTime <= 0f, 5.5f, "Throw cooldown or recovery stalled");
            await Until(() => Mathf.Abs(body.position.x) < 0.07f, 4f, "No return after throw");
            platform.enabled = true;
            PlaceTarget(-4f, 0.35f);
            await Until(() => toss.IsPreparing, 2f, "No left-facing platform throw");
            marker = root.GetComponentsInChildren<AttackTelegraphUI>().Single(item => item.IsVisible);
            Check(Mathf.Abs(marker.transform.position.y - 0.35f) < 0.03f && throwPoint.position.x < body.position.x &&
                animator.transform.TransformVector(Vector3.right).x < 0f && !animator.GetComponent<SpriteRenderer>().flipX,
                "Wrong landing height or left-facing throw direction");
            var secondHurtbox = new GameObject("Extra target collider").AddComponent<BoxCollider2D>();
            secondHurtbox.transform.SetParent(target.transform, false);
            secondHurtbox.isTrigger = true;
            secondHurtbox.size = targetBody.size;
            secondHurtbox.offset = targetBody.offset;
            await Until(() => Object.FindAnyObjectByType<EnemyLobProjectile>() != null, 1.2f, "No platform projectile");

            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            Hit(enemy, 10f);
            Check(Mathf.Abs(health.CurrentHealth - (170f - 1000f / 106f)) < 0.01f &&
                reaction.RemainingReactionTime >= 0.149f && Mathf.Abs(Mathf.Abs(body.linearVelocityX) - 0.875f) < 0.02f,
                "Wrong defense, stagger or knockback");
            var reward = new SerializedObject(root.GetComponentInChildren<EnemyCurrencyRewardModule>());
            var gold = (CurrencySO)reward.FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            Hit(enemy, 999f);
            Check(enemy.IsDead && marker.IsVisible, "Death removed fired projectile or warning");
            await Until(() => animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), 1f, "No immediate death animation");
            await Until(() => probe.ReceivedHitCount == 2, 1.5f, "No single platform impact after shooter died");
            Check(probe.TotalDamage == 36f, "Wrong throw damage");
            await Until(() => Object.FindAnyObjectByType<EnemyRewardCoin>() != null, 2f, "No coins");
            int total = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Sum(coin => coin.Amount);
            Check(total >= 16 && total <= 24 && counter.GetKillCount() == 1, "Wrong coins or kill count");
            await Until(() => root == null && currency.Data.Count == total, 8f, "Death fade or gold collection did not finish");
            Check(probe.ReceivedHitCount == 2, "Duplicate hit from an extra target collider");
            return new { hitCount = probe.ReceivedHitCount, damage = probe.TotalDamage, gold = total, platformImpact = true, wallBlocked = true };

            void PlaceTarget(float x, float y = 0f)
            {
                target.transform.position = new Vector3(x, y);
                Physics2D.SyncTransforms();
            }
        }
        finally
        {
            foreach (var item in new[] { root, target, floor.gameObject, wall.gameObject, platform.gameObject, counter.gameObject })
                if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
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
