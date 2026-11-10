using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using UnityEditor;
using UnityEngine;

public static class SetupDeathBloodBurst
{
    private const string SourcePath = "Assets/_Prefabs/Enemies/Effects/HitBloodSpray.prefab";
    private const string DeathPath = "Assets/_Prefabs/Enemies/Effects/DeathBloodBurst.prefab";

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(DeathPath) != null)
            throw new Exception("Death burst already exists; preserve Inspector changes");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(SourcePath) == null)
            throw new Exception("Source blood prefab missing");

        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath)
            .Where(path => AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null)
            .ToArray();
        if (paths.Any(path => AssetDatabase.LoadAssetAtPath<GameObject>(path)
                .GetComponentInChildren<EnemyParticleModule>(true) == null))
            throw new Exception("An enemy has no particle module");

        if (!AssetDatabase.CopyAsset(SourcePath, DeathPath)) throw new Exception("Could not copy blood prefab");
        var root = PrefabUtility.LoadPrefabContents(DeathPath);
        try
        {
            root.name = "DeathBloodBurst";
            var blood = root.GetComponent<EnemyBloodParticle>();
            if (blood == null || root.GetComponent<EnemyHitParticle>() == null)
                throw new Exception("Blood behavior missing from copied prefab");
            var data = new SerializedObject(blood);
            data.FindProperty("particleCount").vector2IntValue = new Vector2Int(52, 64);
            data.FindProperty("speedRange").vector2Value = new Vector2(6f, 9f);
            data.FindProperty("spreadAngle").floatValue = 360f;
            data.FindProperty("upwardSpeed").floatValue = 0.5f;
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, DeathPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var burst = AssetDatabase.LoadAssetAtPath<GameObject>(DeathPath).GetComponent<ParticleSystem>();
        foreach (string path in paths)
        {
            var enemy = PrefabUtility.LoadPrefabContents(path);
            try
            {
                var module = enemy.GetComponentInChildren<EnemyParticleModule>(true);
                var data = new SerializedObject(module);
                var property = data.FindProperty("deathBurstPrefab");
                if (property.objectReferenceValue != null)
                    throw new Exception("Death burst already configured: " + path);
                property.objectReferenceValue = burst;
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(enemy, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
        }
        return new { created = DeathPath, attached = paths };
    }
}
