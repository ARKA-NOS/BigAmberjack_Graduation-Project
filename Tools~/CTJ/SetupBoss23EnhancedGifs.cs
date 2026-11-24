using System;
using System.Linq;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23EnhancedGifs
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone";
    private const string Art = "Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss/";

    public static object Inspect()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Boss);

        return new { launchers = root.GetComponentsInChildren<EnemyAttackSkill>(true)
            .Where(s => s is DroneSalvoSkill || s is DroneAirTransitionSkill)
            .Select(s => { var t = (Transform)new SerializedObject(s).FindProperty("muzzle").objectReferenceValue;
                return new { skill = s.name, point = t.name, parent = t.parent.name, local = t.localPosition.ToString("F3") }; }).ToArray() };
    }

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + ".controller");
        var ground = Sprites("Crouch/speedy shot.png");
        var air = Sprites("flying up/shot.png");
        var quiet = Sprites("Crouch/movingt.png")["movingt_0"];

        Clip(controller, "SalvoRearShot", new[] { ground["speedy shot_1"], quiet, quiet }, "SalvoSingleShot");
        Clip(controller, "AirShotFront", new[] { air["shot_3"], air["shot_4"], air["shot_0"] }, "AirIdle");
        Clip(controller, "AirShotRear", new[] { air["shot_1"], air["shot_2"], air["shot_0"] }, "AirIdle");

        var root = PrefabUtility.LoadPrefabContents(Boss);

        try
        {
            var salvo = root.GetComponentInChildren<DroneSalvoSkill>(true);
            var transition = root.GetComponentInChildren<DroneAirTransitionSkill>(true);

            RearPoint(salvo, "SalvoRearShotPoint");
            RearPoint(transition, "AirRearShotPoint");
            Set(salvo, "lastStandFiringAnimationSpeed", 1.2f);
            Set(transition, "shotgunAngle", 24f);
            Set(transition, "shotDuration", 0.24f);
            Set(transition, "shotInterval", 0.38f);
            Set(root.GetComponentInChildren<DroneBombSkill>(true), "lastStandMovementSpeedMultiplier", 1.2f);
            PrefabUtility.SaveAsPrefabAsset(root, Boss);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { alternatingClips = new[] { "SalvoSingleShot", "SalvoRearShot", "AirShotFront", "AirShotRear" },
            airVolleys = 4, normalProjectiles = 4, enhancedProjectiles = 16, shotgunAngle = 24f };
    }

    public static object Preview()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var root = PrefabUtility.LoadPrefabContents(Boss);
        var node = new GameObject("Launcher preview camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(node, root.scene);
        var camera = node.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = root.scene;
        camera.orthographic = true;
        camera.orthographicSize = 2f;
        camera.transform.position = root.transform.position + new Vector3(0.3f, 0.35f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.065f, 0.075f);
        var target = RenderTexture.GetTemporary(512, 320, 24);
        var previous = RenderTexture.active;
        var sheet = new Texture2D(1024, 640, TextureFormat.RGB24, false);

        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            foreach (string name in new[] { "ProcessNumber", "AlarmLamp" })
                animator.transform.Find(name).GetComponent<SpriteRenderer>().sharedMaterial = body.sharedMaterial;
            string[] names = { "AirShotFront", "AirShotRear", "SalvoSingleShot", "SalvoRearShot" };

            for (int i = 0; i < names.Length; i++)
            {
                AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "_" + names[i] + ".anim").SampleAnimation(animator.gameObject, 0f);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                sheet.ReadPixels(new Rect(0, 0, 512, 320), i % 2 * 512, i / 2 * 320);
            }

            sheet.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23AlternatingLaunchers.png", sheet.EncodeToPNG());

            return "Tools~/CTJ/Boss23AlternatingLaunchers.png";
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(node);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    private static System.Collections.Generic.Dictionary<string, Sprite> Sprites(string path) =>
        AssetDatabase.LoadAllAssetsAtPath(Art + path).OfType<Sprite>().ToDictionary(s => s.name);

    private static void RearPoint(EnemyAttackSkill skill, string name)
    {
        var data = new SerializedObject(skill);
        var front = (Transform)data.FindProperty("muzzle").objectReferenceValue;
        var rear = front.parent.Find(name);

        if (rear == null)
        {
            rear = new GameObject(name).transform;
            rear.SetParent(front.parent, false);
        }

        rear.localPosition = new Vector3(-0.45f, front.localPosition.y, front.localPosition.z);
        data.FindProperty("rearMuzzle").objectReferenceValue = rear;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static void Clip(AnimatorController controller, string stateName, Sprite[] sprites, string reference)
    {
        string path = Anim + "_" + stateName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);

        if (clip == null)
        {
            clip = new AnimationClip { frameRate = 200f };
            AssetDatabase.CreateAsset(clip, path);
        }

        var template = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "_" + reference + ".anim");

        foreach (var binding in AnimationUtility.GetCurveBindings(template))
            AnimationUtility.SetEditorCurve(clip, binding, AnimationUtility.GetEditorCurve(template, binding));

        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"),
            sprites.Select((s, i) => new ObjectReferenceKeyframe { time = new[] { 0f, 0.055f, 0.16f }[i], value = s }).ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip);

        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName) ?? machine.AddState(stateName);

        state.motion = clip;
        state.writeDefaultValues = false;
        EditorUtility.SetDirty(clip);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(clip);
        AssetDatabase.SaveAssetIfDirty(controller);
    }

    private static void Set(Object target, string field, float value)
    {
        var data = new SerializedObject(target);

        data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
