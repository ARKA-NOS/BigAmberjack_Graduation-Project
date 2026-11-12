using System;
using _Scripts.Agent.Enemies;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupMetalFragments
{
    private const string BloodPath = "Assets/_Prefabs/Enemies/Effects/HitBloodSpray.prefab";
    private const string MetalPath = "Assets/_Prefabs/Enemies/Effects/HitMetalFragments.prefab";
    private const string RobotPath = "Assets/_Prefabs/Enemies/Enemy/Enemy02/Enemy02_ProjectileRobot.prefab";
    private const string ArcherPath = "Assets/_Prefabs/Enemies/Enemy/Enemy04/Enemy04_MechanicArcher.prefab";
    private const string MeshFolder = "Assets/_Graphics/Effects/MetalMesh";

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        if (typeof(EnemyMetalFragmentParticle).GetField("attachmentParticles",
            System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic) == null)
            throw new Exception("Unity has not loaded the new metal fragment script");
        var blood = AssetDatabase.LoadAssetAtPath<GameObject>(BloodPath).GetComponent<ParticleSystem>();
        bool creating = AssetDatabase.LoadAssetAtPath<GameObject>(MetalPath) == null;
        if (creating && !AssetDatabase.CopyAsset(BloodPath, MetalPath))
            throw new Exception("Could not copy particle prefab");
        if (!AssetDatabase.IsValidFolder(MeshFolder))
            AssetDatabase.CreateFolder("Assets/_Graphics/Effects", "MetalMesh");
        var meshes = new[]
        {
            SavePlate("MetalPlate", new[]
            {
                new Vector2(-0.47f, -0.05f), new Vector2(-0.35f, 0.15f), new Vector2(-0.38f, 0.33f),
                new Vector2(-0.13f, 0.34f), new Vector2(0.07f, 0.49f), new Vector2(0.18f, 0.29f),
                new Vector2(0.46f, 0.20f), new Vector2(0.38f, -0.04f), new Vector2(0.48f, -0.22f),
                new Vector2(0.19f, -0.29f), new Vector2(-0.02f, -0.48f), new Vector2(-0.18f, -0.30f),
                new Vector2(-0.38f, -0.34f)
            }, new[] { 12, 0, 1, 1, 2, 3, 12, 1, 3, 12, 3, 4, 12, 4, 5, 12, 5, 6,
                12, 6, 7, 7, 8, 9, 7, 9, 10, 7, 10, 11, 7, 11, 12 }),
            SavePlate("MetalPlateClipped", new[]
            {
                new Vector2(-0.48f, -0.29f), new Vector2(-0.44f, 0.25f), new Vector2(-0.16f, 0.49f),
                new Vector2(0.10f, 0.13f), new Vector2(0.28f, 0.40f), new Vector2(0.50f, 0.13f),
                new Vector2(0.36f, -0.36f), new Vector2(-0.08f, -0.47f)
            }, new[] { 7, 0, 1, 7, 1, 2, 7, 2, 3, 7, 3, 4, 7, 4, 5, 5, 6, 7 }),
            SavePlate("MetalPlateTapered", new[]
            {
                new Vector2(-0.47f, -0.43f), new Vector2(-0.43f, 0.15f), new Vector2(-0.21f, 0.47f),
                new Vector2(0.23f, 0.49f), new Vector2(0.49f, 0.27f), new Vector2(0.13f, 0.16f),
                new Vector2(-0.04f, -0.11f), new Vector2(0.11f, -0.32f), new Vector2(-0.21f, -0.48f)
            }, new[] { 8, 0, 1, 8, 1, 2, 8, 2, 3, 3, 4, 5, 3, 5, 6, 8, 3, 6, 6, 7, 8 }),
            SavePlate("MetalPlateWithTab", new[]
            {
                new Vector2(-0.49f, -0.19f), new Vector2(-0.42f, 0.08f), new Vector2(-0.20f, 0.15f),
                new Vector2(-0.31f, 0.40f), new Vector2(0.09f, 0.50f), new Vector2(0.30f, 0.26f),
                new Vector2(0.48f, 0.25f), new Vector2(0.35f, -0.01f), new Vector2(0.44f, -0.32f),
                new Vector2(0.08f, -0.47f), new Vector2(-0.15f, -0.35f), new Vector2(-0.37f, -0.44f)
            }, new[] { 11, 0, 1, 11, 1, 2, 2, 3, 4, 11, 2, 4, 11, 4, 5, 11, 5, 6,
                11, 6, 7, 7, 8, 9, 7, 9, 10, 7, 10, 11 })
        };
        var parts = new[]
        {
            SavePlate("MetalPartBracket", new[]
            {
                new Vector2(-0.46f, -0.42f), new Vector2(-0.46f, 0.45f), new Vector2(-0.06f, 0.45f),
                new Vector2(-0.06f, -0.04f), new Vector2(0.48f, -0.04f), new Vector2(0.48f, -0.42f)
            }),
            SavePlate("MetalPartPin", new[]
            {
                new Vector2(-0.48f, -0.11f), new Vector2(-0.40f, 0.14f), new Vector2(0.35f, 0.16f),
                new Vector2(0.50f, 0.06f), new Vector2(0.38f, -0.15f), new Vector2(-0.36f, -0.15f)
            }),
            SaveWasher()
        };

        var root = PrefabUtility.LoadPrefabContents(MetalPath);
        try
        {
            root.name = "HitMetalFragments";
            if (creating)
            {
                Object.DestroyImmediate(root.GetComponent<EnemyBloodParticle>());
                root.AddComponent<EnemyMetalFragmentParticle>();
            }
            var emitter = root.GetComponent<EnemyMetalFragmentParticle>();
            if (emitter == null) throw new Exception("Existing metal effect has no fragment emitter");
            var bodyRenderer = root.GetComponent<ParticleSystemRenderer>();
            bodyRenderer.SetMeshes(meshes);
            var data = new SerializedObject(emitter);
            data.FindProperty("attachmentParticles").objectReferenceValue = ConfigureAttachments(root, bodyRenderer, parts);
            var colors = data.FindProperty("attachmentColors");
            colors.arraySize = 2;
            colors.GetArrayElementAtIndex(0).colorValue = new Color(0.83f, 0.83f, 0.82f);
            colors.GetArrayElementAtIndex(1).colorValue = new Color(0.70f, 0.70f, 0.70f);
            data.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, MetalPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        var metalParticle = AssetDatabase.LoadAssetAtPath<GameObject>(MetalPath).GetComponent<ParticleSystem>();
        LinkHitEffect(RobotPath, blood, metalParticle);
        LinkHitEffect(ArcherPath, blood, metalParticle);
        return new { prefab = MetalPath, appliedTo = new[] { RobotPath, ArcherPath }, meshes = meshes.Length,
            attachmentMeshes = parts.Length };
    }

    private static ParticleSystem ConfigureAttachments(GameObject root, ParticleSystemRenderer body, Mesh[] meshes)
    {
        const string childName = "Attachment Particles";
        var child = root.transform.Find(childName);
        if (child == null)
        {
            child = new GameObject(childName).transform;
            child.SetParent(root.transform, false);
            child.gameObject.layer = root.layer;
        }
        var particles = child.GetComponent<ParticleSystem>();
        if (particles == null) particles = child.gameObject.AddComponent<ParticleSystem>();
        var main = particles.main;
        main.playOnAwake = false;
        main.loop = false;
        var emission = particles.emission;
        emission.enabled = false;
        var shape = particles.shape;
        shape.enabled = false;
        var renderer = child.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Mesh;
        renderer.alignment = body.alignment;
        renderer.sharedMaterial = body.sharedMaterial;
        renderer.enableGPUInstancing = body.enableGPUInstancing;
        renderer.sortingLayerID = body.sortingLayerID;
        renderer.sortingOrder = body.sortingOrder + 1;
        renderer.SetMeshes(meshes);
        return particles;
    }

    private static Mesh SaveWasher()
    {
        const int sides = 6;
        var outline = new Vector2[sides * 2];
        var triangles = new int[sides * 6];
        for (int i = 0; i < sides; i++)
        {
            float angle = -i * 2f * Mathf.PI / sides;
            var direction = new Vector2(Mathf.Cos(angle), Mathf.Sin(angle));
            outline[i] = direction * 0.5f;
            outline[i + sides] = direction * 0.21f;
            int next = (i + 1) % sides;
            triangles[i * 6] = i;
            triangles[i * 6 + 1] = next;
            triangles[i * 6 + 2] = next + sides;
            triangles[i * 6 + 3] = i;
            triangles[i * 6 + 4] = next + sides;
            triangles[i * 6 + 5] = i + sides;
        }
        return SavePlate("MetalPartWasher", outline, triangles);
    }

    private static Mesh SavePlate(string name, Vector2[] outline, int[] triangles = null)
    {
        string path = MeshFolder + "/" + name + ".asset";
        var mesh = AssetDatabase.LoadAssetAtPath<Mesh>(path);
        bool creating = mesh == null;
        if (creating) mesh = new Mesh { name = name };
        mesh.Clear();
        mesh.vertices = Array.ConvertAll(outline, point => new Vector3(point.x, point.y, 0f));
        if (triangles == null)
        {
            // 볼록한 외곽선은 첫 정점을 기준으로 삼각형을 이어서 채웁니다.
            triangles = new int[(outline.Length - 2) * 3];
            for (int i = 0; i < outline.Length - 2; i++)
            {
                triangles[i * 3] = 0;
                triangles[i * 3 + 1] = i + 1;
                triangles[i * 3 + 2] = i + 2;
            }
        }
        mesh.triangles = triangles;
        mesh.RecalculateBounds();
        if (creating) AssetDatabase.CreateAsset(mesh, path);
        else
        {
            EditorUtility.SetDirty(mesh);
            AssetDatabase.SaveAssetIfDirty(mesh);
        }
        return mesh;
    }

    private static void LinkHitEffect(string path, ParticleSystem blood, ParticleSystem metal)
    {
        var enemy = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var module = enemy.GetComponentInChildren<EnemyParticleModule>(true);
            var data = new SerializedObject(module);
            var hitEffects = data.FindProperty("hitEffects");
            for (int i = 0; i < hitEffects.arraySize; i++)
            {
                var entry = hitEffects.GetArrayElementAtIndex(i);
                var prefab = entry.FindPropertyRelative("prefab");
                if (prefab.objectReferenceValue == metal) return;
                if (prefab.objectReferenceValue != blood) continue;
                prefab.objectReferenceValue = metal;
                entry.FindPropertyRelative("label").stringValue = "Metal Fragments";
                data.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(enemy, path);
                return;
            }
            throw new Exception("No original blood hit slot on " + path);
        }
        finally { PrefabUtility.UnloadPrefabContents(enemy); }
    }
}
