using System;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Managers;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupEnemyCombatCoordination
{
    private const string ManagerPath = "Assets/_Prefabs/Manager/GameManager.prefab";
    private const string Modules = "Assets/_Prefabs/Enemies/Modules/";

    public static object UpdateTimeFreezeScalePrefab()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        string path = Modules + "TimeFreezeModule.prefab";
        GameObject root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var data = new SerializedObject(root.GetComponent<TimeFreezeModule>());
            data.FindProperty("stopTimeScale").floatValue = 0.05f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { path, duration = data.FindProperty("freezeDuration").floatValue, stopTimeScale = 0.05f };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        System.IO.Directory.CreateDirectory(Modules);
        AssetDatabase.Refresh();

        GameObject freeze = CreateModule<TimeFreezeModule>("TimeFreezeModule");
        GameObject token = CreateModule<EnemyAttackTokenModule>("EnemyAttackTokenModule");
        GameObject separation = CreateModule<EnemySeparationModule>("EnemySeparationModule");
        string[] enemies =
        {
            "Assets/_Prefabs/Enemies/Enemy/Enemy17/Enemy17_IntakeBiter.prefab",
            "Assets/_Prefabs/Enemies/Enemy/Enemy18/Enemy18_EffluentLeaper.prefab",
            "Assets/_Prefabs/Enemies/Enemy/Enemy19/Enemy19_OverheatedWorker.prefab",
            "Assets/_Prefabs/Enemies/Enemy/Enemy20/Enemy20_CeilingPatrol.prefab",
            "Assets/_Prefabs/Enemies/Enemy/Enemy21/Enemy21_CutterAutomaton.prefab",
            "Assets/_Prefabs/Enemies/Enemy/Enemy22/Enemy22_LoadingCrusher.prefab",
            "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab"
        };

        foreach (string path in enemies)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                AddModule<TimeFreezeModule>(root, freeze);
                if (root.GetComponent<_Scripts.Agent.Enemies.Bosses.BossBase>() == null)
                {
                    AddModule<EnemyAttackTokenModule>(root, token);
                    AddModule<EnemySeparationModule>(root, separation);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        GameObject manager = PrefabUtility.LoadPrefabContents(ManagerPath);
        try
        {
            AddManager<TimeStopService>(manager, "TimeStopService");
            AddManager<AttackTokenManager>(manager, "AttackTokenManager");
            PrefabUtility.SaveAsPrefabAsset(manager, ManagerPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(manager); }

        return new { enemyPrefabs = enemies.Length, timeFreeze = 7, attackTokens = 6, separation = 6,
            manager = ManagerPath, maxToken = 2, waitTimeout = 2f, retryDelay = 0.5f, scenesSaved = false };
    }

    private static GameObject CreateModule<T>(string name) where T : Component
    {
        string path = Modules + name + ".prefab";
        var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
        if (existing != null) return existing;

        var temporary = new GameObject(name);
        try
        {
            temporary.AddComponent<T>();
            return PrefabUtility.SaveAsPrefabAsset(temporary, path);
        }
        finally { Object.DestroyImmediate(temporary); }
    }

    private static void AddModule<T>(GameObject root, GameObject prefab) where T : Component
    {
        if (root.GetComponentInChildren<T>(true) == null) PrefabUtility.InstantiatePrefab(prefab, root.transform);
    }

    private static void AddManager<T>(GameObject root, string name) where T : Component
    {
        if (root.GetComponentInChildren<T>(true) != null) return;

        var child = new GameObject(name);
        child.transform.SetParent(root.transform, false);
        child.AddComponent<T>();
    }
}
