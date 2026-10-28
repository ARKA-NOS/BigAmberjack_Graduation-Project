using System;
using System.Collections.Generic;
using System.Linq;
using CTJ.Enemies;
using Lrw.Script.Agent;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class MigrateEnemyNewHealth
    {
        private const string Source = "Assets/Lrw/GameModule/Module/NewHealthModule.prefab";
        private const string OldScriptGuid = "c1c2a7de7bdb4bad899b57a041df3e4c";
        private static readonly string[] Paths = {
            "Assets/CTJ/Prototypes/Prefabs/Catto_Melee.prefab",
            "Assets/CTJ/Prototypes/Prefabs/MadGhost_Ranged.prefab",
            "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab",
            "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab",
            "Assets/CTJ/Prototypes/Prefabs/Enemy04_MechanicArcher.prefab",
            "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab",
            "Assets/CTJ/Prefabs/CTJ_Player.prefab"
        };

        public static object Run()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode && !EditorApplication.isCompiling, "Edit mode after compilation required");
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Source);
            Check(source != null && source.GetComponent<NewHealthModule>() != null, "Missing shared prefab");
            Object core = new SerializedObject(source.GetComponent<NewHealthModule>()).FindProperty("coreHealth").objectReferenceValue;
            Object hpStat = new SerializedObject(core).FindProperty("maxHpStatData").objectReferenceValue;
            // 모든 프리팹을 먼저 검사하고, 조건을 만족할 때만 CTJ 파일을 저장합니다.
            foreach (string path in Paths)
            {
                Check(path.StartsWith("Assets/CTJ/", StringComparison.Ordinal), "CTJ only");
                GameObject asset = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(asset != null && asset.GetComponent<Agent>() != null, "Missing agent: " + path);
                MonoBehaviour[] old = OldModules(asset);
                NewHealthModule[] current = asset.GetComponentsInChildren<NewHealthModule>(true);
                if (old.Length == 0) { Check(current.Length == 1, "No health / duplicate: " + path); continue; }
                Check(asset.GetComponentsInChildren<Transform>(true).All(t =>
                    GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(t.gameObject) == 0),
                    "Missing scripts prevent prefab saving; inspect before migration: " + path);
                Check(old.Length == 1 && current.Length == 0, "Ambiguous health modules: " + path);
                Check(old[0].transform != asset.transform && old[0].transform.childCount == 0 &&
                    old[0].GetComponents<Component>().Length == 2, "Health child has additional responsibilities: " + path);
                Check(new SerializedObject(old[0]).FindProperty("maxHpStatData").objectReferenceValue == hpStat,
                    "Core health stat differs: " + path);
                foreach (Component component in asset.GetComponentsInChildren<Component>(true))
                {
                    if (component == null || component == old[0]) continue;
                    SerializedProperty p = new SerializedObject(component).GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            Check(p.objectReferenceValue != old[0], "Direct old component reference: " + path + " / " + p.propertyPath);
                }
            }
            var migrated = new List<string>();
            foreach (string path in Paths)
            {
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    MonoBehaviour old = OldModules(root).SingleOrDefault();
                    if (old == null) continue;
                    Transform previous = old.transform;
                    GameObject child = (GameObject)PrefabUtility.InstantiatePrefab(source, previous.parent);
                    child.name = "NewHealthModule";
                    child.layer = old.gameObject.layer;
                    child.transform.SetSiblingIndex(previous.GetSiblingIndex());
                    child.transform.localPosition = previous.localPosition;
                    child.transform.localRotation = previous.localRotation;
                    child.transform.localScale = previous.localScale;
                    child.SetActive(true);
                    NewHealthModule health = child.GetComponent<NewHealthModule>();
                    SerializedObject settings = new SerializedObject(health);
                    settings.FindProperty("otherHealths").arraySize = 0; // 기존 단일 HP 유지, 추가 체력 스탯 의존 없음
                    settings.FindProperty("healthViewModel").objectReferenceValue = null;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.RecordPrefabInstancePropertyModifications(health);
                    Object.DestroyImmediate(old.gameObject);
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved);
                    Check(saved, "Could not save " + path);
                    migrated.Add(path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return new { migrated, validation = Validate() };
        }

        public static object Validate()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            var result = new List<object>();
            foreach (string path in Paths)
            {
                AssetDatabase.ImportAsset(path, ImportAssetOptions.ForceUpdate);
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                Check(OldModules(root).Length == 0, "Old module remains: " + path);
                NewHealthModule[] healths = root.GetComponentsInChildren<NewHealthModule>(true);
                Check(healths.Length == 1, "Expected one new module: " + path);
                NewHealthModule health = healths[0];
                Check(health.enabled && health.gameObject.activeSelf, "Inactive new module: " + path);
                bool isEnemy = root.GetComponent<EnemyBase>() != null;
                // 구형 CTJ_Player는 기존 Missing Script 때문에 YAML의 체력 컴포넌트만 직접 교체했습니다.
                if (isEnemy) Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(health.gameObject) == Source, "Missing nested prefab link");
                SerializedObject settings = new SerializedObject(health);
                Object core = settings.FindProperty("coreHealth").objectReferenceValue;
                Check(core != null && settings.FindProperty("otherHealths").arraySize == 0 &&
                    settings.FindProperty("healthViewModel").objectReferenceValue == null, "Enemy health configuration");
                StatData hpStat = (StatData)new SerializedObject(core).FindProperty("maxHpStatData").objectReferenceValue;
                StatGroup group = (StatGroup)new SerializedObject(root.GetComponentInChildren<StatModule>(true)).FindProperty("baseStats").objectReferenceValue;
                float hp = group.Stats.First(s => s.StatData == hpStat).BaseValue;
                int existingMissingScripts = 0;
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    if (component == null)
                    {
                        Check(!isEnemy, "Missing script: " + path);
                        existingMissingScripts++;
                        continue;
                    }
                    SerializedProperty p = new SerializedObject(component).GetIterator();
                    while (p.Next(true))
                        if (p.propertyType == SerializedPropertyType.ObjectReference)
                            Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0,
                                "Broken reference: " + path + " / " + p.propertyPath);
                }
                result.Add(new { path, maxHp = hp, nestedPrefab = isEnemy ? Source : null, existingMissingScripts });
            }
            return result;
        }
        private static MonoBehaviour[] OldModules(GameObject root) => root.GetComponentsInChildren<MonoBehaviour>(true)
            .Where(c => c != null && AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(MonoScript.FromMonoBehaviour(c))) == OldScriptGuid).ToArray();

        public static object ValidateScenes()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            Scene active = SceneManager.GetActiveScene();
            Check(!active.isDirty, "Keep original scene unchanged");
            var result = new List<object>();
            foreach (string path in new[] { "Assets/CTJ/Scenes/CTJ_MapTestScene.unity",
                "Assets/CTJ/Prototypes/Scenes/EnemyArtTest.unity", "Assets/CTJ/Prototypes/Bosses/BossPatternTest.unity" })
            {
                Scene scene = SceneManager.GetSceneByPath(path);
                bool opened = !scene.IsValid() || !scene.isLoaded;
                if (opened) scene = EditorSceneManager.OpenScene(path, OpenSceneMode.Additive);
                try
                {
                    EnemyBase[] enemies = scene.GetRootGameObjects().SelectMany(root => root.GetComponentsInChildren<EnemyBase>(true)).ToArray();
                    foreach (EnemyBase enemy in enemies)
                    {
                        Check(OldModules(enemy.gameObject).Length == 0, "Scene enemy retained old health: " + path + "/" + enemy.name);
                        Check(enemy.GetComponentsInChildren<NewHealthModule>(true).Length == 1, "Scene new module count");
                    }
                    result.Add(new { scene = path, enemies = enemies.Select(e => e.name).ToArray(), dirty = scene.isDirty });
                }
                finally { if (opened) EditorSceneManager.CloseScene(scene, true); }
            }
            SceneManager.SetActiveScene(active);
            Check(!active.isDirty, "Original scene became dirty");
            return result;
        }
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
