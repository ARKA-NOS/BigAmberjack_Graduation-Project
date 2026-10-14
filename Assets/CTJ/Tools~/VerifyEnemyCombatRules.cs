using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script.Agent;
using Lrw.Script.Agent.StatSystem;
using Lrw.Script.Player.Skills;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 씬 저장 없이 CTJ 전용 Play 인스턴스에서만 실행합니다.
    public static class VerifyEnemyCombatRules
    {
        private const string Cat = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string Ghost = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";
        private const string Delay = "Assets/CTJ/Prototypes/Stats/EnemyAttackDelay.asset";
        private const string Player = "Assets/Lrw/GameModule/Player/Character/PlayerTestPack Variant.prefab";

        private static float PlayerDamage()
        {
            GameObject player = AssetDatabase.LoadAssetAtPath<GameObject>(Player);
            PlayerMeleeAttackSkill skill = player.GetComponentInChildren<PlayerMeleeAttackSkill>(true);
            StatData damage = (StatData)new SerializedObject(skill).FindProperty("physicalDamage").objectReferenceValue;
            StatModule module = player.GetComponentInChildren<StatModule>(true);
            StatGroup group = (StatGroup)new SerializedObject(module).FindProperty("baseStats").objectReferenceValue;
            return group.Stats.First(entry => entry.StatData == damage).BaseValue;
        }

        public static object ValidatePrefabs()
        {
            float damage = PlayerDamage();
            return new { playerDamage = damage, enemies = new[] { Cat, Ghost }.Select(path =>
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                foreach (Transform child in enemy.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + path);
                EnemyAttackSkill skill = enemy.GetComponent<EnemyAttackSkill>();
                SerializedObject serialized = new SerializedObject(skill);
                Check(serialized.FindProperty("attackDelayStat").objectReferenceValue == AssetDatabase.LoadAssetAtPath<StatData>(Delay), "Delay reference");
                AttackTelegraphUI ui = (AttackTelegraphUI)serialized.FindProperty("telegraphPrefab").objectReferenceValue;
                Check(ui != null && ui.GetComponent<Canvas>().renderMode == RenderMode.WorldSpace, "World Canvas reference");
                Check(!ui.GetComponentInChildren<Image>().raycastTarget && ui.GetComponent<GraphicRaycaster>() == null, "UI blocks input");
                StatGroup group = (StatGroup)new SerializedObject(enemy.GetComponent<StatModule>()).FindProperty("baseStats").objectReferenceValue;
                float hp = group.Stats.First(s => s.StatData.name == "MaxHealth").BaseValue;
                float delay = group.Stats.First(s => s.StatData.name == "EnemyAttackDelay").BaseValue;
                Check(enemy.ChaseReleaseRange > enemy.DetectionRange && hp > damage && delay > 0f, "Enemy config values");
                return new { path, detection = enemy.DetectionRange, release = enemy.ChaseReleaseRange, hp, hitsToKill = Mathf.CeilToInt(hp / damage), delay, referencesValid = true };
            }).ToArray() };
        }

        public static async Task<object> RunScenarios()
        {
            RequirePlay();
            return await Task.WhenAll(
                Chase(Cat, 4000f), Chase(Ghost, 4100f),
                Health(Cat, 4200f, 4), Health(Ghost, 4300f, 3),
                Melee(4400f, false), Melee(4500f, true),
                Ranged(4600f, false), Ranged(4700f, true),
                Cancellation(Cat, 4800f), Cancellation(Ghost, 4900f),
                ReleaseDuringAttack(Cat, 5000f), ReleaseDuringAttack(Ghost, 5100f),
                DelayStat(5200f));
        }

        private static async Task<object> Chase(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x, false);
                EnemyDamageProbe target = Target(arena, x + enemy.DetectionRange + 1f);
                enemy.SetTarget(target.transform);
                await Wait(0.2f);
                Check(State(enemy) == "EnemyIdleState", "Detected outside entry range");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange - 1f);
                await Wait(0.2f);
                Check(State(enemy) == "EnemyChaseState" && enemy.GetComponent<Rigidbody2D>().linearVelocityX > 0.1f, "Detection did not chase");
                target.transform.position = enemy.transform.position + Vector3.right * ((enemy.DetectionRange + enemy.ChaseReleaseRange) * 0.5f);
                await Wait(0.15f);
                Check(State(enemy) == "EnemyChaseState", "Chase broke at detection radius");
                target.transform.position = enemy.transform.position + Vector3.right * enemy.ChaseReleaseRange;
                await Wait(0.1f);
                Check(State(enemy) == "EnemyChaseState", "Chase broke at exact release boundary");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.ChaseReleaseRange + 1f);
                await Wait(0.1f);
                Check(State(enemy) == "EnemyIdleState", "Release did not idle");
                float stoppedX = enemy.transform.position.x;
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange + 0.5f);
                await Wait(0.25f);
                Check(State(enemy) == "EnemyIdleState" && Mathf.Abs(enemy.transform.position.x - stoppedX) < 0.02f && stoppedX > x + 0.1f,
                    "Enemy returned home or reacquired outside detection");
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.DetectionRange - 1f);
                await Wait(0.15f);
                Check(State(enemy) == "EnemyChaseState", "Reacquire failed");
                return new { scenario = path == Cat ? "cat-chase-hysteresis" : "ghost-chase-hysteresis", passed = true, noReturn = true, boundaryAndReacquire = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Health(string path, float x, int expectedHits)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                float damage = PlayerDamage();
                float initial = enemy.HealthModule.CurrentHealth;
                Check(initial == enemy.HealthModule.MaxHealth && initial == expectedHits * damage, "HP initialization");
                for (int i = 1; i <= expectedHits; i++)
                {
                    ((IDamageable)(Agent)enemy).ApplyDamage(new DamageData { DamageAmount = damage }, Vector2.zero, Vector2.zero, Vector2.zero);
                    Check(enemy.HealthModule.CurrentHealth == initial - damage * i && enemy.IsDead == (i == expectedHits), "HP/death threshold");
                }
                Check(!enemy.GetComponent<Rigidbody2D>().simulated && enemy.GetComponentsInChildren<Collider2D>().All(c => !c.enabled), "Death collision cleanup");
                await Wait(1.2f);
                Check(enemy == null, "Death despawn");
                return new { scenario = path == Cat ? "cat-4-player-hits" : "ghost-3-player-hits", passed = true, damage, initialHP = initial };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Melee(float x, bool dodge)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Cat, arena, x);
                if (dodge) enemy.transform.Find("Visual").localScale = new Vector3(-1f, 1f, 1f);
                EnemyDamageProbe target = Target(arena, x + (dodge ? -1f : 1f));
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemyMeleeSkill skill = enemy.GetComponent<EnemyMeleeSkill>();
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                Check(module.TryUseAttack(), "Melee begin");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                Check(ui != null && ui.IsVisible && skill.IsPreparing, "Preparation UI missing");
                Check(Mathf.Abs(ui.GetComponentInChildren<Image>().color.a - 50f / 255f) < 0.001f, "Alpha must be 50/255");
                BoxCollider2D hitbox = (BoxCollider2D)new SerializedObject(skill).FindProperty("attackHitbox").objectReferenceValue;
                RectTransform rect = ui.GetComponent<RectTransform>();
                Check(Vector2.Distance(ui.transform.position, hitbox.bounds.center) < 0.01f && Vector2.Distance(rect.rect.size, hitbox.bounds.size) < 0.01f, "Hitbox/UI size or facing mismatch");
                enemy.OnAttackHit(); enemy.OnAttackFinished();
                Check(target.ReceivedHitCount == 0 && skill.IsPreparing, "Early animation event bypassed delay");
                float start = Time.time;
                await Wait(0.2f);
                Check(skill.IsPreparing && target.ReceivedHitCount == 0, "Melee released before stat delay");
                if (dodge) target.transform.position += Vector3.up * 3f;
                bool blink = false, blinkOff = false, blinkOn = false;
                await ObserveUntil(() => !skill.IsUsing, () =>
                {
                    blink |= ui.IsBlinking;
                    if (ui.IsBlinking)
                    {
                        float alpha = ui.GetComponentInChildren<Image>().color.a;
                        blinkOff |= alpha < 0.001f;
                        blinkOn |= alpha > 0.1f;
                    }
                });
                Check(blink && blinkOff && blinkOn && !ui.IsVisible, "Melee flash/hide sequence");
                Check(target.ReceivedHitCount == (dodge ? 0 : 1), "Melee event contact damage");
                enemy.OnAttackHit(); enemy.OnAttackHit();
                Check(target.ReceivedHitCount == (dodge ? 0 : 1), "Late duplicate event");
                return new { scenario = dodge ? "left-melee-dodge-and-ui" : "melee-delay-event-flash-and-ui", passed = true, duration = Time.time - start, hits = target.ReceivedHitCount };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Ranged(float x, bool dodge)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(Ghost, arena, x);
                EnemyDamageProbe target = Target(arena, x + 4f);
                enemy.SetTarget(target.transform);
                await Wait(0.15f);
                EnemyProjectileSkill skill = enemy.GetComponent<EnemyProjectileSkill>();
                Check(enemy.GetModule<EnemySkillModule>().TryUseAttack(), "Ranged begin");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                Vector3 direction = ui.transform.right;
                Check(ui.IsVisible && skill.IsPreparing && Shots(enemy).Length == 0, "Ranged fired immediately");
                await Wait(0.25f);
                Check(skill.IsPreparing && Shots(enemy).Length == 0 && target.ReceivedHitCount == 0, "Ranged ignored delay");
                if (dodge) target.transform.position += Vector3.up * 3f;
                bool blink = false, off = false, on = false;
                await ObserveUntil(() => !skill.IsPreparing, () =>
                {
                    blink |= ui.IsBlinking;
                    if (ui.IsBlinking)
                    {
                        off |= ui.GetComponentInChildren<Image>().color.a < 0.001f;
                        on |= ui.GetComponentInChildren<Image>().color.a > 0.1f;
                    }
                });
                Check(blink && off && on && !ui.IsVisible, "Ranged flash/hide");
                EnemyProjectile projectile = Shots(enemy).Single();
                Check(Vector3.Dot(projectile.transform.right, direction) > 0.999f, "Shot changed advertised direction");
                await Wait(0.7f);
                Check(!skill.IsUsing && target.ReceivedHitCount == (dodge ? 0 : 1), "Ranged contact/recovery");
                return new { scenario = dodge ? "ranged-fixed-aim-dodge" : "ranged-delay-flash-projectile", passed = true, hits = target.ReceivedHitCount };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> Cancellation(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponent<EnemyAttackSkill>();
                Set(skill, "<BaseCoolTime>k__BackingField", 0f);
                await Wait(0.15f);
                EnemySkillModule module = enemy.GetModule<EnemySkillModule>();
                Check(module.TryUseAttack(), "Cancel begin");
                AttackTelegraphUI ui = enemy.GetComponentInChildren<AttackTelegraphUI>();
                enemy.SetTarget(null);
                Check(!skill.IsUsing && !ui.IsVisible, "Target switch retained UI");
                enemy.SetTarget(target.transform);
                Check(module.TryUseAttack(), "Disable begin");
                enemy.enabled = false;
                Check(!skill.IsUsing && !ui.IsVisible, "Disable retained UI");
                enemy.enabled = true;
                if (path == Cat)
                {
                    Check(module.TryUseAttack(), "Missing animator case begin");
                    Animator animator = enemy.GetComponentInChildren<Animator>();
                    animator.enabled = false;
                    await Wait(0.45f);
                    Check(!skill.IsUsing && !ui.IsVisible && target.ReceivedHitCount == 0, "Disabled animator did not cancel release");
                    animator.enabled = true;
                }
                Check(module.TryUseAttack(), "Death begin");
                ((Agent)enemy).ApplyDamage(new DamageData { DamageAmount = 999f }, Vector2.zero, Vector2.zero, Vector2.zero);
                enemy.OnAttackWarning(); enemy.OnAttackHit();
                Check(!skill.IsUsing && !ui.IsVisible && enemy.IsDead, "Death retained UI/skill");
                await Wait(0.8f);
                Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Cancelled attack caused damage");
                return new { scenario = path == Cat ? "cat-cancel-cleanup" : "ghost-cancel-cleanup", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> ReleaseDuringAttack(string path, float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase enemy = Spawn(path, arena, x, false);
                EnemyDamageProbe target = Target(arena, x + 1f);
                enemy.SetTarget(target.transform);
                EnemyAttackSkill skill = enemy.GetComponent<EnemyAttackSkill>();
                await ObserveUntil(() => skill.IsPreparing, () => { });
                target.transform.position = enemy.transform.position + Vector3.right * (enemy.ChaseReleaseRange + 2f);
                await Wait(0.1f);
                Check(State(enemy) == "EnemyIdleState" && !skill.IsUsing && !enemy.GetComponentInChildren<AttackTelegraphUI>().IsVisible,
                    "Attack preparation ignored chase release");
                await Wait(0.6f);
                Check(target.ReceivedHitCount == 0 && Shots(enemy).Length == 0, "Attack released after losing pursuit");
                return new { scenario = path == Cat ? "cat-release-cancels-windup" : "ghost-release-cancels-windup", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        private static async Task<object> DelayStat(float x)
        {
            GameObject arena = Arena(x);
            try
            {
                EnemyBase a = Spawn(Ghost, arena, x);
                EnemyBase b = Spawn(Ghost, arena, x + 10f);
                StatData data = AssetDatabase.LoadAssetAtPath<StatData>(Delay);
                a.GetModule<IStatModule>().GetStat(data).SetBaseValue(0.8f);
                EnemyAttackSkill skill = a.GetComponent<EnemyAttackSkill>();
                Check(Mathf.Approximately(skill.AttackDelay, 0.8f) && Mathf.Approximately(b.GetComponent<EnemyAttackSkill>().AttackDelay, 0.5f), "Stat isolation");
                EnemyDamageProbe target = Target(arena, x + 4f);
                a.SetTarget(target.transform);
                await Wait(0.15f);
                Check(a.GetModule<EnemySkillModule>().TryUseAttack(), "Stat begin");
                a.GetModule<IStatModule>().GetStat(data).SetBaseValue(0.1f);
                await Wait(0.5f);
                Check(skill.IsPreparing && Shots(a).Length == 0, "In-flight stat update changed cast deadline");
                a.GetModule<EnemySkillModule>().StopAttack();
                return new { scenario = "per-enemy-delay-stat-and-cast-snapshot", passed = true };
            }
            finally { DestroyShots(x); Object.Destroy(arena); }
        }

        public static async Task<string> PrepareVisualPreview()
        {
            RequirePlay();
            foreach (EnemyBase enemy in Object.FindObjectsByType<EnemyBase>(FindObjectsSortMode.None))
            {
                enemy.GetModule<EnemySkillModule>().StopAttack();
                Set(enemy.GetComponent<EnemyAttackSkill>(), "<BaseCoolTime>k__BackingField", 0f);
                enemy.GetModule<IStatModule>().GetStat(AssetDatabase.LoadAssetAtPath<StatData>(Delay)).SetBaseValue(10f);
            }
            await Wait(0.2f);
            return "Runtime-only preview: preparation extended to 10 seconds. Stop Play to restore original settings.";
        }

        private static EnemyProjectile[] Shots(EnemyBase enemy) => Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None)
            .Where(p => ReferenceEquals(Field(p, "_dealer"), enemy)).ToArray();
        private static void DestroyShots(float x)
        {
            foreach (EnemyProjectile p in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                if (Mathf.Abs(p.transform.position.x - x) < 45f) Object.Destroy(p.gameObject);
        }
        private static string State(EnemyBase enemy) => Field(Field(enemy, "_stateMachine"), "_currentState").GetType().Name;
        private static object Field(object target, string name)
        {
            for (Type type = target.GetType(); type != null; type = type.BaseType)
            {
                FieldInfo field = type.GetField(name, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.DeclaredOnly);
                if (field != null) return field.GetValue(target);
            }
            throw new InvalidOperationException("Field not found: " + name);
        }
        private static void Set(Object target, string field, float value)
        {
            SerializedObject so = new SerializedObject(target);
            so.FindProperty(field).floatValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static GameObject Arena(float x)
        {
            GameObject arena = new GameObject("CTJ Combat Rules runtime " + x);
            GameObject ground = new GameObject("Ground");
            ground.transform.SetParent(arena.transform);
            ground.transform.position = new Vector3(x, -0.5f);
            ground.layer = 6;
            ground.AddComponent<BoxCollider2D>().size = new Vector2(70f, 1f);
            return arena;
        }
        private static EnemyBase Spawn(string path, GameObject arena, float x, bool manual = true)
        {
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path), new Vector3(x, 0.05f), Quaternion.identity, arena.transform);
            if (manual) { Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f); }
            return enemy;
        }
        private static EnemyDamageProbe Target(GameObject arena, float x)
        {
            GameObject go = new GameObject("Combat target");
            go.transform.SetParent(arena.transform);
            go.transform.position = new Vector3(x, 0.02f);
            BoxCollider2D collider = go.AddComponent<BoxCollider2D>();
            collider.size = new Vector2(0.5f, 0.8f);
            collider.offset = new Vector2(0f, 0.4f);
            collider.isTrigger = true;
            return go.AddComponent<EnemyDamageProbe>();
        }
        private static Task Wait(float seconds)
        {
            float end = Time.time + seconds;
            return ObserveUntil(() => Time.time >= end, () => { });
        }
        private static async Task ObserveUntil(Func<bool> condition, Action observe)
        {
            DateTime timeout = DateTime.UtcNow.AddSeconds(12);
            while (!condition())
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < timeout, "Play test timeout");
                observe();
                await Task.Yield();
            }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path ==
            "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Run only in CTJ EnemyArtTest Play Mode");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
