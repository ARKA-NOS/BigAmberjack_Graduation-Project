using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using DevLib.BattleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.StatSystem;
using SDW.Scripts.Maps;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemy01
    {
        private const string Path = "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab";
        private const string SessionKey = "CTJ.Enemy01.Verifying";
        private const string RestoreKey = "CTJ.Enemy01.OriginalScene";

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            EnemyBase prefab = AssetDatabase.LoadAssetAtPath<EnemyBase>(Path);
            Check(prefab != null, "Prefab missing");
            foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
            EnemyMeleeSkill attack = prefab.GetComponentInChildren<EnemyMeleeSkill>();
            EnemySkillModule skills = prefab.GetComponentInChildren<EnemySkillModule>();
            Check(skills.Skills.Count == 1 && skills.Skills[0].Skill == attack, "Skill entry");
            Check(attack.SkillSo != null && Mathf.Approximately(attack.CoolTime, 2.5f), "Skill SO / cooldown");
            SerializedObject a = new SerializedObject(attack);
            Check(a.FindProperty("attackAnimator").objectReferenceValue != null && a.FindProperty("attackHitbox").objectReferenceValue != null,
                "Animation / hitbox references");
            Check(a.FindProperty("preparationWarning").objectReferenceValue == prefab.GetComponentInChildren<EnemyAttackWarningModule>(), "Warning reference");
            Check(Mathf.Approximately(a.FindProperty("damage").floatValue, 8f), "Damage 8");
            Check(Mathf.Approximately(new SerializedObject(prefab).FindProperty("moveSpeed").floatValue, 6f), "Chase speed 6");
            Check(Mathf.Approximately(new SerializedObject(prefab.GetComponentInChildren<EnemyMovementModule>()).FindProperty("jumpHeight").floatValue, 0f), "No jump");
            StatGroup stats = (StatGroup)new SerializedObject(prefab.GetComponentInChildren<StatModule>()).FindProperty("baseStats").objectReferenceValue;
            Check(stats.Stats[0].BaseValue == 50f && Mathf.Approximately(stats.Stats[1].BaseValue + attack.PostBlinkDelay, 0.8f), "HP / preparation");
            Check(prefab.GetComponentInChildren<EnemyCurrencyRewardModule>() != null && prefab.GetComponentInChildren<EnemyDeathModule>() != null, "Reward/death wiring");

            Check(new SerializedObject(prefab.GetComponentInChildren<EnemyPatrolModule>()).FindProperty("centerOverride") == null,
                "Obsolete room/override center field remains");
            return new { passed = true, scope = "prefab / references / stats / spawn-only patrol configuration" };
        }

        public static string BeginPlay()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && SceneManager.sceneCount == 1, "Single edit scene required");
            Scene scene = SceneManager.GetActiveScene();
            Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save the original scene first");
            SessionState.SetString(RestoreKey, scene.path);
            SessionState.SetBool(SessionKey, true);
            EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            EditorApplication.isPlaying = true;
            return "Starting isolated, unsaved empty scene. Original scene will be reopened without saving.";
        }

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool(SessionKey, false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
            return await Task.WhenAll(PatrolAndChase(), Obstacle(false), Obstacle(true), Combat(), OriginalCatto());
        }

        private static async Task<object> PatrolAndChase()
        {
            GameObject arena = Arena(1000f, 30f);
            try
            {
                RoomDefinition room = arena.AddComponent<RoomDefinition>();
                EnemyBase enemy = Spawn(arena, 1001f);
                EnemyPatrolModule patrol = enemy.GetModule<EnemyPatrolModule>();
                Check(Mathf.Approximately(patrol.Center.x, 1001f), "Must use spawn, not room/portal center");
                Set(patrol, "centerOffsetX", 0.5f);
                Check(Mathf.Approximately(patrol.Center.x, 1001.5f), "Spawn-relative center offset");
                Set(patrol, "centerOffsetX", 0f);
                float min = enemy.transform.position.x, max = min;
                float end = Time.time + 3f;
                while (Time.time < end)
                {
                    min = Mathf.Min(min, enemy.transform.position.x); max = Mathf.Max(max, enemy.transform.position.x);
                    Check(enemy.transform.position.x >= patrol.MinX - 0.05f && enemy.transform.position.x <= patrol.MaxX + 0.05f, "Patrol escaped range");
                    await Task.Yield();
                }
                Check(max - min > 0.5f, "Patrol did not walk");
                float x = enemy.transform.position.x;
                EnemyDamageProbe target = Target(arena, x + 5f);
                enemy.SetTarget(target.transform);
                await Wait(0.2f);
                Check(enemy.GetComponent<Rigidbody2D>().linearVelocityX > 5f, "Chase did not override patrol");
                target.transform.position += Vector3.right * 40f;
                await Wait(0.25f);
                Check(!enemy.IsAttackInProgress && Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) <= 2.01f, "Patrol did not resume after chase release");
                Set(patrol, "halfWidth", 1f);
                await Wait(3f);
                Check(enemy.transform.position.x >= patrol.MinX - 0.05f && enemy.transform.position.x <= patrol.MaxX + 0.05f, "Live range change");
                Check(Mathf.Approximately(patrol.Center.x, 1001f), "Chase/release changed spawn center");
                patrol.enabled = false;
                await Wait(0.1f);
                Check(Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) < 0.01f, "Disabled patrol did not stop");
                patrol.enabled = true;
                await Until(() => Mathf.Abs(enemy.GetComponent<Rigidbody2D>().linearVelocityX) > 0.1f);
                enemy.gameObject.SetActive(false); enemy.gameObject.SetActive(true);
                enemy.transform.SetParent(null, true); enemy.transform.SetParent(arena.transform, true);
                Check(Mathf.Approximately(patrol.Center.x, 1001f), "Reactivation/reparent changed spawn center");
                return new { scenario = "spawn centered patrol / bounds / chase / release / width / offset / reactivation", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Obstacle(bool cliff)
        {
            float x = cliff ? 1200f : 1100f;
            GameObject arena = Arena(x, cliff ? 4f : 30f);
            try
            {
                if (!cliff)
                {
                    Box(arena, new Vector2(x - 1.5f, 1f), new Vector2(0.2f, 2f));
                    Box(arena, new Vector2(x + 1.5f, 1f), new Vector2(0.2f, 2f));
                }
                EnemyBase enemy = Spawn(arena, x);
                bool left = false, right = false;
                float end = Time.time + 5f;
                while (Time.time < end)
                {
                    float v = enemy.GetComponent<Rigidbody2D>().linearVelocityX;
                    left |= v < -0.1f; right |= v > 0.1f;
                    Check(Mathf.Abs(enemy.transform.position.x - x) < (cliff ? 2f : 1.5f) && enemy.transform.position.y > -0.1f,
                        "Wall/cliff safety");
                    await Task.Yield();
                }
                Check(left && right, "Did not reverse at obstacle");
                return new { scenario = cliff ? "cliff avoidance" : "wall turnaround", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> Combat()
        {
            GameObject arena = Arena(1300f, 30f);
            try
            {
                EnemyBase enemy = Spawn(arena, 1300f, true);
                EnemyDamageProbe target = Target(arena, 1300.8f);
                enemy.SetTarget(target.transform);
                EnemySkillModule skills = enemy.GetModule<EnemySkillModule>();
                EnemyMeleeSkill attack = enemy.GetComponentInChildren<EnemyMeleeSkill>();
                EnemyAttackWarningModule warning = enemy.GetModule<EnemyAttackWarningModule>();
                SpriteRenderer sprite = enemy.GetComponentInChildren<SpriteRenderer>();
                Material original = sprite.sharedMaterial;
                Color color = sprite.color;
                INewHealthModule hp = enemy.GetModule<INewHealthModule>();
                await Wait(0.2f);
                Check(hp.CurrentHealth == 50f && skills.TryUseAttack(), "Initial HP / attack start");
                float start = Time.time;
                await Until(() => warning.IsShowing);
                float redAt = Time.time;
                Check(redAt - start >= 0.55f && sprite.color.r == 1f && sprite.color.g == 0f && sprite.color.b == 0f,
                    "Final preparation is solid red");
                Check(target.ReceivedHitCount == 0, "Damage before warning hold finished");
                await Until(() => target.ReceivedHitCount == 1);
                Check(Time.time - redAt >= 0.19f && Time.time - start >= 0.79f && target.TotalDamage == 8f, "Warning duration / release / 8 damage");
                Check(!warning.IsShowing && sprite.sharedMaterial == original && sprite.color == color, "Warning restored on release");
                enemy.OnAttackHit(); enemy.OnAttackHit();
                Check(target.ReceivedHitCount == 1, "Duplicate hit event");
                await Until(() => !skills.IsBusy);
                Check(!skills.TryUseAttack() && attack.RemainingCoolTime > 0f, "Cooldown prevents repeat");
                await Until(() => attack.RemainingCoolTime <= 0f);
                Check(skills.TryUseAttack(), "Second attack");
                await Until(() => warning.IsShowing);
                enemy.ApplyDamage(new DamageData { DamageAmount = 12f }, enemy.transform.position, Vector2.right, Vector2.zero);
                Check(hp.CurrentHealth == 38f && !skills.IsBusy && !warning.IsShowing && enemy.IsHitReacting, "Hit cancels preparation and subtracts HP");
                await Wait(0.45f);
                Check(sprite.color == color && sprite.sharedMaterial == original && target.ReceivedHitCount == 1, "No lingering red / no cancelled damage");

                // 피격 취소를 끈 경우에도 피격 원본으로 경고색이 저장되지 않아야 합니다.
                Set(enemy.GetModule<EnemyHitReactionModule>(), "cancelAttackOnHit", false);
                target.transform.position = enemy.transform.position + new Vector3(0.8f, 0f, 0f);
                await Until(() => attack.RemainingCoolTime <= 0f);
                Check(skills.TryUseAttack(), "Non-cancelling attack");
                await Until(() => warning.IsShowing);
                enemy.ApplyDamage(new DamageData { DamageAmount = 1f }, enemy.transform.position, Vector2.right, Vector2.zero);
                await Wait(0.5f);
                Check(sprite.color == color && sprite.sharedMaterial == original, "Overlapping hit and warning restore");
                enemy.GetModule<EnemyCurrencyRewardModule>().enabled = false;
                enemy.GetModule<EnemyDeathModule>().enabled = false;
                enemy.ApplyDamage(new DamageData { DamageAmount = 100f }, enemy.transform.position, Vector2.zero, Vector2.zero);
                Check(enemy.IsDead && !warning.IsShowing && !skills.IsBusy, "Death cleanup");
                return new { scenario = "HP / timed red warning / damage / cooldown / cancellation / overlapping flash / death", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> OriginalCatto()
        {
            GameObject arena = Arena(1400f, 30f);
            try
            {
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(
                    "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab"), new Vector3(1400f, 0.05f), Quaternion.identity, arena.transform);
                await Wait(0.7f);
                Check(enemy.GetModule<EnemyPatrolModule>() == null && enemy.GetModule<EnemyAttackWarningModule>() == null &&
                    Mathf.Abs(enemy.transform.position.x - 1400f) < 0.01f, "Original Catto behavior changed");
                return new { scenario = "original Catto stays idle without optional modules", passed = true };
            }
            finally { Object.Destroy(arena); }
        }

        public static string StopPlay()
        {
            Check(SessionState.GetBool(SessionKey, false), "Not our verification session");
            EditorApplication.isPlaying = false;
            return "Stopping verification Play; call RestoreScene after Play exits.";
        }

        public static string RestoreScene()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && SessionState.GetBool(SessionKey, false), "Stop verification first");
            Check(SceneManager.GetActiveScene().path == "" && SceneManager.GetActiveScene().rootCount == 0, "Unexpected scene content; do not replace");
            string path = SessionState.GetString(RestoreKey, "");
            Check(!string.IsNullOrEmpty(path), "Restore path missing");
            EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
            SessionState.SetBool(SessionKey, false);
            return "Restored without saving: " + path;
        }

        private static EnemyBase Spawn(GameObject arena, float x, bool manualCombat = false)
        {
            GameObject inactive = new GameObject("Configuration staging"); inactive.SetActive(false);
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Path), new Vector3(x, 0.05f), Quaternion.identity, inactive.transform);
            // 검증 범위/시간은 테스트 복제본에만 고정합니다. 프리팹에서 조정한 배회 수치는 보존합니다.
            Set(enemy.GetComponentInChildren<EnemyPatrolModule>(), "halfWidth", 3f);
            Set(enemy.GetComponentInChildren<EnemyPatrolModule>(), "pauseDuration", 0.6f);
            if (manualCombat)
            {
                Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
                enemy.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            }
            enemy.transform.SetParent(arena.transform, true);
            Object.Destroy(inactive);
            return enemy;
        }

        private static GameObject Arena(float x, float width)
        {
            GameObject arena = new GameObject("Enemy01 verification " + x);
            arena.transform.position = new Vector3(x, 0f);
            Box(arena, new Vector2(x, -0.5f), new Vector2(width, 1f));
            return arena;
        }

        private static void Box(GameObject arena, Vector2 point, Vector2 size)
        {
            GameObject box = Child(arena, "Ground", point).gameObject;
            box.layer = 6; box.AddComponent<BoxCollider2D>().size = size;
        }

        private static Transform Child(GameObject parent, string name, Vector2 position)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent.transform); child.transform.position = position;
            return child.transform;
        }

        private static EnemyDamageProbe Target(GameObject arena, float x)
        {
            GameObject target = Child(arena, "Damage target", new Vector2(x, 0.05f)).gameObject;
            BoxCollider2D collider = target.AddComponent<BoxCollider2D>();
            collider.isTrigger = true; collider.offset = new Vector2(0f, 0.4f); collider.size = new Vector2(0.5f, 0.8f);
            return target.AddComponent<EnemyDamageProbe>();
        }

        private static void Set(Object obj, string field, object value)
        {
            SerializedObject so = new SerializedObject(obj);
            SerializedProperty p = so.FindProperty(field);
            Check(p != null, "Missing field: " + field);
            if (value is float f) p.floatValue = f;
            else if (value is bool b) p.boolValue = b;
            else if (value is Object reference) p.objectReferenceValue = reference;
            else throw new InvalidOperationException("Unsupported setting");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds;
            await Until(() => Time.time >= until);
        }

        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (!condition())
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Runtime verification timeout");
                await Task.Yield();
            }
        }

        private static void Check(bool valid, string message)
        {
            if (!valid) throw new InvalidOperationException(message);
        }
    }
}
