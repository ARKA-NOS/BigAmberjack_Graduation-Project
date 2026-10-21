using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using DevLib.BattleSystem;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.HealthSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyDeathAndFlight
    {
        private static readonly string[] Prefabs = {
            "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab",
            "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab",
            "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab"
        };
        private const string ManagerPath = "Assets/CTJ/Prototypes/Prefabs/EnemyKillCountManager.prefab";
        private static int _checks;
        private static readonly MethodInfo Report = typeof(EnemyDeathModule).GetMethod("ReportDeath", BindingFlags.Instance | BindingFlags.NonPublic);

        public static object ReloadMapScene()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            Scene scene = SceneManager.GetActiveScene();
            Check(SceneManager.sceneCount == 1 && scene.path == "Assets/CTJ/Scenes/CTJ_MapTestScene.unity" && !scene.isDirty,
                "Do not reload a different or unsaved user scene");
            AssetDatabase.ImportAsset(scene.path, ImportAssetOptions.ForceUpdate);
            scene = EditorSceneManager.OpenScene(scene.path, OpenSceneMode.Single);
            EnemyKillCountManager[] managers = Object.FindObjectsByType<EnemyKillCountManager>(FindObjectsSortMode.None);
            Check(managers.Length == 1 && managers[0].transform.parent == null, "One standalone manager in map test scene");
            Check(!scene.isDirty, "Reload must not leave scene edits");
            return new { scene = scene.path, manager = managers[0].name, roots = scene.rootCount, isDirty = scene.isDirty };
        }

        public static object Validate()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            foreach (string path in Prefabs.Concat(new[] { ManagerPath }))
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(prefab != null, "Missing prefab " + path);
                if (path != ManagerPath)
                {
                    EnemyDeathModule[] deaths = prefab.GetComponentsInChildren<EnemyDeathModule>(true);
                    Check(deaths.Length == 1 && deaths[0].transform.parent == prefab.transform && deaths[0].enabled,
                        "One active child DeathModule per enemy");
                }
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing script");
                    SerializedProperty p = new SerializedObject(component).GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0, "Missing reference: " + p.propertyPath);
                }
            }
            Check(AssetDatabase.LoadAssetAtPath<EnemyKillCountManager>(ManagerPath) is AbstractManager, "GameManager-compatible manager");
            return new { valid = true, enemies = Prefabs.Length, manager = ManagerPath };
        }

        public static async Task<object> Run()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == "Assets/CTJ/Prototypes/Bosses/BossPatternTest.unity", "Use isolated boss test scene");
            _checks = 0;
            EnemyKillCountManager manager = ServiceLocator.Get<EnemyKillCountManager>();
            Check(manager != null && manager.transform.parent == null, "Standalone scene manager must self-initialize");
            manager.ResetKillCount();
            List<GameObject> spawned = new List<GameObject>();
            try
            {
                Check(manager.GetKillCount() == 0, "Reset/Get");
                foreach (string path in Prefabs)
                {
                    EnemyBase enemy = Spawn(path, spawned);
                    int before = manager.GetKillCount();
                    EnemyDeathModule death = enemy.GetModule<EnemyDeathModule>();
                    Check(death != null, "ModuleOwner must register DeathModule");
                    Report.Invoke(death, null);
                    Damage(enemy, 1f);
                    Check(!enemy.IsDead && manager.GetKillCount() == before, "Living/nonlethal must not count");
                    Damage(enemy, 99999f);
                    Check(enemy.IsDead && manager.GetKillCount() == before + 1, "Cat/Ghost/Boss death must count once");
                    Damage(enemy, 99999f);
                    enemy.GetModule<IHealthModule>().CurrentHealth = 0f;
                    Report.Invoke(death, null);
                    Check(manager.GetKillCount() == before + 1, "Duplicate damage, health callback and report");
                }
                Check(manager.GetKillCount() == 3, "Total across all species");
                manager.Initialize(); manager.Initialize();
                Check(manager.GetKillCount() == 3, "Repeated Initialize must not reset count");
                manager.enabled = false; manager.enabled = true;
                Check(manager.GetKillCount() == 3 && ServiceLocator.Get<EnemyKillCountManager>() == manager, "Re-enable preserves count and registration");

                EnemyBase dead = Spawn(Prefabs[0], spawned);
                dead.GetModule<IHealthModule>().CurrentHealth = 0f;
                Check(manager.GetKillCount() == 4, "Direct lethal health change");
                manager.ResetKillCount();
                Report.Invoke(dead.GetModule<EnemyDeathModule>(), null);
                Check(manager.GetKillCount() == 0, "Reset must not re-arm already dead enemies");

                EnemyBase removed = Spawn(Prefabs[0], spawned);
                removed.gameObject.SetActive(false);
                Object.Destroy(removed.gameObject);
                await Task.Yield(); await Task.Yield();
                Check(manager.GetKillCount() == 0, "Despawn/disable is not death");
                Scene temporary = SceneManager.CreateScene("CTJ Death Unload Verification");
                EnemyBase unloaded = Spawn(Prefabs[1], spawned);
                SceneManager.MoveGameObjectToScene(unloaded.gameObject, temporary);
                AsyncOperation unloading = SceneManager.UnloadSceneAsync(temporary);
                while (!unloading.isDone) await Task.Yield();
                Check(manager.GetKillCount() == 0, "Scene unload must not count living enemies");

                EnemyBase excluded = Spawn(Prefabs[0], spawned);
                excluded.GetModule<EnemyDeathModule>().enabled = false;
                Damage(excluded, 99999f);
                Check(excluded.IsDead && manager.GetKillCount() == 0, "Disabled death module opts out");
                EnemyBase revivedObject = Spawn(Prefabs[0], spawned);
                revivedObject.gameObject.SetActive(false); revivedObject.gameObject.SetActive(true);
                Damage(revivedObject, 99999f);
                Check(manager.GetKillCount() == 1, "A living enemy survives disable/re-enable and counts normally");

                GameObject duplicate = new GameObject("Duplicate kill manager verification"); spawned.Add(duplicate);
                EnemyKillCountManager duplicateManager = duplicate.AddComponent<EnemyKillCountManager>();
                Check(!duplicateManager.enabled && ServiceLocator.Get<EnemyKillCountManager>() == manager, "Duplicate cannot steal registration");
                Object.Destroy(duplicate);
                await Task.Yield(); await Task.Yield();
                Check(ServiceLocator.Get<EnemyKillCountManager>() == manager, "Duplicate destruction cannot unregister owner");

                manager.enabled = false;
                EnemyBase missing = Spawn(Prefabs[0], spawned);
                Damage(missing, 99999f);
                Check(missing.IsDead && manager.GetKillCount() == 1, "Missing manager warns, does not block death");
                EnemyBase delayed = Spawn(Prefabs[1], spawned);
                manager.enabled = true;
                Report.Invoke(missing.GetModule<EnemyDeathModule>(), null);
                Check(manager.GetKillCount() == 1, "No retroactive count for unconfigured death");
                Damage(delayed, 99999f);
                Check(manager.GetKillCount() == 2, "Manager resolved at death, not spawn");
                manager.GameEnd();
                Check(ServiceLocator.Get<EnemyKillCountManager>() == null, "GameEnd clears registration");
                manager.Initialize();
                Check(manager.GetKillCount() == 2 && ServiceLocator.Get<EnemyKillCountManager>() == manager, "Initialize is not Reset");

                manager.enabled = false;
                GameObject gameRoot = new GameObject("CTJ GameManager compatibility verification");
                spawned.Add(gameRoot); gameRoot.SetActive(false);
                GameObject child = new GameObject("EnemyKillCountManager"); child.transform.SetParent(gameRoot.transform);
                EnemyKillCountManager integrated = child.AddComponent<EnemyKillCountManager>();
                gameRoot.AddComponent<GameManager>(); // 이 임시 GameManager에는 저장하는 매니저가 없습니다.
                gameRoot.SetActive(true);
                Check(ServiceLocator.Get<EnemyKillCountManager>() == integrated, "Existing GameManager child initialization");
                EnemyBase integratedEnemy = Spawn(Prefabs[0], spawned); Damage(integratedEnemy, 99999f);
                Check(integrated.GetKillCount() == 1, "Integrated manager receives deaths");
                Object.Destroy(gameRoot);
                await Task.Yield(); await Task.Yield();
                Check(ServiceLocator.Get<EnemyKillCountManager>() == null, "GameManager destruction cleans service");
                manager.enabled = true;

                VerifyFlight(spawned);
                return new { passed = true, checks = _checks, expectedWarnings = 2,
                    coverage = "3 species / once-only / reset-get / disable-unload / late manager / duplicate / GameManager compatibility / smooth motion" };
            }
            finally
            {
                foreach (GameObject go in spawned) if (go != null) Object.Destroy(go);
                manager.enabled = true;
                manager.ResetKillCount();
            }
        }

        private static void VerifyFlight(List<GameObject> spawned)
        {
            EnemyBase boss = Spawn(Prefabs[2], spawned);
            boss.enabled = false; // 보스 FSM 대신 동일 프레임 안에서 물리 틱을 직접 검사합니다.
            BossFlightModule flight = boss.GetModule<BossFlightModule>();
            Rigidbody2D body = boss.GetComponent<Rigidbody2D>();
            float dt = Time.fixedDeltaTime;
            foreach (float distance in new[] { 8f, 0.1f })
            {
                flight.Place(Vector2.zero);
                flight.MoveTo(Vector2.right * distance, 8f);
                List<float> steps = new List<float>();
                for (int i = 0; flight.IsMoving && i < 500; i++)
                {
                    float before = body.position.x;
                    flight.Tick();
                    steps.Add(body.position.x - before);
                }
                Check(!flight.IsMoving && flight.HasArrived && Vector2.Distance(body.position, Vector2.right * distance) < 0.0001f, "Exact finite arrival");
                Check(steps.All(s => s >= 0f && s <= 8f * dt + 0.0001f), "No reverse/overshoot or peak speed violation");
                Check(steps[0] < steps.Max() && steps.Last() < steps.Max(), "Slow start and slow end");
                if (distance == 8f)
                {
                    Check(steps[1] > steps[0] && Mathf.Abs(steps.Max() / dt - 8f) < 0.001f, "Accelerates to configured top speed");
                    Check(Mathf.Abs(steps.Count * dt - 1.2f) <= dt + 0.001f, "Long trip duration includes 0.2s easing overhead");
                }
                else Check(steps.Max() / dt < 8f, "Short trip lowers peak speed");
            }
            flight.Place(Vector2.zero);
            flight.MoveTo(Vector2.zero, 8f);
            Check(flight.HasArrived && !flight.IsMoving, "Zero distance finishes immediately");
            flight.MoveTo(Vector2.right * 8f, 8f);
            for (int i = 0; i < 15; i++) flight.Tick();
            Vector2 stopped = body.position;
            flight.Stop(); flight.Tick();
            Check(body.position == stopped && !flight.IsMoving, "Cancellation stops immediately");
            flight.MoveTo(Vector2.zero, 8f); flight.Tick();
            Check(Vector2.Distance(body.position, stopped) < 8f * dt, "New move restarts easing, no old speed carried over");
            for (int i = 0; flight.IsMoving && i < 500; i++) flight.Tick();
            Check(flight.HasArrived && body.position == Vector2.zero, "Retarget completes at new destination");
        }

        private static EnemyBase Spawn(string path, List<GameObject> spawned)
        {
            GameObject holder = new GameObject("Inactive death fixture"); holder.SetActive(false);
            EnemyBase enemy;
            try
            {
                enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(path),
                    new Vector3(4000f + spawned.Count * 5f, 4f, 0f), Quaternion.identity, holder.transform);
                enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                enemy.transform.SetParent(null);
                spawned.Add(enemy.gameObject);
                enemy.GetComponent<Rigidbody2D>().gravityScale = 0f;
            }
            finally { Object.Destroy(holder); }
            return enemy;
        }

        private static void Damage(EnemyBase enemy, float amount) => enemy.ApplyDamage(
            new DamageData { DamageAmount = amount }, Vector2.zero, Vector2.right, Vector2.zero);
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
    }
}
