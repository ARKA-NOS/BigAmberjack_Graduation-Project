using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupHitCameraShake
{
    private const string ModulePath = "Assets/_Prefabs/Enemies/Effects/EnemyHitCameraShakeModule.prefab";

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        if (AssetDatabase.LoadAssetAtPath<Object>(ModulePath) != null)
            throw new Exception("Already configured; do not overwrite Inspector settings");
        string[] paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null).ToArray();
        var root = new GameObject("EnemyHitCameraShakeModule");
        try
        {
            root.AddComponent<EnemyHitCameraShakeModule>();
            PrefabUtility.SaveAsPrefabAsset(root, ModulePath);
        }
        finally { Object.DestroyImmediate(root); }

        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
        foreach (string path in paths)
        {
            var enemy = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (enemy.GetComponentInChildren<EnemyHitCameraShakeModule>(true) != null)
                    throw new Exception("Module already exists: " + path);
                var module = (GameObject)PrefabUtility.InstantiatePrefab(prefab, enemy.transform);
                module.transform.localPosition = Vector3.zero;
                module.transform.localRotation = Quaternion.identity;
                module.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(enemy, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
        }
        return new { created = ModulePath, attached = paths };
    }
}
