using System;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemy04RangeStability
    {
        public static async Task<object> Diagnose()
        {
            RequirePlay();
            return await Task.WhenAll(Platform(false, false), Platform(true, false));
        }
        public static async Task<object> RunPlay()
        {
            RequirePlay();
            return await Task.WhenAll(Platform(false, true), Platform(true, true));
        }
        private static async Task<object> Platform(bool below, bool assertFixed)
        {
            float x = below ? 15100f : 15000f;
            GameObject root = new GameObject("Range stability " + below);
            try
            {
                Box(root, new Vector2(x, -0.5f), new Vector2(50f, 1f));
                // 위층 바닥이 시야를 가립니다. 아래쪽 타깃 검증은 적과 타깃 높이를 뒤집습니다.
                Box(root, new Vector2(x, 2.7f), new Vector2(45f, 0.6f));
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab"),
                    new Vector3(x, below ? 3f : 0f), Quaternion.identity, root.transform);
                GameObject target = new GameObject("Target"); target.transform.SetParent(root.transform);
                target.transform.position = new Vector3(x + 4f, below ? 0f : 3f);
                BoxCollider2D targetBody = target.AddComponent<BoxCollider2D>(); targetBody.isTrigger = true;
                targetBody.size = new Vector2(0.5f, 2f); targetBody.offset = Vector2.up;
                target.AddComponent<EnemyDamageProbe>(); enemy.SetTarget(target.transform);
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>(); Transform visual = enemy.transform.Find("Visual");
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                // 생성 직후의 착지/첫 거리 보정이 끝난 뒤 반복 진동을 측정합니다.
                await Wait(0.6f);
                float min = body.position.x, max = min, end = Time.time + 2f, oldSide = Mathf.Sign(visual.localScale.x);
                int turns = 0, movingFrames = 0;
                while (Time.time < end)
                {
                    float side = Mathf.Sign(visual.localScale.x); if (side != oldSide) turns++; oldSide = side;
                    if (Mathf.Abs(body.linearVelocityX) > 0.1f) movingFrames++;
                    min = Mathf.Min(min, body.position.x); max = Mathf.Max(max, body.position.x);
                    Check(!shot.IsUsing, "Must not start attack through platform");
                    await Task.Yield();
                }
                if (assertFixed)
                {
                    Check(turns == 0 && movingFrames == 0 && max - min < 0.001f, "Oscillation remained: turns=" + turns + ", movement=" + (max - min));
                    target.transform.position = new Vector3(enemy.transform.position.x + 2f, below ? 0f : 3f);
                    await Until(() => body.linearVelocityX < -0.1f, "Retreat while sight is blocked");
                    await Until(() => Vector2.Distance(enemy.transform.position, target.transform.position) >= 5f && Mathf.Abs(body.linearVelocityX) < 0.01f,
                        "Hold after retreat with blocked sight");
                    float heldX = body.position.x;
                    await Wait(0.4f);
                    Check(Mathf.Abs(body.position.x - heldX) < 0.001f && visual.localScale.x > 0f && !shot.IsUsing,
                        "Retreat must end in a stable hold");
                    target.transform.position = new Vector3(enemy.transform.position.x + 12f, below ? 0f : 3f);
                    await Until(() => body.linearVelocityX > 0.1f, "Approach distant blocked target");
                    await Until(() => Vector2.Distance(enemy.transform.position, target.transform.position) <= 11f && Mathf.Abs(body.linearVelocityX) < 0.01f,
                        "Hold after approach with blocked sight");
                    heldX = body.position.x;
                    await Wait(0.4f);
                    Check(Mathf.Abs(body.position.x - heldX) < 0.001f && !shot.IsUsing, "Approach must end in a stable hold");
                    // 플레이어가 같은 발판으로 내려오면 멈춘 상태에서 정상 사격으로 복귀해야 합니다.
                    target.transform.position = new Vector3(enemy.transform.position.x + 8f, below ? 3f : 0f);
                    await Until(() => shot.IsPreparing, "Resume shooting after landing");
                    Check(shot.CastDirection == Vector2.right, "Horizontal shot after blocked sight");
                }
                return new { passed = assertFixed, below, turns, movingFrames, movement = max - min };
            }
            finally
            {
                root.SetActive(false); Object.Destroy(root);
                foreach (EnemyProjectile arrow in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (Mathf.Abs(arrow.transform.position.x - x) < 30f) Object.Destroy(arrow.gameObject);
            }
        }
        private static BoxCollider2D Box(GameObject parent, Vector2 position, Vector2 size)
        {
            GameObject floor = new GameObject("Ground"); floor.layer = 6; floor.transform.SetParent(parent.transform);
            floor.transform.position = position; BoxCollider2D box = floor.AddComponent<BoxCollider2D>(); box.size = size; return box;
        }
        private static async Task Wait(float duration) { float end = Time.time + duration; await Until(() => Time.time >= end, "Wait"); }
        private static async Task Until(Func<bool> condition, string label)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Timeout: " + label); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
