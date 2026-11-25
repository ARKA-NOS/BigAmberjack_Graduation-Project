using System;
using System.Linq;
using _Scripts.Agent.Enemies.Bosses;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23Recovery
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";
    private const string MaterialPath = "Assets/_Graphics/Materials/Enemy/Boss23_GroggySparks.mat";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        foreach (string state in new[] { "CrouchFire", "GroundRush", "SalvoSingleShot", "SalvoRearShot" })
            Markings(Clip(state), new[] { 4.5f });
        Markings(Clip("CrouchCharge"), new[] { 11.5f, 13.5f, 6.5f, 3.5f, 2.5f });
        Markings(Clip("CrouchRecover"), new[] { 2.5f, 3.5f, 6.5f, 13.5f, 11.5f });

        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim.TrimEnd('_') + ".controller");
        var hold = Clip("GroggyHold", true);
        var chargeFrames = AnimationUtility.GetObjectReferenceCurve(Clip("CrouchCharge"), SpriteBinding);
        var lowest = chargeFrames[4].value;
        AnimationUtility.SetObjectReferenceCurve(hold, SpriteBinding, new[] {
            new ObjectReferenceKeyframe { time = 0f, value = lowest }, new ObjectReferenceKeyframe { time = 0.1f, value = lowest } });
        var settings = AnimationUtility.GetAnimationClipSettings(hold);
        settings.loopTime = true;
        AnimationUtility.SetAnimationClipSettings(hold, settings);
        Markings(hold, new[] { 2.5f });
        State(controller, "GroggyHold", hold);

        var recover = Clip("GroggyRecover", true);
        EditorUtility.CopySerialized(Clip("CrouchRecover"), recover);
        recover.name = "Boss23_ProductionControlDrone_GroggyRecover";
        EditorUtility.SetDirty(recover);
        AssetDatabase.SaveAssetIfDirty(recover);
        State(controller, "GroggyRecover", recover);
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);

        const string shaderPath = "Assets/_Graphics/Shaders/Enemy/DroneGroggySpark.shader";
        AssetDatabase.ImportAsset(shaderPath, ImportAssetOptions.ForceUpdate);
        var shader = AssetDatabase.LoadAssetAtPath<Shader>(shaderPath);
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Spark shader missing/failed");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        if (material == null)
        {
            material = new Material(shader);
            AssetDatabase.CreateAsset(material, MaterialPath);
        }
        material.shader = shader;
        material.SetFloat("_GlowStrength", 0.7f);
        material.SetFloat("_CoreStrength", 1.8f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);

        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            var number = animator.transform.Find("ProcessNumber");
            var feedback = root.GetComponentInChildren<DroneLastStandFeedbackModule>(true);
            var sparks = (ParticleSystem)new SerializedObject(feedback).FindProperty("malfunctionSparks").objectReferenceValue;
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            sparks.transform.SetParent(number, false);
            sparks.transform.localPosition = Vector3.zero;
            sparks.transform.localRotation = Quaternion.identity;
            sparks.transform.localScale = Vector3.one;
            var main = sparks.main;
            main.maxParticles = 160;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.35f, 0.65f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(2.2f, 4.8f);
            var shape = sparks.shape;
            shape.radius = 0.12f;
            var emission = sparks.emission;
            emission.rateOverTime = 110f;
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.sortingLayerID = body.sortingLayerID;
            renderer.sortingOrder = body.sortingOrder - 1;
            renderer.sharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { shotMarkingRaisePixels = 2, groggySprite = lowest.name, recoverLength = recover.length,
            sparkRate = 110, sparkOrigin = "ProcessNumber", sparkOrderOffset = -1, glow = MaterialPath };
    }

    private static EditorCurveBinding SpriteBinding => EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite");

    private static AnimationClip Clip(string name, bool create = false)
    {
        string path = Anim + name + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null && create)
        {
            clip = new AnimationClip { name = "Boss23_ProductionControlDrone_" + name, frameRate = 200f };
            AssetDatabase.CreateAsset(clip, path);
        }
        return clip ?? throw new Exception("Missing clip: " + path);
    }

    private static void Markings(AnimationClip clip, float[] yPixels)
    {
        var keys = AnimationUtility.GetObjectReferenceCurve(clip, SpriteBinding);
        foreach (string child in new[] { "ProcessNumber", "AlarmLamp" })
            foreach (string axis in new[] { "x", "y" })
            {
                float x = child == "ProcessNumber" ? -15.5f : -11.5f;
                float offset = child == "ProcessNumber" ? 0f : 10f;
                var curve = new AnimationCurve(keys.Select((k, i) => new Keyframe(k.time,
                    (axis == "x" ? x : yPixels[Mathf.Min(i, yPixels.Length - 1)] + offset) / 28f)).ToArray());
                for (int i = 0; i < curve.length; i++)
                {
                    AnimationUtility.SetKeyLeftTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                    AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                }
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition." + axis), curve);
            }
        EditorUtility.SetDirty(clip);
        AssetDatabase.SaveAssetIfDirty(clip);
    }

    private static void State(AnimatorController controller, string name, AnimationClip clip)
    {
        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == name) ?? machine.AddState(name);
        state.motion = clip;
        state.writeDefaultValues = false;
    }
}
