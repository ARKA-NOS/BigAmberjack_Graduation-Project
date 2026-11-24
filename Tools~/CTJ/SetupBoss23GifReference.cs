using System;
using System.Linq;
using System.Security.Cryptography;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23GifReference
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone";
    private const string Crouch = "Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss/Crouch/";

    public static object ReadExisting()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Boss);
        var snapshots = root.GetComponentsInChildren<EnemyAttackSkill>(true).Select(skill =>
        {
            var data = new SerializedObject(skill);
            var iterator = data.GetIterator();
            var fields = new System.Collections.Generic.List<string>();
            while (iterator.Next(true))
                if (iterator.name.StartsWith("lastStand", StringComparison.Ordinal) || iterator.name == "reaimDuration")
                    fields.Add(iterator.name + "=" + (iterator.propertyType == SerializedPropertyType.Integer
                        ? iterator.intValue.ToString() : iterator.floatValue.ToString("R", System.Globalization.CultureInfo.InvariantCulture)));
            return new { name = skill.GetType().Name, fields = fields.ToArray() };
        }).ToArray();
        string[] clips = { "BombCharge", "Bomb", "CrouchFire", "Charge", "AirIdle", "AirShot", "Rise", "Descend", "Laser", "LaserCharge" };
        using (var sha = SHA256.Create())
            return new { enhanced = snapshots, protectedClips = clips.Select(name => new { name,
                hash = BitConverter.ToString(sha.ComputeHash(System.IO.File.ReadAllBytes(Anim + "_" + name + ".anim"))).Replace("-", "") }).ToArray() };
    }

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + ".controller");
        var frames = AssetDatabase.LoadAllAssetsAtPath(Crouch + "speedy shot.png").OfType<Sprite>().ToDictionary(s => s.name);
        Sprite quiet = QuietSprite();
        Clip(controller, "SalvoSingleShot", new[] { frames["speedy shot_0"], quiet, quiet },
            new[] { 0f, 0.055f, 0.16f }, false);
        Clip(controller, "GroundRush", new[] { frames["speedy shot_0"], frames["speedy shot_2"], frames["speedy shot_0"] },
            new[] { 0f, 0.06f, 0.12f }, true);

        var root = PrefabUtility.LoadPrefabContents(Boss);
        try
        {
            var visual = root.GetComponentInChildren<DroneVisualModule>(true);
            var air = root.GetComponentInChildren<DroneAirTransitionSkill>(true);
            var bomb = root.GetComponentInChildren<DroneBombSkill>(true);
            Set(air, "normalAscentSpeedMultiplier", 0.65f);
            Set(bomb, "normalHoverSpeed", 3.2f);
            Set(bomb, "normalHoverOffset", 1.2f);
            Set(visual, "basicHoverAmplitude", 0.06f);
            Set(visual, "basicHoverFrequency", 1.4f);

            Transform facing = root.transform.Find("Visual");
            var child = facing.Find("BasicFlightExhaust");
            var particles = child != null ? child.GetComponent<ParticleSystem>() : null;
            if (particles == null)
            {
                var node = new GameObject("BasicFlightExhaust");
                node.transform.SetParent(facing, false);
                node.transform.localPosition = new Vector3(-0.1f, -0.6f, 0f);
                node.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
                particles = node.AddComponent<ParticleSystem>();
            }
            particles.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = particles.main;
            main.playOnAwake = false; main.loop = true; main.duration = 1f;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.3f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.2f, 1.6f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.025f, 0.05f);
            main.startColor = new Color(0.65f, 1f, 0.8f, 0.4f);
            main.gravityModifier = 0.1f; main.maxParticles = 24;
            var emission = particles.emission; emission.enabled = true; emission.rateOverTime = 14f;
            var shape = particles.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 12f; shape.radius = 0.05f;
            var fade = particles.colorOverLifetime; fade.enabled = true;
            var gradient = new Gradient();
            gradient.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
            fade.color = gradient;
            var renderer = particles.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Enemy/AttackPreparationParticle.mat");
            var body = root.GetComponentInChildren<Animator>(true).GetComponent<SpriteRenderer>();
            renderer.sortingLayerID = body.sortingLayerID; renderer.sortingOrder = body.sortingOrder - 1;
            Set(visual, "basicFlightExhaust", particles);
            PrefabUtility.SaveAsPrefabAsset(root, Boss);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { basicShotClip = "SalvoSingleShot", flashWindows = 1, basicRushClip = "GroundRush",
            normalAscentSpeed = 6.5f, basicBombHoverSpeed = 3.2f, enhancedSettingsUntouched = true };
    }

    private static Sprite QuietSprite()
    {
        string path = Crouch + "movingt.png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var existing = provider.GetSpriteRects().FirstOrDefault(r => r.name == "movingt_0");
        var rect = new SpriteRect { name = "movingt_0", spriteID = existing != null ? existing.spriteID : GUID.Generate(),
            rect = new Rect(32, 9, 92, 68), alignment = SpriteAlignment.Custom, pivot = new Vector2(32f / 92f, 18f / 68f) };
        provider.SetSpriteRects(new[] { rect });
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(new[] { new SpriteNameFileIdPair(rect.name, rect.spriteID) });
        provider.Apply(); importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 28f;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.SaveAndReimport();
        return AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().Single();
    }

    public static object PreviewShot()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents(Boss);
        var cameraRoot = new GameObject("Shot preview camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraRoot, root.scene);
        var camera = cameraRoot.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = root.scene;
        camera.orthographic = true; camera.orthographicSize = 1.5f;
        camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.05f, 0.065f, 0.075f);
        var target = RenderTexture.GetTemporary(512, 320, 24);
        var previous = RenderTexture.active;
        var sheet = new Texture2D(1536, 320, TextureFormat.RGB24, false);
        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            foreach (string name in new[] { "ProcessNumber", "AlarmLamp" })
                animator.transform.Find(name).GetComponent<SpriteRenderer>().sharedMaterial = body.sharedMaterial;
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "_SalvoSingleShot.anim");
            for (int i = 0; i < 3; i++)
            {
                clip.SampleAnimation(animator.gameObject, new[] { 0f, 0.07f, 0.14f }[i]);
                camera.transform.position = root.transform.position + new Vector3(0.65f, 0.3f, -10f);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0, 0, 512, 320), i * 512, 0);
            }
            sheet.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23SingleFlashPreview.png", sheet.EncodeToPNG());
            return "Tools~/CTJ/Boss23SingleFlashPreview.png";
        }
        finally
        {
            RenderTexture.active = previous; camera.targetTexture = null;
            RenderTexture.ReleaseTemporary(target); Object.DestroyImmediate(sheet); Object.DestroyImmediate(cameraRoot);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static void Clip(AnimatorController controller, string name, Sprite[] frames, float[] times, bool loop)
    {
        string path = Anim + "_" + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip { frameRate = 200f }; AssetDatabase.CreateAsset(clip, path); }
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
            frames.Select((s, i) => new ObjectReferenceKeyframe { time = times[i], value = s }).ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        foreach (string child in new[] { "ProcessNumber", "AlarmLamp" })
        {
            Vector2 anchor = child == "ProcessNumber" ? new Vector2(-15.5f, 2.5f) : new Vector2(-11.5f, 10.5f);
            foreach (string axis in new[] { "x", "y" })
            {
                float value = (axis == "x" ? anchor.x : anchor.y) / 28f;
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition." + axis),
                    AnimationCurve.Constant(0f, times.Last(), value));
            }
        }
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = clip; state.writeDefaultValues = false;
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(clip); AssetDatabase.SaveAssetIfDirty(controller);
    }

    private static void Set(Object target, string field, Object value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Set(Object target, string field, float value)
    {
        var data = new SerializedObject(target); data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
