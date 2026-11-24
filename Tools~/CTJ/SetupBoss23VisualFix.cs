using System;
using System.Linq;
using _Scripts.Agent.Enemies.Bosses;
using TMPro;
using UnityEditor;
using UnityEngine;

public static class SetupBoss23VisualFix
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";
    private const string Popup = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicator.prefab";
    private const string Font = "Assets/_Graphics/Font/Amberjack/AmberjackPixel Raster.asset";
    private const string TextMaterial = "Assets/_Graphics/Materials/Enemy/EnemyDamageIndicatorText.mat";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var root = PrefabUtility.LoadPrefabContents(Boss);

        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var idle = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "Idle.anim");
            var air = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "AirIdle.anim");
            float groundY = AnimationUtility.GetEditorCurve(idle, Binding("ProcessNumber")).Evaluate(0f);
            float airY = AnimationUtility.GetEditorCurve(air, Binding("ProcessNumber")).Evaluate(0f);
            float[] rise = { groundY - 2f / 28f, groundY - 5f / 28f, groundY - 6f / 28f,
                airY - 5f / 28f, airY, airY };

            NumberCurve("Rise", rise);
            NumberCurve("Descend", rise.Reverse().ToArray());

            foreach (var clip in animator.runtimeAnimatorController.animationClips.Distinct())
            {
                string path = AssetDatabase.GetAssetPath(clip);
                if (!path.StartsWith("Assets/_Graphics/Animations/Enemy/Enemy23/", StringComparison.Ordinal))
                    throw new Exception("Unexpected animation outside Enemy23: " + path);

                var number = AnimationUtility.GetEditorCurve(clip, Binding("ProcessNumber"));
                if (number == null) continue;
                var curve = new AnimationCurve(number.keys.Select(k => new Keyframe(k.time, k.value + 10f / 28f)).ToArray());

                Constant(curve);
                AnimationUtility.SetEditorCurve(clip, Binding("AlarmLamp"), curve);
                EditorUtility.SetDirty(clip);
                AssetDatabase.SaveAssetIfDirty(clip);
            }

            var numberTransform = animator.transform.Find("ProcessNumber");
            var lamp = animator.transform.Find("AlarmLamp");
            Vector3 position = lamp.localPosition;

            position.y = numberTransform.localPosition.y + 10f / 28f;
            lamp.localPosition = position;
            PrefabUtility.SaveAsPrefabAsset(root, Boss);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Font);
        if (font == null || !font.HasCharacters("0123456789")) throw new Exception("Pixel digits missing");
        var material = AssetDatabase.LoadAssetAtPath<Material>(TextMaterial);

        EditorUtility.CopySerialized(font.material, material);
        material.name = "EnemyDamageIndicatorText";
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        var popup = PrefabUtility.LoadPrefabContents(Popup);

        try
        {
            var label = popup.GetComponentInChildren<TMP_Text>(true);

            label.font = font;
            label.fontSharedMaterial = material;
            label.fontSize = 42f;
            PrefabUtility.SaveAsPrefabAsset(popup, Popup);
        }
        finally { PrefabUtility.UnloadPrefabContents(popup); }

        return new { riseNumberPixels = new[] { 8.5f, 5.5f, 4.5f, 5.5f, 10.5f, 10.5f },
            lampRaisePixels = 2f, font = font.name, fontSize = 42f, shader = material.shader.name };
    }

    private static EditorCurveBinding Binding(string path) => EditorCurveBinding.FloatCurve(path, typeof(Transform), "m_LocalPosition.y");

    private static void NumberCurve(string state, float[] positions)
    {
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + state + ".anim");
        var frames = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
        var curve = new AnimationCurve(frames.Select((k, i) => new Keyframe(k.time, positions[Mathf.Min(i, positions.Length - 1)])).ToArray());

        Constant(curve);
        AnimationUtility.SetEditorCurve(clip, Binding("ProcessNumber"), curve);
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
    }

    private static void Constant(AnimationCurve curve)
    {
        for (int i = 0; i < curve.length; i++)
        {
            AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
            AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
        }
    }
}
