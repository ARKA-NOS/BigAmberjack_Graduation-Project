using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Managers;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players;
using _Scripts.DevLib.BattleSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemyCombatCoordination
{
    private const string EnemyPath = "Assets/_Prefabs/Enemies/Enemy/Enemy17/Enemy17_IntakeBiter.prefab";

    public static object Assets()
    {
        var manager = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Manager/GameManager.prefab");
        Check(manager.GetComponentsInChildren<TimeStopService>(true).Length == 1 &&
            manager.GetComponentsInChildren<AttackTokenManager>(true).Length == 1, "Manager children missing/duplicated");
        var settings = new SerializedObject(manager.GetComponentInChildren<AttackTokenManager>(true));
        Check(settings.FindProperty("maxToken").intValue == 2 && settings.FindProperty("waitTimeout").floatValue == 2f,
            "Default token settings differ");

        int checkedEnemies = 0;
        foreach (string id in AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies/Enemy" }))
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(AssetDatabase.GUIDToAssetPath(id));
            if (root.GetComponent<EnemyBase>() == null) continue;
            Check(root.GetComponentsInChildren<EnemyAttackTokenModule>(true).Length == 1 &&
                root.GetComponentsInChildren<TimeFreezeModule>(true).Length == 1 &&
                root.GetComponentsInChildren<EnemySeparationModule>(true).Length == 1, "Missing enemy module: " + root.name);
            foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + root.name);
            checkedEnemies++;
        }

        var boss = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab");
        Check(boss.GetComponentInChildren<TimeFreezeModule>(true) != null &&
            boss.GetComponentInChildren<EnemyAttackTokenModule>(true) == null, "Boss token exclusion/freezing configuration incorrect");
        return new { checkedEnemies, bossExemptFromTokens = true, managerChildren = 2, maxToken = 2, waitTimeout = 2f };
    }

    public static async Task<object> TimeEffects()
    {
        using (var s = new Scope())
        {
            var rows = new List<object>();
            var service = s.TimeService;
            service.SetBaseTimeScale(1f);
            await Awaitable.NextFrameAsync();
            float start = Time.unscaledTime;
            EventBus<HitStopEvent>.Raise(new HitStopEvent(0.12f));
            Check(Mathf.Abs(Time.timeScale - 0.05f) < 0.001f, "Hit stop event did not change time");
            await Delay(0.06f);
            service.RequestHitStop(0.15f);
            await Until(() => !service.IsStopped && Mathf.Approximately(Time.timeScale, 1f), 0.6f, "Overlapping stop did not restore");
            Check(Time.unscaledTime - start >= 0.20f, "Overlap shortened its later deadline");
            rows.Add(new { test = "HitStopOverlap", elapsed = Time.unscaledTime - start, restored = Time.timeScale });

            object a = new object(), b = new object();
            service.SetPaused(a, true);
            service.SetPaused(b, true);
            service.SetPaused(a, false);
            Check(Time.timeScale == 0f, "Nested pause was released too early");
            service.RequestHitStop(0.03f);
            await Delay(0.07f);
            Check(Time.timeScale == 0f, "Hit stop expiration unpaused the game");
            service.SetPaused(b, false);
            Check(Time.timeScale == 1f, "Pause resumed a stale freeze scale");
            rows.Add(new { test = "NestedPauseAndFreezeExpiration", passed = true });

            service.RequestHitStop(0.08f);
            float legacyCaptured = Time.timeScale;
            Time.timeScale = 0f; // 아직 이전하지 않은 팀 UI의 동작을 재현합니다.
            await Delay(0.12f);
            Check(Time.timeScale == 0f, "Service overwrote a legacy UI pause");
            Time.timeScale = legacyCaptured;
            await Awaitable.NextFrameAsync();
            Check(Time.timeScale == 1f, "Legacy pause resumed an expired freeze");
            rows.Add(new { test = "ExistingUiPauseCoexistence", passed = true });

            var slowRoot = new GameObject("Slow motion module verification");
            var slow = slowRoot.AddComponent<EnemyDeathSlowMotionModule>();
            Call(slow, "PlaySlowMotion");
            Check(Mathf.Abs(Time.timeScale - 0.2f) < 0.001f, "Boss death module bypassed time service");
            service.RequestHitStop(0.03f);
            Check(Mathf.Abs(Time.timeScale - 0.05f) < 0.001f, "Hit stop did not compose with boss slow motion");
            await Until(() => !service.IsStopped, 0.3f, "Composed hit stop did not expire");
            Check(Time.timeScale > 0.05f && Time.timeScale < 0.3f, "Hit stop expiration discarded the boss slow motion");
            await Until(() => Mathf.Approximately(Time.timeScale, 1f), 0.8f, "Death slow motion did not recover");
            Object.Destroy(slowRoot);
            rows.Add(new { test = "DeathSlowMotionBridge", passed = true });

            EnemyBase enemy = s.Enemy(0f);
            await Delay(0.08f);
            var health = enemy.GetModule<INewHealthModule>();
            float before = health.CurrentHealth;
            enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.left);
            Check(health.CurrentHealth < before && !service.IsStopped, "Non-player damage triggered hit stop");
            before = health.CurrentHealth;
            enemy.ApplyDamage(new DamageData { DamageAmount = 1f, Dealer = s.Player }, enemy.transform.position, Vector2.right, Vector2.left);
            Check(health.CurrentHealth < before && service.IsStopped, "Actual player damage did not trigger hit stop");
            await Until(() => !service.IsStopped, 0.3f, "Player hit stop stuck");
            enemy.ApplyDamage(new DamageData { DamageAmount = 0f, Dealer = s.Player }, enemy.transform.position, Vector2.right, Vector2.left);
            Check(!service.IsStopped, "Zero damage triggered hit stop");
            enemy.ApplyDamage(new DamageData { DamageAmount = 10000f, Dealer = s.Player }, enemy.transform.position, Vector2.right, Vector2.left);
            Check(enemy.IsDead && service.IsStopped, "Fatal player damage skipped hit stop");
            await Until(() => !service.IsStopped, 0.3f, "Fatal hit stop did not expire");
            rows.Add(new { test = "PlayerDamageOnly", passed = true });

            Field(typeof(TimeStopService), "stopTimeScale").SetValue(service, 0f);
            service.RequestHitStop(0.03f);
            Check(Time.timeScale == 0f, "Full freeze did not stop time");
            await Until(() => !service.IsStopped && Time.timeScale == 1f, 0.3f, "Full freeze could not recover using unscaled time");
            Field(typeof(TimeStopService), "stopTimeScale").SetValue(service, 0.05f);
            rows.Add(new { test = "ZeroScaleFreezeRestores", passed = true });

            service.RequestHitStop(1f);
            service.enabled = false;
            Check(Time.timeScale == 1f, "Disabling service left time stopped");
            service.enabled = true;
            rows.Add(new { test = "DisabledServiceRestores", passed = true });
            return new { results = rows };
        }
    }

    public static async Task<object> Tokens()
    {
        using (var s = new Scope())
        {
            var manager = s.TokenManager;
            var settings = new SerializedObject(manager);
            int originalMax = settings.FindProperty("maxToken").intValue;
            float originalTimeout = settings.FindProperty("waitTimeout").floatValue;
            var rows = new List<object>();
            try
            {
                Configure(manager, 2, 2f);
                var enemies = Enumerable.Range(0, 4).Select(_ => s.Enemy(0f, false)).ToArray();
                await Delay(0.10f);
                Check(Start(enemies[0]) && Start(enemies[1]) && !Start(enemies[2]) && !Start(enemies[3]), "Token capacity did not gate attacks");
                Hold(enemies[0]); Hold(enemies[1]);
                Check(manager.ActiveTokenCount == 2 && manager.WaitingCount == 2, "Wrong capacity/queue count");
                enemies[0].GetModule<EnemySkillModule>().StopAttack();
                Check(enemies[2].GetModule<EnemyAttackTokenModule>().HasToken && !enemies[3].GetModule<EnemyAttackTokenModule>().HasToken,
                    "Returned token did not go to the FIFO waiter");
                Check(Start(enemies[2]), "Granted waiter could not start");
                Hold(enemies[2]);
                enemies[1].GetModule<EnemySkillModule>().StopAttack();
                Check(enemies[3].GetModule<EnemyAttackTokenModule>().HasToken && manager.WaitingCount == 0, "Second waiter was skipped");
                Check(Start(enemies[3]), "Second granted waiter could not start");
                rows.Add(new { test = "CapacityAndFifo", maxActive = manager.ActiveTokenCount, fifo = true });
                s.ClearEnemies();

                Configure(manager, 1, 2f);
                EnemyBase holder = s.Enemy(0f, false), waiting = s.Enemy(0f, false);
                await Delay(0.1f);
                Check(Start(holder), "Timeout holder could not start"); Hold(holder);
                float queuedAt = Time.time;
                Check(!Start(waiting), "Waiter bypassed the holder");
                await Delay(0.6f);
                Check(!Start(waiting) && manager.WaitingCount == 1, "Repeated request duplicated its queue entry");
                await Until(() => !waiting.GetModule<EnemyAttackTokenModule>().IsWaiting, 1.7f, "Wait timeout did not expire");
                float expiredAt = Time.time;
                Check(expiredAt - queuedAt >= 1.98f && expiredAt - queuedAt < 2.15f, "Repeated request extended the deadline");
                Check(!Start(waiting) && manager.WaitingCount == 0, "Expired enemy rejoined before its retry delay");
                await Delay(0.53f);
                Check(!Start(waiting) && manager.WaitingCount == 1, "Expired enemy did not rejoin at the end");
                rows.Add(new { test = "TimeoutAndRetry", timeout = expiredAt - queuedAt, retryDelay = 0.5f, duplicateRequests = false });

                s.Target.transform.position = Vector3.right * 20f;
                await Awaitable.NextFrameAsync();
                Check(manager.WaitingCount == 0 && !waiting.GetModule<EnemyAttackTokenModule>().IsWaiting, "Range loss did not cancel the queue");
                s.Target.transform.position = Vector3.right * 0.75f;
                Check(!Start(waiting) && manager.WaitingCount == 1, "Range recovery could not queue again");
                GameObject wall = s.Wall(new Vector2(0.38f, 0.8f), new Vector2(0.04f, 1.6f));
                Physics2D.SyncTransforms();
                await Until(() => manager.WaitingCount == 0, 0.3f, "Blocked sight did not cancel the queue");
                wall.SetActive(false);
                Physics2D.SyncTransforms();
                await Delay(0.1f);
                Check(!Start(waiting) && manager.WaitingCount == 1, "Cleared sight could not queue again");
                waiting.ApplyDamage(new DamageData { DamageAmount = 10000f }, waiting.transform.position, Vector2.zero, Vector2.zero);
                Check(waiting.IsDead && manager.WaitingCount == 0, "Death left a queued enemy registered");
                rows.Add(new { test = "RangeSightLossAndQueuedDeath", passed = true });

                EnemyBase next = s.Enemy(0f, false);
                await Delay(0.1f);
                Check(!Start(next), "Next waiter bypassed token holder");
                holder.ApplyDamage(new DamageData { DamageAmount = 10000f }, holder.transform.position, Vector2.zero, Vector2.zero);
                Check(holder.IsDead && next.GetModule<EnemyAttackTokenModule>().HasToken, "Holder death did not return its token immediately");
                rows.Add(new { test = "HolderDeath", passed = true });
                s.ClearEnemies();

                EnemyBase natural = s.Enemy(0f, false);
                await Delay(0.1f);
                Check(Start(natural), "Natural attack did not start");
                var skill = natural.GetComponentInChildren<EnemyTimedMeleeSkill>();
                await Until(() => skill.IsRecovering, 3f, "Attack did not reach recovery");
                Check(natural.GetModule<EnemyAttackTokenModule>().HasToken && manager.ActiveTokenCount == 1, "Token released before recovery finished");
                await Until(() => !skill.IsUsing, 2f, "Recovery did not finish");
                Check(manager.ActiveTokenCount == 0 && !natural.GetModule<EnemyAttackTokenModule>().HasToken, "Normal skill completion did not return token");
                rows.Add(new { test = "HoldThroughRecovery", passed = true });
                s.ClearEnemies();

                holder = s.Enemy(0f, false); waiting = s.Enemy(0f, false);
                await Delay(0.1f);
                Check(Start(holder), "Pause holder did not start"); Hold(holder);
                Check(!Start(waiting), "Pause waiter did not queue");
                object pause = new object();
                s.TimeService.SetPaused(pause, true);
                float gameTime = Time.time;
                await Delay(0.15f);
                Check(manager.WaitingCount == 1 && Time.time == gameTime, "Paused queue consumed game time");
                s.TimeService.SetPaused(pause, false);
                manager.enabled = false;
                Check(!holder.IsAttackInProgress && !holder.GetModule<EnemyAttackTokenModule>().HasToken &&
                    !waiting.GetModule<EnemyAttackTokenModule>().IsWaiting && manager.ActiveTokenCount == 0 && manager.WaitingCount == 0,
                    "Manager shutdown left a running attack or reservation behind");
                manager.enabled = true;
                rows.Add(new { test = "PausedQueueAndManagerShutdown", passed = true });
                return new { results = rows };
            }
            finally
            {
                s.ClearEnemies();
                Configure(manager, originalMax, originalTimeout);
            }
        }
    }

    public static async Task<object> Separation()
    {
        using (var s = new Scope())
        {
            var results = new List<object>();
            EnemyBase a = s.Enemy(0f, false, true), b = s.Enemy(0f, false, true);
            await Delay(1.5f);
            float distance = Mathf.Abs(a.transform.position.x - b.transform.position.x);
            float required = a.GetComponent<Collider2D>().bounds.extents.x + b.GetComponent<Collider2D>().bounds.extents.x + 0.15f;
            Check(distance > required * 0.92f, "Ground enemies failed to separate: " + distance + "/" + required);
            results.Add(new { test = "GroundSeparation", distance, desired = required });
            s.ClearEnemies();

            a = s.Enemy(0f, false, true);
            b = s.Enemy(0f, false, true);
            float extent = a.GetComponentsInChildren<Collider2D>().First(c => !c.isTrigger).bounds.extents.x;
            float edgeX = 15f - extent - 0.05f;
            MoveX(a, edgeX); MoveX(b, edgeX - 0.1f);
            float initialY = a.transform.position.y;
            await Delay(1f);
            Check(a.transform.position.x <= edgeX + 0.03f && a.transform.position.y >= initialY - 0.1f,
                "Separation pushed an enemy off the ledge");
            results.Add(new { test = "LedgeProtection", initialX = edgeX, finalX = a.transform.position.x, grounded = a.GetModule<EnemyMovementModule>().IsGrounded });
            s.ClearEnemies();

            a = s.Enemy(0f, false, true, true); b = s.Enemy(0f, false, true, true);
            await Delay(1.8f);
            float airDistance = Vector2.Distance(a.transform.position, b.transform.position);
            Check(airDistance > 0.3f, "Flying enemies failed to separate");
            results.Add(new { test = "AirSeparation", distance = airDistance });
            return new { results };
        }
    }

    private static void MoveX(EnemyBase enemy, float x)
    {
        var position = enemy.transform.position; position.x = x;
        enemy.transform.position = position;
        enemy.GetComponent<Rigidbody2D>().position = position;
        Physics2D.SyncTransforms();
    }

    private static bool Start(EnemyBase enemy) => enemy.GetModule<EnemySkillModule>().TryUseAttack();

    private static void Hold(EnemyBase enemy) => Field(typeof(EnemyAttackSkill), "_phaseEndTime")
        .SetValue(enemy.GetModule<EnemySkillModule>().ActiveSkill, Time.time + 20f);

    private static void Configure(AttackTokenManager manager, int count, float timeout)
    {
        var data = new SerializedObject(manager);
        data.FindProperty("maxToken").intValue = count;
        data.FindProperty("waitTimeout").floatValue = timeout;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private sealed class Scope : IDisposable
    {
        public readonly PlayerController Player;
        public readonly GameObject Target;
        public readonly TimeStopService TimeService;
        public readonly AttackTokenManager TokenManager;
        private readonly GameObject[] _originals;
        private readonly GameObject _floor;
        private readonly List<EnemyBase> _enemies = new List<EnemyBase>();
        private readonly List<GameObject> _owned = new List<GameObject>();
        private readonly bool _background;
        private readonly float _baseScale;

        public Scope()
        {
            Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
            Player = Object.FindFirstObjectByType<PlayerController>(FindObjectsInactive.Include);
            Check(Player != null, "A real PlayerController is required for dealer filtering");
            _originals = SceneManager.GetActiveScene().GetRootGameObjects().Where(o => o.activeSelf && o.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
            foreach (var root in _originals) root.SetActive(false);
            _background = Application.runInBackground;
            Application.runInBackground = true;

            TimeService = ServiceLocator.Get<TimeStopService>();
            if (TimeService == null) TimeService = Own<TimeStopService>();
            TokenManager = ServiceLocator.Get<AttackTokenManager>();
            if (TokenManager == null) TokenManager = Own<AttackTokenManager>();
            _baseScale = TimeService.BaseTimeScale;
            TimeService.SetBaseTimeScale(1f);
            _floor = new GameObject("Coordination test floor") { layer = LayerMask.NameToLayer("Ground") };
            _floor.transform.position = Vector3.down * 0.1f;
            _floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 0.2f);
            Target = new GameObject("Coordination target probe");
            Target.transform.position = Vector3.right * 0.75f;
            var targetBody = Target.AddComponent<BoxCollider2D>();
            targetBody.isTrigger = true; targetBody.size = new Vector2(0.3f, 1.3f); targetBody.offset = Vector2.up * 0.65f;
            Target.AddComponent<_Scripts.Testing.Enemy.EnemyDamageProbe>();
        }

        private T Own<T>() where T : Component
        {
            var root = new GameObject("Verification " + typeof(T).Name);
            _owned.Add(root);
            return root.AddComponent<T>();
        }

        public GameObject Wall(Vector2 position, Vector2 size)
        {
            var wall = new GameObject("Coordination line of sight wall") { layer = LayerMask.NameToLayer("Ground") };
            _owned.Add(wall);
            wall.transform.position = position;
            wall.AddComponent<BoxCollider2D>().size = size;
            return wall;
        }

        public EnemyBase Enemy(float x, bool freeze = true, bool separate = false, bool flying = false)
        {
            string path = flying ? "Assets/_Prefabs/Enemies/Enemy/Enemy20/Enemy20_CeilingPatrol.prefab" : EnemyPath;
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path));
            var enemy = root.GetComponent<EnemyBase>();
            _enemies.Add(enemy);
            Field(typeof(EnemyBase), "detectionRange").SetValue(enemy, 0f);
            Field(typeof(EnemyBase), "moveSpeed").SetValue(enemy, 0f);

            foreach (var module in root.GetComponentsInChildren<MonoBehaviour>())
                if (module is EnemyParticleModule || module is EnemySoundModule || module is EnemyDamageIndicatorModule ||
                    module is EnemyHitCameraShakeModule || module is EnemyCurrencyRewardModule || module is EnemyDeathModule ||
                    module is EnemyHitReactionModule || module is EnemyPatrolModule) module.enabled = false;
            enemy.GetModule<TimeFreezeModule>().enabled = freeze;
            enemy.GetModule<EnemySeparationModule>().enabled = separate;
            Collider2D solid = root.GetComponentsInChildren<Collider2D>().FirstOrDefault(c => !c.isTrigger);
            if (solid == null) solid = root.GetComponent<Collider2D>();
            Vector3 position = new Vector3(x, root.transform.position.y - solid.bounds.min.y + 0.01f, 0f);
            if (flying) position.y = 3f;
            root.transform.position = position;
            root.GetComponent<Rigidbody2D>().position = position;
            enemy.SetTarget(Target.transform);
            Physics2D.SyncTransforms();
            return enemy;
        }

        public void ClearEnemies()
        {
            foreach (EnemyBase enemy in _enemies)
                if (enemy != null) { enemy.gameObject.SetActive(false); Object.Destroy(enemy.gameObject); }
            _enemies.Clear();
        }

        public void Dispose()
        {
            ClearEnemies();
            Object.Destroy(Target); Object.Destroy(_floor);
            TimeService.SetBaseTimeScale(_baseScale);
            foreach (GameObject root in _owned) { root.SetActive(false); Object.Destroy(root); }
            Application.runInBackground = _background;
            foreach (GameObject root in _originals) if (root != null) root.SetActive(true);
        }
    }

    private static FieldInfo Field(Type type, string name) => type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic);
    private static object Call(object target, string name) => target.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic).Invoke(target, null);
    private static void Check(bool passed, string message) { if (!passed) throw new Exception(message); }
    private static Task Delay(float seconds)
    {
        float end = Time.realtimeSinceStartup + seconds;
        return Until(() => Time.realtimeSinceStartup >= end, seconds + 0.4f, "Realtime delay stalled");
    }
    private static async Task Until(Func<bool> predicate, float seconds, string message)
    {
        float end = Time.realtimeSinceStartup + seconds;
        while (!predicate() && Time.realtimeSinceStartup < end) await Awaitable.NextFrameAsync();
        Check(predicate(), message);
    }
}
