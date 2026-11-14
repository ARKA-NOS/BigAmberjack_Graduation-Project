using System;
using System.Linq;
using System.Threading.Tasks;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Currency;
using _Scripts.DevLib.BattleSystem;
using _Scripts.Testing.Enemy;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemy05
{
    private const string SheetPath = "Assets/_Graphics/Assets/Enemy/Legacy/ai_created_shieldman.png";
    private const string PrefabPath = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy05/Enemy05_Shieldbearer.prefab";

    private static void Check(bool valid, string message)
    {
        if (!valid) throw new InvalidOperationException(message);
    }

    public static object Assets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        Check(root != null && root.GetComponent<EnemyShieldbearer>() != null, "Shieldbearer prefab missing");
        var turn = root.GetComponentInChildren<EnemyTurnDelayModule>();
        Check(turn != null && turn.enabled && new SerializedObject(turn).FindProperty("turnDelay").floatValue > 0f,
            "Shieldbearer turn delay module missing or inactive");
        var sprites = AssetDatabase.LoadAllAssetsAtPath(SheetPath).OfType<Sprite>().ToArray();
        Check(sprites.Length == 29 && sprites.All(sprite => sprite.rect.width > 60 && sprite.rect.height > 40 &&
            sprite.pivot.y < 6f && sprite.pivot.x > 0f && sprite.pivot.x < sprite.rect.width &&
            Mathf.Approximately(sprite.pixelsPerUnit, 86f)), "Invalid shieldbearer sprite rectangles or pivots");
        var animator = root.GetComponentInChildren<Animator>();
        var controller = (AnimatorController)animator.runtimeAnimatorController;
        var states = controller.layers[0].stateMachine.states;
        Check(states.Length == 8, "Expected eight animation states");
        foreach (var state in states)
        {
            var clip = (AnimationClip)state.state.motion;
            var binding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, binding);
            Check(keys.Length >= 2 && keys.All(key => key.value is Sprite sprite && sprites.Contains(sprite)),
                "A clip is missing a shieldbearer sprite reference: " + clip.name);
        }
        int references = 0;
        foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0,
                "Missing script on " + child.name);
            foreach (Component component in child.GetComponents<Component>())
            {
                using var data = new SerializedObject(component);
                using var property = data.GetIterator();
                while (property.Next(true))
                {
                    if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                    Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                        "Broken reference: " + child.name + "/" + property.propertyPath);
                    if (property.objectReferenceValue != null) references++;
                }
            }
        }
        var skills = root.GetComponentInChildren<EnemySkillModule>();
        Check(skills.Skills.Count == 2 && skills.Skills[0].Skill is EnemyShieldBashSkill &&
            skills.Skills[1].Skill is EnemyShieldChargeSkill &&
            skills.Skills.All(entry => entry.Skill.enabled && entry.Skill.SkillSo != null) &&
            skills.Skills[0].Skill.SkillSo != skills.Skills[1].Skill.SkillSo, "Invalid skill entries");
        return new { passed = true, slices = sprites.Length, animationStates = states.Length, references };
    }

    private static async Task Until(Func<bool> condition, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    private static async Task Delay(float seconds)
    {
        float end = Time.time + seconds;
        await Until(() => Time.time >= end, seconds + 3f, "Game clock stopped");
    }

    private static BoxCollider2D Box(string name, Vector2 position, Vector2 size)
    {
        var obj = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        obj.transform.position = position;
        var collider = obj.AddComponent<BoxCollider2D>();
        collider.size = size;
        return collider;
    }

    private static void IncomingHit(EnemyShieldbearer enemy, float side, float damage)
    {
        Vector2 point = (Vector2)enemy.transform.position + new Vector2(side * 0.55f, 1f);
        enemy.ApplyDamage(new DamageData { DamageAmount = damage }, point, Vector2.left * side, Vector2.right * side);
    }

    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }

    public static async Task<object> RunTurnDelay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        bool previousBackground = Application.runInBackground;
        Application.runInBackground = true;
        var floor = Box("Shield Turn Floor", new Vector2(0f, -0.5f), new Vector2(40f, 1f));
        var target = new GameObject("Shield Turn Target");
        var collider = target.AddComponent<BoxCollider2D>();
        collider.isTrigger = true;
        collider.size = new Vector2(0.6f, 1.8f);
        collider.offset = Vector2.up * 0.9f;
        var probe = target.AddComponent<EnemyDamageProbe>();
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),
            new Vector3(0f, 0.025f), Quaternion.identity);
        try
        {
            var enemy = root.GetComponent<EnemyShieldbearer>();
            var body = root.GetComponent<Rigidbody2D>();
            var move = root.GetComponentInChildren<EnemyMovementModule>();
            var bash = root.GetComponentInChildren<EnemyShieldBashSkill>();
            var charge = root.GetComponentInChildren<EnemyShieldChargeSkill>();
            var turn = root.GetComponentInChildren<EnemyTurnDelayModule>();
            var facing = (Transform)new SerializedObject(enemy).FindProperty("facingRoot").objectReferenceValue;
            var health = enemy.GetModule<INewHealthModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            root.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            root.GetComponentInChildren<EnemyDamageIndicatorModule>().enabled = false;
            await Until(() => move.IsGrounded, 2f, "Shieldbearer did not land");

            float configuredDelay = new SerializedObject(turn).FindProperty("turnDelay").floatValue;
            PlaceTarget(8f);
            enemy.SetTarget(target.transform);
            await Until(() => body.linearVelocityX > 0f, 1f, "Forward chase did not start");
            PlaceTarget(-8f);
            await Delay(configuredDelay * 0.5f);
            Check(facing.localScale.x > 0f && Mathf.Abs(body.linearVelocityX) < 0.01f,
                "Chase turned or walked backward during the turn delay");
            PlaceTarget(8f);
            await Until(() => body.linearVelocityX > 0f, 1f, "Chase did not resume when target crossed back");

            PlaceTarget(-1.4f);
            float crossedAt = Time.time;
            await Delay(configuredDelay * 0.75f);
            Check(facing.localScale.x > 0f && !bash.IsUsing && !charge.IsUsing,
                "Brief crossing counted toward the next turn or an attack bypassed the delay");
            await Until(() => facing.localScale.x < 0f, 1f, "Sustained crossing did not flip the visual");
            float elapsed = Time.time - crossedAt;
            Check(elapsed >= configuredDelay - 0.005f && elapsed < configuredDelay + 0.15f,
                "Actual flip timing is outside the configured delay");
            await Until(() => bash.IsPreparing, 1f, "Bash did not start after turning");

            await Delay(0.35f);
            PlaceTarget(1.25f);
            await Delay(0.2f);
            Check(facing.localScale.x < 0f && probe.ReceivedHitCount == 0,
                "Bash release snapped the visual or hitbox toward the player");
            float before = health.CurrentHealth;
            IncomingHit(enemy, 1f, 10f);
            float rearDamage = before - health.CurrentHealth;
            Check(rearDamage > 9f && reaction.IsStunned,
                "Rear attack did not bypass guard before the visual flipped");

            return new { passed = true, configuredDelay, elapsed, rearDamage, canceledWhenCrossingBack = true,
                heldFacingDuringBash = true };

            void PlaceTarget(float offset)
            {
                target.transform.position = new Vector3(body.position.x + offset, 0f);
                Physics2D.SyncTransforms();
            }
        }
        finally
        {
            Object.Destroy(root);
            Object.Destroy(target);
            Object.Destroy(floor.gameObject);
            Application.runInBackground = previousBackground;
        }
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        bool background = Application.runInBackground;
        float maximumDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        GameObject root = null, target = null, floor = null, wall = null;
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.03f;
            floor = Box("Shieldbearer Test Floor", new Vector2(0f, -0.5f), new Vector2(50f, 1f)).gameObject;
            target = new GameObject("Player Damage Probe");
            var targetBody = target.AddComponent<BoxCollider2D>();
            targetBody.size = new Vector2(0.6f, 1.8f);
            targetBody.offset = Vector2.up * 0.9f;
            targetBody.isTrigger = true;
            var probe = target.AddComponent<EnemyDamageProbe>();
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath),
                new Vector3(0f, 0.02f), Quaternion.identity);
            root.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            root.GetComponentInChildren<EnemyDamageIndicatorModule>().enabled = false;
            var enemy = root.GetComponent<EnemyShieldbearer>();
            var body = root.GetComponent<Rigidbody2D>();
            var move = root.GetComponentInChildren<EnemyMovementModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            var health = enemy.GetModule<INewHealthModule>();
            var bash = root.GetComponentInChildren<EnemyShieldBashSkill>();
            var charge = root.GetComponentInChildren<EnemyShieldChargeSkill>();
            var animator = root.GetComponentInChildren<Animator>();
            await Until(() => move.IsGrounded, 2f, "Shieldbearer did not land");
            Check(Mathf.Approximately(health.MaxHealth, 140f) && enemy.DetectionRange == 10f &&
                enemy.ChaseReleaseRange == 13f && probe.ReceivedHitCount == 0, "Incorrect HP, sight, or contact damage");

            float previousHealth = health.CurrentHealth;
            IncomingHit(enemy, 1f, 10f);
            float frontDamage = previousHealth - health.CurrentHealth;
            Check(!reaction.IsReacting && Mathf.Abs(frontDamage - 3f * 100f / 108f) < 0.01f,
                "Front guard did not reduce damage and suppress hit control");
            previousHealth = health.CurrentHealth;
            IncomingHit(enemy, -1f, 10f);
            float rearDamage = previousHealth - health.CurrentHealth;
            Check(reaction.IsStunned && reaction.IsKnockedBack && reaction.RemainingReactionTime >= 0.34f &&
                Mathf.Abs(body.linearVelocityX - 1.75f) < 0.01f && Mathf.Abs(rearDamage - 10f * 100f / 108f) < 0.01f,
                "Rear reaction, knockback, or defense stat is incorrect");
            await Until(() => !enemy.IsHitReacting, 2f, "Rear stagger did not finish");

            ResetTarget(1.35f);
            await Until(() => bash.IsPreparing, 1f, "Bash did not start automatically");
            float preparedAt = Time.time;
            float bashStartX = body.position.x;
            await Delay(0.26f);
            Check(bash.IsPreparing && body.position.x < bashStartX - 0.15f && probe.ReceivedHitCount == 0,
                "Bash preparation did not step back");
            await Until(() => probe.ReceivedHitCount == 1, 1f, "Bash did not hit");
            float bashPreparation = Time.time - preparedAt;
            Check(Mathf.Abs(bashPreparation - 0.5f) < 0.08f && probe.TotalDamage == 18f, "Bash timing or damage is wrong");
            await Until(() => bash.IsRecovering, 1f, "Bash never entered recovery");
            preparedAt = Time.time;
            previousHealth = health.CurrentHealth;
            IncomingHit(enemy, 1f, 10f);
            Check(Mathf.Abs(previousHealth - health.CurrentHealth - rearDamage) < 0.01f,
                "The recovery opening was still guarded");
            await Until(() => !bash.IsUsing, 1.5f, "Bash recovery did not finish");
            Check(Mathf.Abs(Time.time - preparedAt - 0.7f) < 0.10f && probe.ReceivedHitCount == 1,
                "Bash hit more than once or recovered too early");

            ResetTarget(4.5f);
            await Until(() => charge.IsPreparing, 1f, "Charge did not start automatically");
            preparedAt = Time.time;
            await Until(() => charge.ControlsMovement, 1.5f, "Charge never released");
            float chargePreparation = Time.time - preparedAt;
            await Until(() => charge.IsRecovering, 1f, "Charge did not stop on player contact");
            Check(Mathf.Abs(chargePreparation - 0.7f) < 0.08f && probe.ReceivedHitCount == 2 &&
                probe.TotalDamage == 42f && body.position.x < target.transform.position.x, "Wrong contact charge result");
            preparedAt = Time.time;
            enemy.SetTarget(null);
            await Until(() => !charge.IsUsing, 2f, "Charge recovery did not finish");
            Check(Mathf.Abs(Time.time - preparedAt - 1.2f) < 0.10f && probe.ReceivedHitCount == 2,
                "Charge recovery or one-hit restriction is wrong");

            ResetCooldown();
            ResetTarget(5f);
            await Until(() => charge.IsPreparing, 1f, "Second charge did not prepare");
            wall = Box("Charge Blocking Wall", new Vector2(2f, 1.5f), new Vector2(0.2f, 3f)).gameObject;
            Physics2D.SyncTransforms();
            await Until(() => charge.IsRecovering, 2f, "Charge did not end at the wall");
            Check(body.position.x > 0.4f && body.position.x < 1.42f && probe.ReceivedHitCount == 2,
                "Wall did not stop the charge before reaching the player");
            wall.SetActive(false);
            enemy.SetTarget(null);
            await Until(() => !charge.IsUsing, 2f, "Wall recovery did not finish");

            ResetCooldown();
            ResetTarget(5f);
            await Until(() => charge.IsPreparing, 1f, "Parry test charge did not prepare");
            Check(enemy.TryParryCharge() && !charge.IsUsing && reaction.RemainingReactionTime >= 0.99f &&
                !enemy.TryParryCharge(), "Parry did not cancel the charge and apply one-second stagger");
            enemy.SetTarget(null);
            await Until(() => !enemy.IsHitReacting, 2f, "Parry stagger did not finish");

            ResetCooldown();
            ResetTarget(5f);
            await Until(() => charge.ControlsMovement, 1.5f, "Open-path charge did not release");
            float chargeStart = body.position.x;
            target.transform.position = new Vector3(-8f, 0f);
            Physics2D.SyncTransforms();
            await Until(() => charge.IsRecovering, 1.5f, "Open-path charge did not recover");
            float travelled = body.position.x - chargeStart;
            Check(Mathf.Abs(travelled - 6f) < 0.16f && probe.ReceivedHitCount == 2,
                "Charge exceeded its range or changed direction after release");

            var reward = root.GetComponentInChildren<EnemyCurrencyRewardModule>();
            var gold = (CurrencySO)new SerializedObject(reward).FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            int deaths = 0;
            enemy.DeathStarted += () => deaths++;
            IncomingHit(enemy, -1f, 99999f);
            Check(enemy.IsDead && deaths == 1 &&
                animator.GetCurrentAnimatorStateInfo(0).IsName("Base Layer.Death"), "Death did not start on the lethal frame");
            await Until(() => Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Length > 0,
                3f, "No reward coins were created");
            var coins = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
            Check(coins.Sum(coin => coin.Amount) == 18, "Death reward must total 18 gold");
            await Until(() => root == null && currency.Data.Count == 18, 9f, "Death fade or automatic pickup failed");
            return new { passed = true, frontDamage, rearDamage, bashPreparation, chargePreparation,
                travelled, playerDamage = probe.TotalDamage, wallStopped = true, parryStun = true, reward = currency.Data.Count };

            void ResetTarget(float x)
            {
                body.position = new Vector2(0f, 0.025f);
                body.linearVelocity = Vector2.zero;
                target.transform.position = new Vector3(x, 0f);
                enemy.SetTarget(target.transform);
                Physics2D.SyncTransforms();
            }

            void ResetCooldown() => typeof(AbstractCoolTimeSkill).GetProperty("CoolTimeStartTime")
                .SetValue(charge, float.NegativeInfinity);
        }
        finally
        {
            Application.runInBackground = background;
            Time.maximumDeltaTime = maximumDelta;
            ServiceLocator.Register<ICurrencyManager>(previousCurrency);
            foreach (var item in new[] { root, target, floor, wall })
                if (item != null) Object.Destroy(item);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
        }
    }

    public static async Task<object> Preview()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        var camera = new GameObject("Shieldbearer Preview Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 1.05f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.backgroundColor = new Color(0.20f, 0.24f, 0.29f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Preview Light").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        var floor = Box("Shieldbearer Preview Floor", new Vector2(0f, -0.25f), new Vector2(16f, 0.5f));
        var floorSprite = floor.gameObject.AddComponent<SpriteRenderer>();
        floorSprite.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        floorSprite.color = new Color(0.14f, 0.17f, 0.20f);
        floor.transform.localScale = new Vector3(16f, 0.5f, 1f);
        floor.size = Vector2.one;
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath);
        ShowPose(-2.15f, "Idle", 0f);
        ShowPose(0f, "Attack", 0.4f);
        ShowPose(2.15f, "Death", 1f);
        await Awaitable.NextFrameAsync();
        EditorApplication.isPaused = true;
        return new { poses = 3 };

        void ShowPose(float x, string state, float normalizedTime)
        {
            var root = Object.Instantiate(prefab, new Vector3(x, 0f), Quaternion.identity);
            root.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            root.GetComponent<Rigidbody2D>().simulated = false;
            var animator = root.GetComponentInChildren<Animator>();
            animator.Play("Base Layer." + state, 0, normalizedTime);
            animator.Update(0f);
            animator.speed = 0f;
        }
    }
}
