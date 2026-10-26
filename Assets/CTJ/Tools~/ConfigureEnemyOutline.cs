using System;
using System.Collections.Generic;
using System.Linq;
using CTJ.Enemies;
using UnityEditor;
using UnityEngine;

namespace CTJ.Tools
{
    public static class ConfigureEnemyOutline
    {
        private static readonly string[] Paths = {
            "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss"
        };
        private const string MaterialPath = "Assets/CTJ/Prototypes/Sprites/EnemyOutline.mat";

        public static object Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CTJ/Shaders/EnemyOutline.shader");
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Outline shader must compile");
            foreach (string suffix in Paths)
                Check(AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + suffix + ".prefab")
                    .GetComponentInChildren<EnemyOutlineModule>(true) == null, "Already configured: " + suffix);
            Material material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
            if (material == null)
            {
                material = new Material(shader) { name = "EnemyOutline" };
                AssetDatabase.CreateAsset(material, MaterialPath);
            }
            var results = new List<object>();
            foreach (string suffix in Paths)
            {
                string path = "Assets/CTJ/Prototypes/" + suffix + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    // 기존 전투/피격/애니메이션 설정이 그대로인지 저장 전에 검사합니다.
                    var unchanged = root.GetComponentsInChildren<Component>(true)
                        .Where(c => c != null && !(c is Transform)).ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
                    GameObject child = new GameObject("OutlineModule"); child.layer = root.layer;
                    child.transform.SetParent(root.transform, false);
                    EnemyOutlineModule module = child.AddComponent<EnemyOutlineModule>();
                    SpriteRenderer[] sources = root.transform.Find("Visual").GetComponentsInChildren<SpriteRenderer>(true);
                    Check(sources.Length > 0, "No visual sprites: " + suffix);
                    SerializedObject so = new SerializedObject(module);
                    so.FindProperty("outlineMaterial").objectReferenceValue = material;
                    SerializedProperty sprites = so.FindProperty("sprites"); sprites.arraySize = sources.Length;
                    for (int i = 0; i < sources.Length; i++) sprites.GetArrayElementAtIndex(i).objectReferenceValue = sources[i];
                    so.ApplyModifiedPropertiesWithoutUndo();
                    foreach (var pair in unchanged)
                        Check(EditorJsonUtility.ToJson(pair.Key) == pair.Value, "Unrelated component changed: " + pair.Key);
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool success);
                    Check(success, "Failed to save: " + path);
                    results.Add(new { prefab = suffix, sources = sources.Length, originalComponentsPreserved = true });
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return results;
        }

        public static object Validate()
        {
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>("Assets/CTJ/Shaders/EnemyOutline.shader");
            Check(shader != null && !ShaderUtil.ShaderHasError(shader), "Shader compile error");
            foreach (string suffix in Paths)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + suffix + ".prefab");
                var modules = root.GetComponentsInChildren<EnemyOutlineModule>(true);
                Check(modules.Length == 1 && modules[0].transform.parent == root.transform, "One root child module required");
                SerializedObject so = new SerializedObject(modules[0]);
                Check(so.FindProperty("outlineMaterial").objectReferenceValue == AssetDatabase.LoadAssetAtPath<Material>(MaterialPath), "Shared outline material");
                Check(so.FindProperty("sprites").arraySize > 0 && so.FindProperty("hitColor").colorValue == Color.white, "Sources/white hit");
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing component: " + suffix);
                    SerializedProperty property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Broken reference: " + suffix + "." + property.propertyPath);
                }
            }
            return new { passed = true, prefabs = Paths.Length, scope = "shader / optional module / material / sprite references" };
        }

        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
