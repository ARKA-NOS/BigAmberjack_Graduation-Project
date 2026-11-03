using System;
using CTJ.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class ConfigureEnemyBloodSpray
    {
        private const string Folder = "Assets/CTJ/Prototypes/Particles/";
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged",
            "Prefabs/Enemy01_TwistedArm", "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };

        public static object Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode) throw new InvalidOperationException("Edit mode only");
            Mesh[] shapes = CreateShapes();
            AssetDatabase.ImportAsset(Folder + "PolygonBlood.shader", ImportAssetOptions.ForceUpdate);
            Shader shader = AssetDatabase.LoadAssetAtPath<Shader>(Folder + "PolygonBlood.shader");
            string materialPath = Folder + "BloodPolygon.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material == null)
            {
                material = new Material(shader);
                AssetDatabase.CreateAsset(material, materialPath);
            }
            material.shader = shader;
            material.SetColor("_Color", Color.white);
            EditorUtility.SetDirty(material); AssetDatabase.SaveAssetIfDirty(material);
            GameObject root = new GameObject("HitBloodSpray");
            ParticleSystem prefab;
            try
            {
                ParticleSystem particle = root.AddComponent<ParticleSystem>();
                particle.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = particle.main;
                main.loop = false; main.playOnAwake = false; main.duration = 1f; main.startLifetime = 1f;
                main.startSpeed = 0f; main.startSize3D = true; main.startSize = 1f;
                main.startRotation3D = true; main.gravityModifier = 0f; main.simulationSpeed = 0f;
                main.simulationSpace = ParticleSystemSimulationSpace.World;
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.maxParticles = 64; main.cullingMode = ParticleSystemCullingMode.AlwaysSimulate;
                var emission = particle.emission; emission.enabled = false;
                var shape = particle.shape; shape.enabled = false;
                var renderer = particle.GetComponent<ParticleSystemRenderer>();
                renderer.sharedMaterial = material;
                renderer.enableGPUInstancing = false;
                renderer.renderMode = ParticleSystemRenderMode.Mesh;
                renderer.SetMeshes(shapes);
                renderer.meshDistribution = ParticleSystemMeshDistribution.UniformRandom;
                renderer.alignment = ParticleSystemRenderSpace.World;
                renderer.sortingOrder = 31;
                root.AddComponent<EnemyBloodParticle>();
                root.AddComponent<EnemyHitParticle>();
                prefab = PrefabUtility.SaveAsPrefabAsset(root, Folder + "HitBloodSpray.prefab").GetComponent<ParticleSystem>();
            }
            finally { Object.DestroyImmediate(root); }

            foreach (string enemyPath in Enemies)
            {
                string path = "Assets/CTJ/Prototypes/" + enemyPath + ".prefab";
                GameObject enemy = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnemyParticleModule module = enemy.GetComponentInChildren<EnemyParticleModule>(true);
                    if (module == null) throw new InvalidOperationException("Missing ParticleModule " + path);
                    SerializedObject data = new SerializedObject(module);
                    SerializedProperty entries = data.FindProperty("hitEffects");
                    entries.arraySize = 1;
                    SerializedProperty entry = entries.GetArrayElementAtIndex(0);
                    entry.FindPropertyRelative("label").stringValue = "Polygon Blood Spray";
                    entry.FindPropertyRelative("enabled").boolValue = true;
                    entry.FindPropertyRelative("prefab").objectReferenceValue = prefab;
                    entry.FindPropertyRelative("randomPrefabs").arraySize = 0;
                    entry.FindPropertyRelative("scale").floatValue = 1f;
                    entry.FindPropertyRelative("offset").vector2Value = Vector2.zero;
                    entry.FindPropertyRelative("alignToHitDirection").boolValue = true;
                    entry.FindPropertyRelative("angleOffset").floatValue = 0f;
                    data.FindProperty("maxConcurrentEffects").intValue = 24;
                    data.FindProperty("maxEffectLifetime").floatValue = 6f;
                    data.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(enemy, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(enemy); }
            }
            return new { configured = Enemies.Length, prefab = AssetDatabase.GetAssetPath(prefab), polygonMeshes = shapes.Length };
        }

        private static Mesh[] CreateShapes()
        {
            var random = new System.Random(713);
            Mesh[] shapes = new Mesh[4];
            for (int index = 0; index < shapes.Length; index++)
            {
                string path = Folder + "BloodPolygon" + (index + 1) + ".asset";
                Mesh mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
                if (mesh == null) { mesh = new Mesh(); AssetDatabase.CreateAsset(mesh, path); }
                mesh.Clear(); mesh.name = "BloodPolygon" + (index + 1);
                int corners = 5 + index % 3;
                Vector3[] vertices = new Vector3[corners + 1];
                Vector2[] uv = new Vector2[vertices.Length];
                Color32[] colors = new Color32[vertices.Length];
                int[] triangles = new int[corners * 3];
                for (int i = 0; i < corners; i++)
                {
                    float angle = (i + (float)random.NextDouble() * 0.35f) / corners * Mathf.PI * 2f;
                    float radius = Mathf.Lerp(0.33f, 0.58f, (float)random.NextDouble());
                    vertices[i + 1] = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle)) * radius;
                    triangles[i * 3] = 0; triangles[i * 3 + 1] = i + 1; triangles[i * 3 + 2] = (i + 1) % corners + 1;
                }
                for (int i = 0; i < vertices.Length; i++) { uv[i] = (Vector2)vertices[i] + Vector2.one * 0.5f; colors[i] = new Color32(255, 255, 255, 255); }
                // ParticleSystem의 메시 색 스트림과 같은 RGBA 8비트 형식으로 저장합니다.
                mesh.vertices = vertices; mesh.uv = uv; mesh.colors32 = colors; mesh.triangles = triangles;
                mesh.RecalculateNormals(); mesh.RecalculateBounds();
                EditorUtility.SetDirty(mesh); AssetDatabase.SaveAssetIfDirty(mesh);
                shapes[index] = mesh;
            }
            return shapes;
        }
    }
}
