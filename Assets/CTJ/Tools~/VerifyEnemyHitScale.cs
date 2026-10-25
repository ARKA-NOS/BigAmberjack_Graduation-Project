using System;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // VerifyEnemy01.BeginPlay / StopPlay / RestoreScene의 독립 빈 씬에서만 실행합니다.
    public static class VerifyEnemyHitScale
    {
        private static readonly string[] Names =
        { "Catto_Melee", "MadGhost_Ranged", "Enemy01_TwistedArm", "Enemy02_ProjectileRobot" };

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Isolated Play only");
            var tasks = Names.Select((name, i) => ScaleScenario(name, 500f + i * 30f)).ToList();
            tasks.AddRange(Names.Take(3).Select((name, i) => KnockbackScenario(name, 700f + i * 60f)));
            return await Task.WhenAll(tasks);
        }

        private static async Task<object> ScaleScenario(string name, float x)
        {
            GameObject arena = new GameObject("Hit scale verification " + name);
            try
            {
                EnemyBase enemy = Spawn(arena, name, x);
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic; body.constraints = RigidbodyConstraints2D.FreezeAll;
                EnemyHitReactionModule reaction = enemy.GetModule<EnemyHitReactionModule>();
                Set(reaction, "enableKnockback", false); Set(reaction, "enableBlink", false);
                Transform[] targets = Targets(reaction);
                float amount = new SerializedObject(reaction).FindProperty("scalePulseAmount").floatValue;
                float restoreWait = new SerializedObject(reaction).FindProperty("scalePulseDuration").floatValue + 0.05f;
                // 원본 크기가 1이 아니어도 복원되는지 확인합니다.
                foreach (Transform target in targets) target.localScale = new Vector3(1.2f, 0.8f, 1.1f);
                Vector3[] originals = targets.Select(t => t.localScale).ToArray();
                await Wait(0.05f);
                Collider2D[] colliders = enemy.GetComponentsInChildren<Collider2D>(true);
                Bounds[] bounds = colliders.Select(c => c.bounds).ToArray();
                Transform muzzle = enemy.GetComponentsInChildren<Transform>(true).FirstOrDefault(t => t.name == "FirePoint");
                Vector3 muzzlePoint = muzzle != null ? muzzle.position : Vector3.zero;
                Vector3 rootScale = enemy.transform.localScale;
                Vector3 rootPoint = enemy.transform.position;
                Hit(enemy);
                Check(!reaction.IsBlinking, "Scale must work with blink disabled");
                CheckPulse(targets, originals, amount);
                Physics2D.SyncTransforms();
                for (int i = 0; i < colliders.Length; i++)
                {
                    Check(Vector3.Distance(colliders[i].bounds.center, bounds[i].center) < 0.001f &&
                        Vector3.Distance(colliders[i].bounds.size, bounds[i].size) < 0.001f, "Collider changed with graphic scale");
                }
                if (muzzle != null) Check(Vector3.Distance(muzzle.position, muzzlePoint) < 0.001f, "Muzzle moved with scale");
                Check(enemy.transform.localScale == rootScale && enemy.transform.position == rootPoint, "Root changed with scale");
                await Wait(restoreWait); CheckRestored(targets, originals);

                float min = float.MaxValue, max = float.MinValue;
                for (int i = 0; i < 24; i++)
                {
                    Hit(enemy); CheckPulse(targets, originals, amount);
                    float offset = targets[0].localScale.x / originals[0].x - 1f;
                    min = Mathf.Min(min, offset); max = Mathf.Max(max, offset);
                }
                Check(max - min > 0.001f, "Random values did not vary");
                await Wait(restoreWait); CheckRestored(targets, originals);

                // 넓힌 상한이 런타임의 옛 0.2 제한에 막히지 않는지 확인합니다.
                Set(reaction, "scalePulseAmount", 0.8f);
                Hit(enemy); CheckPulse(targets, originals, 0.8f);
                await Wait(restoreWait); CheckRestored(targets, originals);
                Set(reaction, "scalePulseAmount", 0f);
                Hit(enemy); CheckRestored(targets, originals);
                Set(reaction, "scalePulseAmount", amount);

                // 최대 변형에서 부드럽게 원상 복귀하는지 확인합니다.
                Hit(enemy); float peak = Mathf.Abs(targets[0].localScale.x - originals[0].x);
                await Wait(0.07f);
                float middle = Mathf.Abs(targets[0].localScale.x - originals[0].x);
                Check(middle > 0f && middle < peak, "Pulse did not interpolate back");
                await Wait(restoreWait); CheckRestored(targets, originals);

                Transform facing = new SerializedObject(enemy).FindProperty("facingRoot").objectReferenceValue as Transform;
                Hit(enemy);
                if (facing != null)
                {
                    Vector3 scale = facing.localScale; scale.x = -Mathf.Abs(scale.x); facing.localScale = scale;
                    await Wait(restoreWait);
                    Check(facing.localScale.x < 0f, "Pulse reset facing"); CheckRestored(targets, originals);
                }

                Set(reaction, "enableScalePulse", false); await Wait(0.03f); CheckRestored(targets, originals);
                Hit(enemy); CheckRestored(targets, originals);
                Set(reaction, "enableScalePulse", true);
                Hit(enemy); reaction.enabled = false; CheckRestored(targets, originals);
                reaction.enabled = true;
                Hit(enemy); enemy.gameObject.SetActive(false); CheckRestored(targets, originals);
                enemy.gameObject.SetActive(true);
                Hit(enemy); CheckPulse(targets, originals, amount);
                enemy.ApplyDamage(new DamageData { DamageAmount = 10000f }, enemy.transform.position, Vector2.right, Vector2.zero);
                Check(enemy.IsDead, "Lethal damage"); CheckRestored(targets, originals);
                return new { prefab = name, passed = true, tests = "random pulse / repeat / interpolation / nonunit restore / collider and muzzle isolation / facing / opt-out / disable / death" };
            }
            finally { Object.Destroy(arena); }
        }

        private static async Task<object> KnockbackScenario(string name, float x)
        {
            GameObject arena = new GameObject("Knockback comparison " + name);
            try
            {
                GameObject ground = new GameObject("Ground"); ground.transform.SetParent(arena.transform);
                ground.layer = 6; ground.transform.position = new Vector3(x + 10f, -0.5f);
                ground.AddComponent<BoxCollider2D>().size = new Vector2(45f, 1f);
                EnemyBase before = Spawn(arena, name, x), after = Spawn(arena, name, x + 20f);
                Set(before.GetModule<EnemyHitReactionModule>(), "knockbackSpeed", 5.1f);
                Check(Mathf.Approximately(new SerializedObject(after.GetModule<EnemyHitReactionModule>()).FindProperty("knockbackSpeed").floatValue, 6.4f), "Updated prefab speed");
                await Wait(0.3f);
                float a = before.transform.position.x, b = after.transform.position.x;
                Hit(before); Hit(after); await Wait(0.3f);
                float oldDistance = before.transform.position.x - a, newDistance = after.transform.position.x - b;
                Check(oldDistance > 0.1f && newDistance / oldDistance > 1.2f && newDistance / oldDistance < 1.3f, "Knockback distance ratio");
                Check(!after.IsHitReacting, "Hit stun lingered");
                Check(Mathf.Abs(after.GetComponent<Rigidbody2D>().linearVelocityX) < 0.01f, "Knockback did not stop");
                return new { prefab = name, passed = true, oldDistance, newDistance, ratio = newDistance / oldDistance };
            }
            finally { Object.Destroy(arena); }
        }

        private static EnemyBase Spawn(GameObject arena, string name, float x)
        {
            GameObject staging = new GameObject("Inactive setup"); staging.SetActive(false);
            try
            {
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(
                    "Assets/CTJ/Prototypes/Prefabs/" + name + ".prefab"), new Vector3(x, 0.05f), Quaternion.identity, staging.transform);
                Set(enemy, "detectionRange", 0f); Set(enemy, "moveSpeed", 0f);
                EnemyPatrolModule patrol = enemy.GetComponentInChildren<EnemyPatrolModule>(); if (patrol != null) patrol.enabled = false;
                enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                enemy.transform.SetParent(arena.transform, true);
                return enemy;
            }
            finally { Object.Destroy(staging); }
        }

        private static Transform[] Targets(EnemyHitReactionModule reaction)
        {
            SerializedProperty p = new SerializedObject(reaction).FindProperty("scaleTargets");
            return Enumerable.Range(0, p.arraySize).Select(i => (Transform)p.GetArrayElementAtIndex(i).objectReferenceValue).ToArray();
        }

        private static void Hit(EnemyBase enemy) => enemy.ApplyDamage(new DamageData { DamageAmount = 0.01f },
            enemy.transform.position, Vector2.right, Vector2.zero);

        private static void CheckPulse(Transform[] targets, Vector3[] originals, float amount)
        {
            for (int i = 0; i < targets.Length; i++)
            {
                Vector3 scale = targets[i].localScale;
                float dx = scale.x / originals[i].x - 1f, dy = scale.y / originals[i].y - 1f;
                Check(Mathf.Abs(dx) >= amount * 0.5f - 0.0001f && Mathf.Abs(dx) <= amount + 0.0001f && Mathf.Abs(dx + dy) < 0.0001f &&
                    scale.z == originals[i].z, "Invalid pulse amplitude / accumulated scale");
            }
        }

        private static void CheckRestored(Transform[] targets, Vector3[] originals)
        {
            for (int i = 0; i < targets.Length; i++) Check(targets[i].localScale == originals[i], "Scale not restored");
        }

        private static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field);
            Check(p != null, "Missing field " + field);
            if (value is float f) p.floatValue = f; else if (value is bool b) p.boolValue = b;
            else throw new InvalidOperationException("Unsupported value");
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds; DateTime deadline = DateTime.UtcNow.AddSeconds(15);
            while (Time.time < until)
            {
                Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Timed out");
                await Task.Yield();
            }
        }

        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
