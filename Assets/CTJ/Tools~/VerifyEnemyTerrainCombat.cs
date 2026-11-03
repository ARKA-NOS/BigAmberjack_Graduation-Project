using System;
using System.Collections.Generic;
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
    // 실제 프리팹 복제본과 물리를 사용하는 독립 씬 검증. 원본 에셋/씬은 저장하지 않습니다.
    public static class VerifyEnemyTerrainCombat
    {
        private const string Folder = "Assets/CTJ/Prototypes/Prefabs/";
        private static readonly string[] Names = { "Enemy01_TwistedArm", "Enemy02_ProjectileRobot", "Enemy04_MechanicArcher", "Catto_Melee", "MadGhost_Ranged" };

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            foreach (string name in Names)
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>(Folder + name + ".prefab");
                Check(enemy != null, "Missing prefab " + name);
                foreach (Component component in enemy.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing component " + name);
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Broken reference " + name + "/" + property.propertyPath);
                }
                EnemyMovementModule movement = enemy.GetComponentInChildren<EnemyMovementModule>();
                SerializedObject settings = new SerializedObject(movement);
                Collider2D body = (Collider2D)settings.FindProperty("bodyCollider").objectReferenceValue;
                Check((settings.FindProperty("terrainLayers").intValue & 1088) == 1088 &&
                    (body.includeLayers.value & 1088) == 1088 && (body.excludeLayers.value & 1088) == 0,
                    "Ground / Platform masks " + name);
            }
            return new { passed = true, prefabs = Names.Length };
        }

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Isolated Play only");
            var tasks = new List<Task<object>>();
            int index = 0;
            foreach (string name in new[] { Names[0], Names[3], Names[4] })
                foreach (int layer in new[] { 6, 10 })
                    foreach (int side in new[] { -1, 1 })
                        tasks.Add(Cliff(2000f + index++ * 50f, name, layer, side, false));
            tasks.Add(Cliff(2700f, Names[3], 6, 1, true));
            tasks.Add(Cliff(2750f, Names[4], 10, -1, true));
            tasks.Add(Archer(3000f, -1, false));
            tasks.Add(Archer(3050f, 1, false));
            tasks.Add(Archer(3100f, -1, true));
            tasks.Add(Zombie(3200f, 1));
            tasks.Add(Zombie(3250f, -1));
            return await Task.WhenAll(tasks);
        }

        private static async Task<object> Cliff(float x, string name, int layer, int side, bool jump)
        {
            GameObject root = new GameObject("Cliff " + name);
            try
            {
                Box(root, new Vector2(x, -0.5f), new Vector2(6f, 1f), layer);
                // 낮은 층이 있어도 현재 발판에서 내려가면 안 됩니다.
                Box(root, new Vector2(x, -4.5f), new Vector2(30f, 1f), 6);
                EnemyBase enemy = Spawn(root, name, x, false);
                enemy.GetComponentInChildren<EnemySkillModule>().enabled = false;
                EnemyDamageProbe target = Target(root, new Vector2(x + side * (jump ? 3.5f : 5f), jump ? 1.5f : -3f));
                enemy.SetTarget(target.transform);
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                Collider2D terrainBody = enemy.transform.Find("TerrainBody").GetComponent<Collider2D>();
                float maxTravel = 0f;
                float end = Time.time + 2.8f;
                while (Time.time < end)
                {
                    Check(body.position.y > -0.06f, "Fell off " + name + "/" + layer + "/" + side + "/jump=" + jump);
                    Check(terrainBody.bounds.min.x >= x - 3.01f && terrainBody.bounds.max.x <= x + 3.01f,
                        "Body crossed edge " + name);
                    maxTravel = Mathf.Max(maxTravel, Mathf.Abs(body.position.x - x));
                    await Task.Yield();
                }
                Check(maxTravel > 0.5f, "Enemy never chased " + name);
                target.transform.position = new Vector3(x - side * 1.5f, -3f);
                await Until(() => body.linearVelocityX * side < -0.1f, "Resume away from cliff " + name);
                return new { passed = true, scenario = "chase cliff", name, layer, side, jump, maxTravel };
            }
            finally { root.SetActive(false); Object.Destroy(root); }
        }

        private static async Task<object> Archer(float x, int wallSide, bool cliff)
        {
            GameObject root = new GameObject("Archer blocked retreat");
            try
            {
                Box(root, new Vector2(x, -0.5f), new Vector2(cliff ? 6f : 30f, 1f), 6);
                BoxCollider2D wall = cliff ? null : Box(root, new Vector2(x + wallSide * 2.5f, 1.5f), new Vector2(0.3f, 3f), 6);
                EnemyBase enemy = Spawn(root, Names[2], x, false);
                EnemyDamageProbe target = Target(root, new Vector2(x - wallSide, 0.05f));
                enemy.SetTarget(target.transform);
                Rigidbody2D body = enemy.GetComponent<Rigidbody2D>();
                Transform visual = enemy.transform.Find("Visual");
                EnemyProjectileSkill shot = enemy.GetComponentInChildren<EnemyProjectileSkill>();
                await Until(() => body.linearVelocityX * wallSide > 0.1f, "Retreat before wall");
                Check(!shot.IsUsing, "Close attack on open ground");
                await Until(() => shot.IsPreparing, "Attack instead of pushing wall");
                Check(Vector2.Distance(enemy.transform.position, target.transform.position) < 5f,
                    "Did not test minimum range exception");
                Check(Mathf.Abs(body.linearVelocityX) < 0.01f && Mathf.Sign(visual.localScale.x) == -wallSide,
                    "Must stop and face target");
                await Until(() => target.ReceivedHitCount > 0, "Close shot damage");
                Check(target.TotalDamage == 14f, "Arrow damage changed");
                await Until(() => !shot.IsUsing, "Recovery");
                if (!cliff)
                {
                    // 벽이 사라지면 예외가 해제되어 다시 정상 거리 확보를 해야 합니다.
                    wall.enabled = false; Physics2D.SyncTransforms();
                    await Until(() => body.linearVelocityX * wallSide > 0.1f, "Resume retreat after wall removal");
                    Check(!shot.IsUsing, "Close exception stuck");
                    await Until(() => body.linearVelocityX == 0f && Vector2.Distance(enemy.transform.position, target.transform.position) >= 5f,
                        "Stop at normal range");
                    await Until(() => Mathf.Sign(visual.localScale.x) == -wallSide, "Face target after retreat");
                    // 타깃 쪽 벽은 예외가 있어도 관통해서 공격할 수 없습니다.
                    float currentX = enemy.transform.position.x;
                    target.transform.position = new Vector3(currentX - wallSide * 2f, 0.05f);
                    wall.transform.position = new Vector3(currentX + wallSide * 0.6f, 1.5f); wall.enabled = true;
                    Box(root, new Vector2(currentX - wallSide, 1.5f), new Vector2(0.2f, 3f), 6);
                    Physics2D.SyncTransforms();
                    await Wait(3f);
                    Check(!shot.IsUsing && target.ReceivedHitCount == 1, "Attack through wall");
                }
                Check(body.position.y > -0.05f, "Retreat fell from cliff");
                return new { passed = true, scenario = "blocked retreat attack / resume / sight", wallSide, cliff };
            }
            finally
            {
                root.SetActive(false); Object.Destroy(root);
                foreach (EnemyProjectile arrow in Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None))
                    if (Mathf.Abs(arrow.transform.position.x - x) < 25f) Object.Destroy(arrow.gameObject);
            }
        }

        private static async Task<object> Zombie(float x, int side)
        {
            GameObject root = new GameObject("Zombie close hit");
            try
            {
                Box(root, new Vector2(x, -0.5f), new Vector2(20f, 1f), 6);
                EnemyBase enemy = Spawn(root, Names[0], x, true);
                Transform visual = enemy.transform.Find("Visual");
                Vector3 scale = visual.localScale; scale.x = Mathf.Abs(scale.x) * side; visual.localScale = scale;
                EnemyDamageProbe target = Target(root, new Vector2(x + side * 0.05f, 0.05f));
                // 폭이 작은 타깃으로 몸통 안쪽의 빈 공간을 실제 Animation Event로 확인합니다.
                target.GetComponent<BoxCollider2D>().size = new Vector2(0.08f, 0.45f);
                target.GetComponent<BoxCollider2D>().offset = new Vector2(0f, 0.28f);
                enemy.SetTarget(target.transform);
                await Wait(0.2f);
                EnemySkillModule skills = enemy.GetModule<EnemySkillModule>();
                Check(skills.TryUseAttack(), "Start zombie attack");
                float start = Time.time;
                await Wait(0.4f);
                Check(target.ReceivedHitCount == 0, "Zombie hit before preparation");
                await Until(() => target.ReceivedHitCount == 1, "Zombie body overlap damage");
                float hitDelay = Time.time - start;
                Check(hitDelay >= 0.45f && hitDelay < 0.7f && target.TotalDamage == 8f, "Zombie timing / damage " + hitDelay);
                enemy.OnAttackHit(); enemy.OnAttackHit();
                Check(target.ReceivedHitCount == 1, "Duplicate hit");
                await Until(() => !skills.IsBusy, "Zombie recovery");
                // 앞쪽 최대 도달점 유지, 뒤쪽 몸체 밖은 여전히 안전합니다.
                BoxCollider2D hitbox = enemy.transform.Find("Visual/AttackHitbox").GetComponent<BoxCollider2D>();
                Check(hitbox.OverlapPoint(hitbox.transform.TransformPoint(new Vector3(1.09f, 0.3f))), "Front reach shrank");
                Check(!hitbox.OverlapPoint(hitbox.transform.TransformPoint(new Vector3(-0.4f, 0.3f))), "Hitbox extended too far behind body");
                return new { passed = true, scenario = "zombie body hit / timing / front reach", side, hitDelay };
            }
            finally { root.SetActive(false); Object.Destroy(root); }
        }

        private static EnemyBase Spawn(GameObject parent, string name, float x, bool manual)
        {
            GameObject staging = new GameObject("Inactive configuration"); staging.SetActive(false);
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Folder + name + ".prefab"),
                new Vector3(x, 0.05f), Quaternion.identity, staging.transform);
            EnemyPatrolModule patrol = enemy.GetComponentInChildren<EnemyPatrolModule>();
            if (patrol != null) patrol.enabled = false;
            if (manual)
            {
                SerializedObject data = new SerializedObject(enemy);
                data.FindProperty("detectionRange").floatValue = 0f;
                data.FindProperty("moveSpeed").floatValue = 0f;
                data.ApplyModifiedPropertiesWithoutUndo();
            }
            enemy.transform.SetParent(parent.transform, true); Object.Destroy(staging);
            return enemy;
        }

        private static BoxCollider2D Box(GameObject parent, Vector2 position, Vector2 size, int layer)
        {
            GameObject go = new GameObject("Terrain"); go.transform.SetParent(parent.transform); go.layer = layer;
            go.transform.position = position; BoxCollider2D box = go.AddComponent<BoxCollider2D>(); box.size = size;
            if (layer == 10) { go.AddComponent<PlatformEffector2D>().useOneWay = true; box.usedByEffector = true; }
            return box;
        }

        private static EnemyDamageProbe Target(GameObject parent, Vector2 position)
        {
            GameObject go = new GameObject("Damage target"); go.transform.SetParent(parent.transform); go.transform.position = position;
            BoxCollider2D box = go.AddComponent<BoxCollider2D>(); box.isTrigger = true;
            box.size = new Vector2(0.5f, 2f); box.offset = Vector2.up;
            return go.AddComponent<EnemyDamageProbe>();
        }

        private static async Task Wait(float duration) { float end = Time.time + duration; await Until(() => Time.time >= end, "Wait"); }
        private static async Task Until(Func<bool> condition, string label)
        {
            DateTime end = DateTime.UtcNow.AddSeconds(15);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < end, "Timeout " + label); await Task.Yield(); }
        }
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
