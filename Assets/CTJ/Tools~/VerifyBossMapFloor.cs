using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using SDW.Scripts.Maps;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyBossMapFloor
    {
        private const string MapPath = "Assets/SDW/Prefabs/Map/TestBossMap.prefab";
        private const string BossPath = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        public static async Task<object> Inspect()
        {
            RequirePlay();
            var map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MapPath), new Vector3(12000f, 0f), Quaternion.identity);
            try
            {
                foreach (var tiles in map.GetComponentsInChildren<TilemapCollider2D>()) tiles.ProcessTilemapChanges();
                await Frames(2); Physics2D.SyncTransforms();
                var room = map.GetComponent<RoomController>();
                Vector2 spawn = room.EnemySpawnPoints[0].position;
                var floor = Physics2D.Raycast(spawn, Vector2.down, 30f, LayerMask.GetMask("Ground"));
                return new { spawn = spawn.ToString(), hit = floor.collider != null ? floor.collider.name : null,
                    point = floor.point.ToString(), normal = floor.normal.ToString(), distance = floor.distance,
                    colliders = map.GetComponentsInChildren<Collider2D>().Select(c => new { c.name, type = c.GetType().Name,
                        layer = c.gameObject.layer, bounds = c.bounds.ToString() }).ToArray() };
            }
            finally { map.SetActive(false); Object.Destroy(map); }
        }

        public static Task<object> RunPlay() => RunScenario(false);
        public static Task<object> RunShiftedMap() => RunScenario(true);
        private static async Task<object> RunScenario(bool lateMap)
        {
            RequirePlay();
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            var stage = new GameObject("TestBossMap floor verification");
            try
            {
                Vector3 offset = new Vector3(lateMap ? 12400f : 12000f, lateMap ? 11f : 0f);
                GameObject map = null;
                if (!lateMap) { map = SpawnMap(stage, offset); await Frames(2); }
                Vector3 spawnLocal = AssetDatabase.LoadAssetAtPath<GameObject>(MapPath).GetComponent<RoomController>().EnemySpawnPoints[0].position;
                var holder = new GameObject("Boss holder"); holder.transform.SetParent(stage.transform); holder.SetActive(false);
                // 사용자 테스트 씬처럼 기본 스폰보다 1유닛 높여 고정 Floor Distance 오차를 재현합니다.
                var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath), offset + spawnLocal + Vector3.up,
                    Quaternion.identity, holder.transform);
                SetFloat(boss, "firstVolleyDelay", 999f);
                boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                holder.SetActive(true);
                var arena = boss.GetModule<BossArenaModule>();
                var laser = boss.GetComponentInChildren<BossLaserSkill>();
                var shape = boss.GetComponentInChildren<BossSpriteBeam>();
                float initialFloor = arena.FloorY;
                if (lateMap)
                {
                    Check(Mathf.Abs(initialFloor - (arena.Center.y - 5f)) < 0.001f, "No ground uses manual fallback");
                    map = SpawnMap(stage, offset); await Frames(2);
                }
                Physics2D.SyncTransforms();
                var actual = Physics2D.Raycast((Vector2)(offset + spawnLocal), Vector2.down, 30f, LayerMask.GetMask("Ground"));
                Check(actual.collider != null && Mathf.Abs(actual.point.y - (offset.y - 2f)) < 0.01f, "Measured TestBossMap floor");
                float expectedFloor = actual.point.y;
                var target = new GameObject("Floor target"); target.transform.SetParent(stage.transform);
                target.transform.position = new Vector3(arena.Center.x, expectedFloor + 0.5f);
                target.layer = 7; target.AddComponent<BoxCollider2D>().isTrigger = true; target.AddComponent<EnemyDamageProbe>();
                boss.SetTarget(target.transform);
                await Until(() => boss.CurrentState == RobotEyeBossState.Waiting, 10);

                // 위쪽 트리거와 다른 레이어의 장식은 바닥 높이에 영향을 주지 않아야 합니다.
                var trigger = new GameObject("Ground trigger"); trigger.transform.SetParent(stage.transform); trigger.layer = 6;
                trigger.transform.position = arena.Center + Vector2.down;
                trigger.AddComponent<BoxCollider2D>().isTrigger = true;
                var decoration = new GameObject("Non-ground decoration"); decoration.transform.SetParent(stage.transform);
                decoration.transform.position = arena.Center + Vector2.down * 2f; decoration.AddComponent<BoxCollider2D>();
                boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, boss.transform.position, Vector2.left, Vector2.right);
                Check(Mathf.Abs(arena.FloorY - expectedFloor) < 0.01f, "Phase entry refreshes floor after map setup");
                Check(Mathf.Abs(arena.LaserBounds.min.y - expectedFloor) < 0.01f, "Warning bounds use real floor");
                Check(Mathf.Abs(arena.LaserLeft.y - expectedFloor - shape.HeightFromFloor) < 0.01f, "Boss firing height follows floor without stretching image");
                trigger.SetActive(false); decoration.SetActive(false);
                bool[] warningSeen = new bool[3], beamSeen = new bool[3];
                DateTime deadline = DateTime.UtcNow.AddSeconds(40);
                while (!boss.HasFinishedLaserPhase)
                {
                    Check(DateTime.UtcNow < deadline, "Laser phase timeout: " + boss.CurrentState);
                    int pass = boss.CompletedTraversals;
                    if (laser.IsPreparing)
                    {
                        warningSeen[pass] = true;
                        Check(laser.DangerPolygon.Count >= 3 && laser.DangerPolygon.All(p => p.y >= expectedFloor - 0.01f), "Warning stays above real floor");
                        float bottom = laser.DangerPolygon.Min(p => p.y);
                        Check(Mathf.Abs(bottom - expectedFloor) < 0.01f, "Warning bottom reaches actual floor");
                        Canvas.ForceUpdateCanvases();
                        var graphic = boss.GetComponentInChildren<ConvexPolygonGraphic>();
                        Mesh warningMesh = graphic.canvasRenderer.GetMesh();
                        Check(warningMesh != null && warningMesh.vertexCount >= 3, "Actual Canvas warning mesh exists");
                        float renderedBottom = warningMesh.vertices.Min(p => graphic.transform.TransformPoint(p).y);
                        Check(Mathf.Abs(renderedBottom - expectedFloor) < 0.01f, "Rendered warning bottom reaches actual floor");
                    }
                    if (laser.IsFiring)
                    {
                        beamSeen[pass] = true;
                        Check(Mathf.Abs(shape.FloorContactY - expectedFloor) < 0.01f, "Rendered beam contacts actual map floor");
                        Check(Mathf.Abs(arena.FloorY - expectedFloor) < 0.01f, "Floor does not follow moving boss");
                        if (shape.CanDamage)
                            Check(Mathf.Abs(shape.Bounds.min.y - expectedFloor - 0.453375f) < 0.01f, "Damage core keeps floor tip grace");
                    }
                    await Task.Yield();
                }
                Check(warningSeen.All(v => v) && beamSeen.All(v => v), "Three mirrored warnings and beams verified");
                var settings = new SerializedObject(arena);
                settings.FindProperty("detectFloor").boolValue = false; settings.ApplyModifiedPropertiesWithoutUndo();
                laser.MatchArenaHeight(arena);
                Check(Mathf.Abs(arena.FloorY - (arena.Center.y - 5f)) < 0.001f, "Detection off preserves manual mode");
                settings.Update(); settings.FindProperty("detectFloor").boolValue = true;
                settings.FindProperty("floorProbeDistance").floatValue = 0.5f; settings.ApplyModifiedPropertiesWithoutUndo();
                laser.MatchArenaHeight(arena);
                Check(Mathf.Abs(arena.FloorY - (arena.Center.y - 5f)) < 0.001f, "Too-short probe preserves fallback");
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, lateMap, initialFloor, actualFloor = expectedFloor,
                    beamContactY = expectedFloor, warningBottomY = expectedFloor, warningSeen, beamSeen, errors };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); Application.logMessageReceived -= capture; }
        }

        private static GameObject SpawnMap(GameObject stage, Vector3 offset)
        {
            var map = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MapPath), offset, Quaternion.identity, stage.transform);
            foreach (var tiles in map.GetComponentsInChildren<TilemapCollider2D>()) tiles.ProcessTilemapChanges();
            return map;
        }
        private static void SetFloat(Object obj, string field, float value)
        {
            var settings = new SerializedObject(obj); settings.FindProperty(field).floatValue = value; settings.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Until(Func<bool> condition, int seconds)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition()) { Check(DateTime.UtcNow < deadline, "Wait timed out"); await Task.Yield(); }
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        private static async Task Frames(int count)
        {
            int end = Time.frameCount + count;
            while (Time.frameCount < end) await Task.Yield();
        }
        private static void RequirePlay()
        {
            if (!EditorApplication.isPlaying || !SessionState.GetBool("CTJ.Enemy01.Verifying", false) || SceneManager.GetActiveScene().path != "")
                throw new InvalidOperationException("Isolated Play required");
        }
    }
}
