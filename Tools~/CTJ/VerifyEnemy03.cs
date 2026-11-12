using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.SkillSystem.NormalSkill;
using _Scripts.Currency;
using _Scripts._Core._ServiceLocator;
using _Scripts.DevLib.BattleSystem;
using _Scripts.Testing.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemy03
{
    private const string Path = "Assets/_Prefabs/Enemies/Enemy/Enemy03/Enemy03_TwistedHuman.prefab";
    private const string OriginalScene = "CTJ.Enemy03.OriginalScene";
    private static void Check(bool valid, string message) { if (!valid) throw new Exception(message); }
    private static void Set(Object obj, string name, float value)
    {
        var data = new SerializedObject(obj);
        data.FindProperty(name).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
    private static void Cooldown(AbstractCoolTimeSkill skill, bool ready) => typeof(AbstractCoolTimeSkill)
        .GetProperty("CoolTimeStartTime").SetValue(skill, ready ? float.NegativeInfinity : Time.time);
    private static async Task Until(Func<bool> condition, float seconds, string message)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }
    private static async Task Delay(float seconds)
    {
        float end = Time.time + seconds;
        await Until(() => Time.time >= end, seconds * 8f + 2f, "Game clock stalled");
    }

    public static object Assets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Path);
        Check(root != null, "Enemy03 prefab missing");
        var warningPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Prefabs/Enemies/Enemy/Enemy03/Enemy03_LeapWarning.prefab");
        Check(warningPrefab != null && warningPrefab.GetComponent<AttackTelegraphUI>() != null &&
            warningPrefab.GetComponentInChildren<_Scripts.UI.Enemy.EnemyLeapLandingGraphic>() != null,
            "Leap warning prefab or circle graphic missing");
        Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(warningPrefab) == 0,
            "Leap warning has a missing script");
        var leapPrefab = root.GetComponentInChildren<EnemyLeapSkill>();
        var leapPrefabSettings = new SerializedObject(leapPrefab);
        Check(leapPrefabSettings.FindProperty("telegraphPrefab").objectReferenceValue ==
            warningPrefab.GetComponent<AttackTelegraphUI>(), "Leap warning reference missing");
        Check(leapPrefabSettings.FindProperty("landingParticlePrefab") != null &&
            root.GetComponentInChildren<EnemyParticleModule>() != null, "Leap landing particle slot missing");
        Check(root.GetComponent<Rigidbody2D>().collisionDetectionMode == CollisionDetectionMode2D.Continuous,
            "Enemy03 body must detect collisions during the fast dive");
        int references = 0;
        foreach (var t in root.GetComponentsInChildren<Transform>(true))
        {
            Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0, "Missing script: " + t.name);
            foreach (var component in t.GetComponents<Component>())
            {
                var p = new SerializedObject(component).GetIterator();
                while (p.Next(true))
                    if (p.propertyType == SerializedPropertyType.ObjectReference)
                    {
                        Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0,
                            "Broken reference: " + t.name + "/" + p.propertyPath);
                        if (p.objectReferenceValue != null) references++;
                    }
            }
        }
        var skills = root.GetComponentInChildren<EnemySkillModule>();
        Check(skills.Skills.Count == 2 && skills.Skills[0].Skill is EnemyTimedMeleeSkill &&
            skills.Skills[1].Skill is EnemyLeapSkill, "Attack list wrong");
        Check(root.GetComponentInChildren<EnemyMeleeSkill>() == null, "Legacy melee component remains");
        Check(skills.Skills.Select(s => s.Skill.SkillSo).Distinct().Count() == 2, "Skill SO collision");
        var controller = (AnimatorController)root.GetComponentInChildren<Animator>().runtimeAnimatorController;
        var states = controller.layers[0].stateMachine.states;
        var attackClip = (AnimationClip)states.Single(s => s.state.name == "Attack").state.motion;
        Check(AnimationUtility.GetAnimationEvents(attackClip).Length == 0, "Timing must not depend on old events");
        var slashPreparation = states.Single(s => s.state.name == "AttackPreparation").state.motion;
        foreach (var state in states.Where(s => s.state.name is "Leap" or "LeapLand"))
            Check(state.state.motion != attackClip && state.state.motion != slashPreparation,
                "Leap state shares a slash animation: " + state.state.name);
        return new { passed = true, references, skills = skills.Skills.Count, attackClipLength = attackClip.length,
            states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray() };
    }

    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying && SceneManager.sceneCount == 1, "One saved scene required");
        var scene = SceneManager.GetActiveScene();
        Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save scene first");
        SessionState.SetString(OriginalScene, scene.path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return scene.path;
    }

    private sealed class TestCurrency : ICurrencyManager
    {
        public CurrencyData Data;
        public CurrencyData GetCurrency(CurrencySO type) => Data;
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        bool background = Application.runInBackground;
        float timeScale = Time.timeScale, maximumDelta = Time.maximumDeltaTime;
        var previousCurrency = ServiceLocator.Get<ICurrencyManager>();
        var objects = new List<GameObject>();
        var checks = new List<string>();
        try
        {
            Application.runInBackground = true;
            Time.timeScale = 1f;
            Time.maximumDeltaTime = 0.033f;
            var floor = new GameObject("Enemy03 Verification Floor"); objects.Add(floor);
            floor.layer = LayerMask.NameToLayer("Ground");
            floor.transform.position = new Vector3(0, -0.05f);
            var floorCollider = floor.AddComponent<BoxCollider2D>();
            floorCollider.size = new Vector2(40f, 0.1f);
            var target = new GameObject("Player Damage Probe"); objects.Add(target);
            target.transform.position = new Vector3(1f, 0f);
            var targetCollider = target.AddComponent<BoxCollider2D>();
            targetCollider.size = new Vector2(0.6f, 1.8f);
            targetCollider.offset = new Vector2(0f, 0.9f);
            targetCollider.isTrigger = true;
            var probe = target.AddComponent<EnemyDamageProbe>();
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path), new Vector3(0, 0.03f), Quaternion.identity);
            objects.Add(root);
            var enemy = root.GetComponent<EnemyBase>();
            Set(enemy, "detectionRange", 0f);
            root.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            root.GetComponentInChildren<EnemyHitCameraShakeModule>().enabled = false;
            enemy.SetTarget(target.transform);
            var body = root.GetComponent<Rigidbody2D>();
            var movement = root.GetComponentInChildren<EnemyMovementModule>();
            var skills = root.GetComponentInChildren<EnemySkillModule>();
            var slash = root.GetComponentInChildren<EnemyTimedMeleeSkill>();
            var leap = root.GetComponentInChildren<EnemyLeapSkill>();
            var particleSource = new GameObject("Leap Landing Particle Probe");
            objects.Add(particleSource);
            particleSource.SetActive(false);
            var landingParticle = particleSource.AddComponent<ParticleSystem>();
            var particleMain = landingParticle.main;
            particleMain.duration = 0.1f;
            particleMain.startLifetime = 0.25f;
            particleMain.loop = false;
            var leapSettings = new SerializedObject(leap);
            leapSettings.FindProperty("landingParticlePrefab").objectReferenceValue = landingParticle;
            leapSettings.ApplyModifiedPropertiesWithoutUndo();
            ParticleSystem[] LandingParticles() => Object.FindObjectsByType<ParticleSystem>(FindObjectsSortMode.None)
                .Where(p => p.name == "Leap Landing Particle Probe(Clone)").ToArray();
            var health = enemy.GetModule<INewHealthModule>();
            var reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
            await Until(() => movement.IsGrounded, 3, "Enemy did not settle");
            Check(Mathf.Approximately(health.CurrentHealth, 80f), "HP is not 80");
            Check(!leap.CanUseSkill(), "Leap allowed while slash is ready");
            Check(skills.TryUseSpecifiedSkill(slash), "Slash failed to start");
            float started = Time.time;
            await Delay(0.35f);
            Check(probe.ReceivedHitCount == 0 && slash.IsPreparing, "Slash hit before preparation");
            await Until(() => probe.ReceivedHitCount == 1, 2f, "Slash did not hit");
            float preparation = Time.time - started;
            Check(preparation >= 0.44f && preparation < 0.58f, "Slash preparation duration: " + preparation);
            Check(Mathf.Approximately(probe.TotalDamage, 12f), "Slash damage != 12");
            await Until(() => !skills.IsBusy, 2f, "Slash recovery did not end");
            Check(Time.time - started >= 1.36f && probe.ReceivedHitCount == 1, "Recovery or single-hit guard failed");
            checks.Add("Slash: 0.45s preparation / 12 damage once / 0.12s active / 0.8s recovery");

            foreach (float range in new[] { 2.99f, 3f, 6f, 6.01f })
            {
                target.transform.position = enemy.transform.position + Vector3.right * range;
                bool expected = range >= 3f && range <= 6f;
                Check(leap.CanUseSkill() == expected, "Leap range boundary failed: " + range);
            }

            float landingX = enemy.transform.position.x + 6f;
            target.transform.position = new Vector3(landingX, enemy.transform.position.y);
            Physics2D.SyncTransforms();
            Check(slash.RemainingCoolTime > 0f && leap.CanUseSkill(), "Leap unavailable during slash cooldown");
            Check(skills.TryUseSpecifiedSkill(leap), "Leap failed to start");
            started = Time.time;
            var warning = root.GetComponentInChildren<AttackTelegraphUI>(true);
            Check(warning != null && !warning.IsVisible, "Leap warning must start hidden");
            target.transform.position = enemy.transform.position + Vector3.right * 2.9f;
            await Delay(0.5f);
            Check(leap.IsPreparing && !leap.ControlsMovement, "Leap launched before 0.7s");
            Check(warning.IsVisible, "Leap warning not shown before launch");
            var warningArea = warning.GetComponent<RectTransform>();
            Check(Mathf.Abs(warningArea.sizeDelta.x - 2f) < 0.01f &&
                Mathf.Abs(warningArea.position.x - landingX) < 0.02f,
                "Leap warning moved after the target left its starting position");
            target.transform.position = new Vector3(landingX + 0.35f, 0f);
            float fromX = body.position.x;
            float gravityBeforeLeap = body.gravityScale;
            await Until(() => leap.ControlsMovement, 2f, "Leap did not launch");
            Check(!warning.IsVisible, "Leap warning remained after launch");
            float leapPreparation = Time.time - started;
            Check(leapPreparation >= 0.69f && leapPreparation < 0.84f, "Leap preparation wrong: " + leapPreparation);
            target.transform.position = new Vector3(landingX - 0.35f, 0f);
            Cooldown(slash, true);
            Check(!skills.TryUseSpecifiedSkill(slash), "Slash started during flight");
            await Until(() => body.linearVelocityY > 1f, 2f, "Leap did not rise");
            float firstHorizontalSpeed = Mathf.Abs(body.linearVelocityX);
            float fastestHorizontalSpeed = firstHorizontalSpeed;
            float fastestDiveSpeed = 0f;
            float landingDeadline = Time.realtimeSinceStartup + 3f;
            while (leap.ControlsMovement && Time.realtimeSinceStartup < landingDeadline)
            {
                fastestHorizontalSpeed = Mathf.Max(fastestHorizontalSpeed, Mathf.Abs(body.linearVelocityX));
                fastestDiveSpeed = Mathf.Max(fastestDiveSpeed, -body.linearVelocityY);
                Check(LandingParticles().Length == 0, "Landing particle played before contact");
                await Awaitable.NextFrameAsync();
            }
            Check(!leap.ControlsMovement, "Leap did not land");
            float distance = body.position.x - fromX;
            Check(Mathf.Abs(movement.BodyCenter.x - landingX) < 0.06f, "Leap missed the locked target X");
            Check(fastestHorizontalSpeed > firstHorizontalSpeed * 2f && fastestDiveSpeed > 9f,
                "Leap did not accelerate or dive fast enough");
            Check(Mathf.Approximately(body.gravityScale, gravityBeforeLeap), "Gravity was not restored after landing");
            var landingEffects = LandingParticles();
            Check(landingEffects.Length == 1 && landingEffects[0].isPlaying &&
                Vector2.Distance(landingEffects[0].transform.position, new Vector2(landingX, 0f)) < 0.12f,
                "Landing particle did not play once at the contact");
            var landingEffect = landingEffects[0];
            Check(probe.ReceivedHitCount == 2 && Mathf.Approximately(probe.TotalDamage, 30f), "Landing damage != 18 once");
            Check(!skills.TryUseAttack(), "Slash started during landing damage window");
            await Until(() => leap.IsRecovering, 2f, "Landing recovery did not start");
            float recoveryStarted = Time.time;
            await Delay(0.75f);
            Check(!skills.TryUseAttack() && !skills.TryUseSpecifiedSkill(slash), "Attack started during leap recovery");
            await Until(() => !skills.IsBusy, 3f, "Landing recovery did not finish");
            float leapRecovery = Time.time - recoveryStarted;
            Check(leapRecovery >= 0.95f && leapRecovery < 1.1f, "Leap recovery wrong: " + leapRecovery);
            Check(landingEffect == null, "Landing particle was not cleaned up");
            Check(probe.ReceivedHitCount == 2, "Landing hit repeated");
            Check(skills.TryUseAttack() && slash.IsPreparing, "Slash did not unlock after recovery");
            skills.StopAttack();
            checks.Add("Leap: inclusive 3–6 start range / warning and landing locked at preparation start / 18 damage once / 1s recovery");
            checks.Add("Accelerating flight / fast dive onto thin floor / one contact particle / gravity and lifetime cleanup");

            Cooldown(leap, true); Cooldown(slash, false);
            target.transform.position = new Vector3(body.position.x + 4.5f, 0f);
            var wall = new GameObject("Leap Obstacle"); objects.Add(wall);
            wall.layer = floor.layer;
            wall.transform.position = new Vector3(body.position.x + 2f, 2f);
            wall.AddComponent<BoxCollider2D>().size = new Vector2(0.3f, 4f);
            Physics2D.SyncTransforms();
            Check(!leap.CanUseSkill(), "Leap passed through blocking wall");
            wall.SetActive(false);
            floorCollider.size = new Vector2(3f, 1f);
            floor.transform.position = new Vector3(body.position.x, -0.5f);
            Physics2D.SyncTransforms();
            Check(!leap.CanUseSkill(), "Leap allowed without landing floor");
            floorCollider.size = new Vector2(40f, 1f);
            floor.transform.position = new Vector3(0f, -0.5f);
            floor.layer = LayerMask.NameToLayer("Platform");
            Physics2D.SyncTransforms();
            Check(leap.CanUseSkill(), "Platform was not accepted as landing ground");
            floor.layer = LayerMask.NameToLayer("Ground");
            checks.Add("Wall / cliff rejection and Platform landing support");

            Check(skills.TryUseSpecifiedSkill(leap), "Leap for hit cancellation failed");
            await Until(() => warning.IsVisible, 2f, "No warning before cancellation");
            float oldHP = health.CurrentHealth;
            enemy.ApplyDamage(new DamageData { DamageAmount = 10f }, body.position, Vector2.left, Vector2.right);
            Check(!skills.IsBusy && reaction.IsStunned, "Preparation was not cancelled by damage");
            Check(!warning.IsVisible, "Leap warning remained after interruption");
            Check(Mathf.Abs((oldHP - health.CurrentHealth) - 10f) < 0.01f, "Enemy03 did not take full damage");
            Check(Mathf.Abs(reaction.RemainingReactionTime - 0.25f) < 0.03f, "Stun != 0.25s");
            await Until(() => !reaction.IsReacting, 2f, "Hit stun stuck");
            Cooldown(slash, true);
            target.transform.position = new Vector3(body.position.x + 1f, 0f);
            Check(skills.TryUseSpecifiedSkill(slash), "Slash cancellation setup failed");
            await Delay(0.1f);
            enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, body.position, Vector2.left, Vector2.right);
            Check(!skills.IsBusy, "Slash preparation not cancelled");
            await Until(() => !reaction.IsReacting, 2f, "Slash hit stun stuck");
            checks.Add("Both preparations cancel on hit / full incoming damage / stun 0.25s");

            // 실제 FSM도 쿨타임이 없는 도약 거리에서 정지하지 않고 걸어가야 합니다.
            Cooldown(slash, true); Cooldown(leap, true);
            Set(enemy, "detectionRange", 9f);
            target.transform.position = new Vector3(body.position.x - 4.8f, 0f);
            float walkX = body.position.x;
            await Delay(0.25f);
            Check(body.position.x < walkX - 0.2f && !leap.IsUsing, "AI stalled at unavailable leap range");
            Cooldown(slash, false);
            await Until(() => leap.IsPreparing, 2f, "AI did not select leap during slash cooldown");
            float launchX = body.position.x;
            float leftLandingX = target.transform.position.x;
            target.transform.position = new Vector3(launchX + 30f, 0f);
            await Until(() => warning.IsVisible, 2f, "AI leap warning disappeared when target left chase range");
            Check(Mathf.Abs(warningArea.position.x - leftLandingX) < 0.02f, "AI leap retargeted its warning");
            await Until(() => leap.ControlsMovement, 2f, "AI leap did not release after target left chase range");
            await Delay(0.15f);
            Check(leap.ControlsMovement && body.position.x < launchX, "Released leap cancelled or reversed when target left");
            await Until(() => !leap.ControlsMovement, 3f, "AI leap did not land");
            Check(Mathf.Abs(movement.BodyCenter.x - leftLandingX) < 0.06f, "Short left leap missed the target");
            enemy.SetTarget(null);
            await Delay(0.45f);
            Check(leap.IsRecovering, "Losing the target cleared leap recovery");
            enemy.SetTarget(target.transform);
            target.transform.position = new Vector3(body.position.x + 1f, 0f);
            Cooldown(slash, true);
            await Until(() => slash.IsPreparing, 2f, "AI did not resume attacking after recovery");
            checks.Add("FSM selects leap in cooldown / movement beyond chase range preserves the left landing / next slash");

            var reward = root.GetComponentInChildren<EnemyCurrencyRewardModule>();
            var gold = (CurrencySO)new SerializedObject(reward).FindProperty("rewardCurrency").objectReferenceValue;
            var currency = new TestCurrency { Data = new CurrencyData(gold, 0) };
            ServiceLocator.Register<ICurrencyManager>(currency);
            var managerObject = new GameObject("Enemy03 Test Kill Count"); objects.Add(managerObject);
            var manager = managerObject.AddComponent<EnemyKillCountManager>();
            int deaths = 0; enemy.DeathStarted += () => deaths++;
            target.transform.position = new Vector3(body.position.x + 1f, 0f);
            enemy.ApplyDamage(new DamageData { DamageAmount = 9999f }, body.position, Vector2.left, Vector2.right);
            Check(enemy.IsDead && deaths == 1, "Immediate death event missing");
            await Until(() => Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None).Length > 0, 3f, "No coins");
            var coins = Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None);
            Check(coins.Length == 3 && coins.Sum(c => c.Amount) == 8, "Reward must be 8 gold in 3 coins");
            await Until(() => root == null && currency.Data.Count == 8, 10f, "Death fade / coin collection failed");
            Check(manager.GetKillCount() == 1 && deaths == 1, "Death counted more than once");
            checks.Add("Death fade / 3 coins totaling 8 gold / exactly one kill");
            return new { passed = true, slashPreparation = preparation, leapPreparation, leapDistance = distance,
                leapRecovery, firstHorizontalSpeed, fastestHorizontalSpeed, fastestDiveSpeed, checks };
        }
        finally
        {
            Application.runInBackground = background;
            Time.timeScale = timeScale;
            Time.maximumDeltaTime = maximumDelta;
            ServiceLocator.Register<ICurrencyManager>(previousCurrency);
            foreach (var obj in objects) if (obj != null) Object.Destroy(obj);
            foreach (var blood in Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None)) Object.Destroy(blood.gameObject);
            foreach (var coin in Object.FindObjectsByType<EnemyRewardCoin>(FindObjectsSortMode.None)) Object.Destroy(coin.gameObject);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;

    public static async Task<object> Preview()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        var camera = new GameObject("Enemy03 Preview Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0f, 1.1f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 2.5f;
        camera.backgroundColor = new Color(0.08f, 0.1f, 0.12f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Preview Light").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        var floor = new GameObject("Preview Floor");
        var floorSprite = floor.AddComponent<SpriteRenderer>();
        floorSprite.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        floorSprite.color = new Color(0.23f, 0.27f, 0.3f);
        floor.transform.position = new Vector3(0f, -0.25f);
        floor.transform.localScale = new Vector3(20f, 0.5f, 1f);
        var states = new[] { "Idle", "AttackPreparation", "Leap" };
        var sizes = new List<Vector3>();
        for (int i = 0; i < states.Length; i++)
        {
            var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(Path), new Vector3((i - 1) * 2.8f, 0), Quaternion.identity);
            Set(root.GetComponent<EnemyBase>(), "detectionRange", 0f);
            root.GetComponent<Rigidbody2D>().simulated = false;
            root.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            var animator = root.GetComponentInChildren<Animator>();
            animator.Play("Base Layer." + states[i], 0, i == 0 ? 0f : 0.5f);
            animator.Update(0f);
            animator.speed = 0f;
            if (i == 2) animator.transform.parent.localScale = new Vector3(1.08f, 0.72f, 1f);
            sizes.Add(animator.GetComponent<SpriteRenderer>().bounds.size);
        }
        var landing = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Prefabs/Enemies/Enemy/Enemy03/Enemy03_LeapWarning.prefab"));
        var landingUI = landing.GetComponent<AttackTelegraphUI>();
        landingUI.Show();
        landingUI.SetBounds(new Bounds(new Vector3(2.8f, 0.05f), new Vector3(2f, 2f, 0f)));
        await Awaitable.NextFrameAsync();
        return new { poses = states, spriteBounds = sizes };
    }

    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(OriginalScene, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(OriginalScene);
        return path;
    }
}
