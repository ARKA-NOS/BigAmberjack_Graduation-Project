using System;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Testing;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // EnemyArtTest의 Play 인스턴스만 사용합니다. 씬/프리팹을 저장하지 않습니다.
    public static class VerifyEnemyMovement
    {
        private const string CatPath = "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab";
        private const string GhostPath = "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab";

        public static object ReadState()
        {
            return new { playing = EditorApplication.isPlaying, compiling = EditorApplication.isCompiling,
                scene = SceneManager.GetActiveScene().path };
        }

        public static object ValidatePrefabs()
        {
            return new[] { CatPath, GhostPath }.Select(path =>
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(path);
                EnemyMovementModule movement = enemy.GetComponentInChildren<EnemyMovementModule>();
                if (movement == null || GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(enemy.gameObject) != 0)
                    throw new InvalidOperationException("Missing movement module/script: " + path);
                SerializedObject serialized = new SerializedObject(movement);
                return new { path, bodyAssigned = serialized.FindProperty("bodyCollider").objectReferenceValue != null,
                    groundMask = serialized.FindProperty("terrainLayers").intValue,
                    jumpHeight = serialized.FindProperty("jumpHeight").floatValue };
            }).ToArray();
        }

        public static async Task<object> RunScenarios()
        {
            RequireTestScene();
            object[] results = await Task.WhenAll(
                RunCase("vertical", CatPath, 2f, 1000f),
                RunCase("ledge", CatPath, 2f, 1100f),
                RunCase("ledge", CatPath, 1f, 1200f),
                RunCase("ledge", GhostPath, 1f, 1300f),
                RunCase("step", CatPath, 2f, 1400f),
                RunCase("flat", CatPath, 2f, 1500f),
                RunCase("flat", GhostPath, 2f, 1600f),
                RunCase("ceiling", CatPath, 2f, 1700f),
                RunCase("tooHigh", CatPath, 2f, 1800f),
                RunCase("death", CatPath, 2f, 1900f),
                RunCase("centerPivot", CatPath, 2f, 2000f),
                RunCase("reverse", CatPath, 2f, 2100f));
            return results;
        }

        private static async Task<object> RunCase(string scenario, string prefab, float scale, float originX)
        {
            GameObject arena = new GameObject("CTJ Movement " + scenario + " (runtime only)");
            try
            {
                Box(arena.transform, "Ground", new Vector2(originX, -0.5f), new Vector2(20f, 1f));
                if (scenario == "ledge")
                    Box(arena.transform, "Solid upper platform", new Vector2(originX, 1.9f), new Vector2(1.5f, 0.2f));
                if (scenario == "step")
                    Box(arena.transform, "Low step", new Vector2(originX + 1.4f, 0.3f), new Vector2(0.5f, 0.6f));
                if (scenario == "ceiling")
                    Box(arena.transform, "Wide low ceiling", new Vector2(originX, 1.6f), new Vector2(10f, 0.2f));

                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(prefab),
                    new Vector3(originX, 0.05f, 0f), Quaternion.identity, arena.transform);
                enemy.transform.localScale = Vector3.one * scale;
                EnemyMovementModule movement = enemy.GetModule<EnemyMovementModule>();
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                GameObject target = new GameObject("Movement target");
                target.transform.SetParent(arena.transform);
                float targetY = scenario == "ledge" ? 2.02f : scenario == "ceiling" ? 2f :
                    scenario == "tooHigh" ? 5f : scenario == "centerPivot" ? 0.82f :
                    scenario == "flat" || scenario == "step" || scenario == "reverse" ? 0.02f : 2.5f;
                float targetX = originX + (scenario == "flat" || scenario == "step" ||
                    scenario == "centerPivot" || scenario == "reverse" ? 3.5f : 0f);
                target.transform.position = new Vector3(targetX, targetY, 0f);
                BoxCollider2D targetCollider = target.AddComponent<BoxCollider2D>();
                targetCollider.size = new Vector2(0.5f, 0.8f);
                targetCollider.offset = new Vector2(0f, 0.4f);
                if (scenario == "centerPivot")
                {
                    targetCollider.size = new Vector2(0.5f, 1.6f);
                    targetCollider.offset = Vector2.zero;
                }
                targetCollider.isTrigger = true;
                EnemyDamageProbe probe = target.AddComponent<EnemyDamageProbe>();
                enemy.SetTarget(target.transform);
                Physics2D.SyncTransforms();
                float maxY = 0f;
                float maxSide = 0f;
                bool landedOnPlatform = false;
                bool usedDetour = false;
                bool deadStable = false;
                bool damagedBeforeLanding = false;
                await Observe(scenario == "ledge" ? 6f : 3f, () =>
                {
                    if (enemy == null)
                        return;
                    maxY = Mathf.Max(maxY, enemy.transform.position.y);
                    maxSide = Mathf.Max(maxSide, Mathf.Abs(enemy.transform.position.x - originX));
                    usedDetour |= movement.IsDetouring;
                    landedOnPlatform |= movement.IsGrounded && enemy.transform.position.y > 1.9f;
                    if (scenario == "ledge" && !landedOnPlatform && probe.ReceivedHitCount > 0)
                        damagedBeforeLanding = true;
                    if (scenario == "death" && body.linearVelocityY > 1f && enemy.transform.position.y > 0.2f && !enemy.IsDead)
                    {
                        enemy.GetModule<Lrw.Script.Agent.HealthSystem.IHealthModule>().CurrentHealth = 0f;
                        deadStable = enemy.IsDead && !body.simulated && body.linearVelocity.sqrMagnitude < 0.0001f;
                    }
                });
                bool reversed = false;
                if (scenario == "reverse")
                {
                    int previousHits = probe.ReceivedHitCount;
                    target.transform.position = enemy.transform.position + Vector3.left * 2.5f;
                    await Observe(3f, () => { });
                    reversed = enemy.transform.Find("Visual").localScale.x < 0f && probe.ReceivedHitCount > previousHits;
                }
                bool passed = scenario == "vertical" ? maxY > 2f && maxY < 3.2f :
                    scenario == "ledge" ? usedDetour && landedOnPlatform && !damagedBeforeLanding && probe.ReceivedHitCount > 0 :
                    scenario == "step" ? maxY > 0.6f && maxSide > 2f && probe.ReceivedHitCount > 0 :
                    scenario == "flat" || scenario == "centerPivot" ? maxY < 0.2f && probe.ReceivedHitCount > 0 :
                    scenario == "reverse" ? reversed :
                    scenario == "death" ? deadStable && maxY > 0.2f && enemy == null : maxY < 0.2f;
                return new { scenario, prefab, scale, passed, maxY, maxSide, usedDetour, landedOnPlatform, damagedBeforeLanding,
                    hits = probe.ReceivedHitCount, finalPosition = enemy != null ? enemy.transform.position.ToString() : "destroyed" };
            }
            finally { Object.Destroy(arena); }
        }

        public static async Task<object> BaselineVertical()
        {
            RequireTestScene();
            GameObject arena = new GameObject("CTJ Movement Baseline (runtime only)");
            try
            {
                Box(arena.transform, "Ground", new Vector2(1000f, -0.5f), new Vector2(20f, 1f));
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(CatPath),
                    new Vector3(1000f, 0.05f, 0f), Quaternion.identity, arena.transform);
                GameObject target = new GameObject("Target directly above");
                target.transform.SetParent(arena.transform);
                target.transform.position = new Vector3(1000f, 2.5f, 0f);
                enemy.SetTarget(target.transform);
                float minX = enemy.transform.position.x;
                float maxX = minX;
                float maxY = enemy.transform.position.y;
                await Observe(2f, () =>
                {
                    minX = Mathf.Min(minX, enemy.transform.position.x);
                    maxX = Mathf.Max(maxX, enemy.transform.position.x);
                    maxY = Mathf.Max(maxY, enemy.transform.position.y);
                });
                return new { horizontalTravel = maxX - minX, maxY,
                    finalPosition = enemy.transform.position.ToString(),
                    velocity = enemy.GetComponent<Rigidbody2D>().linearVelocity.ToString() };
            }
            finally { Object.Destroy(arena); }
        }

        private static BoxCollider2D Box(Transform parent, string name, Vector2 position, Vector2 size)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent);
            obj.transform.position = position;
            obj.layer = LayerMask.NameToLayer("Ground");
            BoxCollider2D box = obj.AddComponent<BoxCollider2D>();
            box.size = size;
            return box;
        }

        private static void RequireTestScene()
        {
            if (!EditorApplication.isPlaying || SceneManager.GetActiveScene().path !=
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity")
                throw new InvalidOperationException("Run only in the isolated EnemyArtTest Play session.");
        }

        private static async Task Observe(float seconds, Action sample)
        {
            float until = Time.time + seconds;
            DateTime deadline = DateTime.UtcNow.AddSeconds(20);
            while (Time.time < until)
            {
                if (!EditorApplication.isPlaying || DateTime.UtcNow > deadline)
                    throw new InvalidOperationException("Movement test timed out or Play Mode stopped.");
                sample();
                await Task.Yield();
            }
        }
    }
}
