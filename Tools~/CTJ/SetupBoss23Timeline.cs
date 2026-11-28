using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupBoss23Timeline
{
    private const string Folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        var controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Anim + ".controller");
        Clip(controller, "AirIdle", Slice("moving"), 0.1f, true);
        Clip(controller, "AirShot", Slice("shot"), 0.08f, false);
        var crouch = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "_CrouchCharge.anim");
        var frames = AnimationUtility.GetObjectReferenceCurve(crouch, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"))
            .Take(5).Reverse().Select(k => (Sprite)k.value).ToArray();
        Clip(controller, "CrouchRecover", frames, 0.13f, false, new[] { -8f, -8f, -2f, -2f, 0f });

        string path = Folder + "Boss23_ProductionControlDrone.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            var boss = root.GetComponent<ProductionControlDrone>();
            Set(boss, "moveSpeed", 2.4f);
            Set(boss, "ascentSpeed", 10f);
            Set(boss, "descentSpeed", 10f);
            var hit = root.GetComponentInChildren<EnemyHitReactionModule>(true);
            Set(hit, "enableKnockback", false);
            Set(hit, "knockbackSpeed", 0f);
            Set(hit, "knockbackDuration", 0f);
            Set(hit, "hitStunDuration", 0f);
            Set(hit, "cancelAttackOnHit", false);
            Set(hit, "hurtAnimator", null);

            EnemyAttackSkill[] timeline = { root.GetComponentInChildren<DroneChargeSkill>(true),
                root.GetComponentInChildren<DroneSalvoSkill>(true), root.GetComponentInChildren<DroneAirTransitionSkill>(true),
                root.GetComponentInChildren<DroneBombSkill>(true) };
            foreach (var skill in timeline)
            {
                Set(skill, "attackDelayStat", null);
                Set(skill, "fallbackAttackDelay", 0f);
                Set(skill, "postBlinkDelay", 0f);
                Set(skill, "blinkBeforeAttack", false);
            }

            var charge = timeline[0];
            Set(charge, "damage", 18f); Set(charge, "lastStandDamage", 22f);
            Set(charge, "dashAnimationSpeed", 2f);
            Set(charge, "followDuration", 0.65f); Set(charge, "chargeDuration", 0.55f);
            Set(charge, "dashDuration", 1f); Set(charge, "recoveryDuration", 0.2f);
            Set(charge, "lastStandFollowDuration", 0.45f); Set(charge, "lastStandChargeDuration", 0.46f);
            Set(charge, "lastStandDashDuration", 0.68f); Set(charge, "reaimDuration", 0.34f);
            Set(charge, "lastStandRecoveryDuration", 0.2f);

            var salvo = timeline[1];
            Set(salvo, "damage", 10f); Set(salvo, "normalShotInterval", 0.32f);
            Set(salvo, "lastStandShotInterval", 0.144f); Set(salvo, "chargeEndTime", 0.55f);
            Set(salvo, "firstShotTime", 0.58f);
            Set(timeline[2], "damage", 10f); Set(timeline[2], "lastStandSpeedMultiplier", 1.2f);
            Set(timeline[2], "postAttackDelay", 0.15f);

            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>(true);
            Set(laser, "damage", 24f); Set(laser, "lastStandDamage", 28f);
            Set(laser, "lastStandSpeedMultiplier", 1.2f);
            var bomb = timeline[3];
            Set(bomb, "damage", 22f); Set(bomb, "lastStandDamage", 26f);
            Set(bomb, "pixelsPerUnit", 28f); Set(bomb, "normalRadiusPixels", 75f); Set(bomb, "lastStandRadiusPixels", 90f);
            Set(bomb, "firstDropTime", 0.8f); Set(bomb, "normalInterval", 1.05f);
            Set(bomb, "warningDuration", 1.65f);
            Set(bomb, "lastStandFirstDropTime", 0.65f); Set(bomb, "lastStandInterval", 0.508f);
            Set(bomb, "lastStandWarningDuration", 1.15f);
            TuneMotion(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { moveSpeed = 2.4f, damage = new[] { "Charge 18/22", "Barrage 10", "Air 10", "Laser 24/28", "Bomb 22/26" },
            bombRadii = new[] { 75f / 28f, 90f / 28f }, animationClips = controller.animationClips.Length };
    }

    public static object UpdateMotion()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        string path = Folder + "Boss23_ProductionControlDrone.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);
        try
        {
            TuneMotion(root);
            PrefabUtility.SaveAsPrefabAsset(root, path);
            return new { ascentSpeed = 10f, descentSpeed = 10f, dashAnimationSpeed = 2f, postAttackDelay = 0.15f, patternRecovery = 1.25f };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static void TuneMotion(GameObject root)
    {
        var boss = root.GetComponent<ProductionControlDrone>();
        Set(boss, "ascentSpeed", 10f); Set(boss, "descentSpeed", 10f); Set(boss, "patternRecoveryTime", 1.25f);
        var flight = root.GetComponentInChildren<BossFlightModule>(true);
        Set(flight, "decelerateFromStart", false); Set(flight, "easeDuration", 0.18f);
        var visual = root.GetComponentInChildren<DroneVisualModule>(true);
        Set(visual, "movementVisual", root.transform.Find("Visual")); Set(visual, "movementSmoothing", 25f);
        var charge = root.GetComponentInChildren<DroneChargeSkill>(true);
        Set(charge, "dashAnimationSpeed", 2f);
        Set(charge, "followDuration", 0.65f); Set(charge, "chargeDuration", 0.55f);
        Set(charge, "dashDuration", 1f); Set(charge, "recoveryDuration", 0.2f);
        Set(charge, "lastStandFollowDuration", 0.45f); Set(charge, "lastStandChargeDuration", 0.46f);
        Set(charge, "lastStandRecoveryDuration", 0.2f);
        var salvo = root.GetComponentInChildren<DroneSalvoSkill>(true);
        Set(salvo, "chargeEndTime", 0.55f); Set(salvo, "firstShotTime", 0.58f); Set(salvo, "lastStandFirstShotTime", 0.48f);
        Set(salvo, "firingAnimationSpeed", 1.2f); Set(salvo, "lastStandFiringAnimationSpeed", 3f); Set(salvo, "recoveryTime", 0.2f);
        var air = root.GetComponentInChildren<DroneAirTransitionSkill>(true);
        Set(air, "postAttackDelay", 0.15f); Set(air, "shotDuration", 0.41f);
        var bomb = root.GetComponentInChildren<DroneBombSkill>(true);
        Set(bomb, "firstDropTime", 0.8f); Set(bomb, "lastStandFirstDropTime", 0.65f);
    }

    private static Sprite[] Slice(string name)
    {
        string path = "Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss/flying up/" + name + ".png";
        var importer = (TextureImporter)AssetImporter.GetAtPath(path);
        importer.GetSourceTextureWidthAndHeight(out int width, out int height);
        if (width % 128 != 0 || height != 96) throw new Exception("Unexpected source dimensions");
        var factories = new SpriteDataProviderFactories(); factories.Init();
        var provider = factories.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
        var ids = provider.GetSpriteRects().ToDictionary(r => r.name, r => r.spriteID);
        var rects = Enumerable.Range(0, width / 128).Select(i => new SpriteRect { name = name + "_" + i,
            spriteID = ids.TryGetValue(name + "_" + i, out GUID id) ? id : GUID.Generate(),
            rect = new Rect(i * 128 + 32, 26, 92, 51), alignment = SpriteAlignment.Custom,
            pivot = new Vector2(32f / 92f, 24f / 51f) }).ToArray();
        provider.SetSpriteRects(rects);
        provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
        provider.Apply();
        importer.spriteImportMode = SpriteImportMode.Multiple; importer.spritePixelsPerUnit = 28f;
        importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
        importer.mipmapEnabled = false; importer.SaveAndReimport();
        var sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().ToDictionary(s => s.name);
        return rects.Select(r => sprites[r.name]).ToArray();
    }

    private static void Clip(AnimatorController controller, string stateName, Sprite[] frames, float interval, bool loop, float[] offsets = null)
    {
        string path = Anim + "_" + stateName + ".anim";
        var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
        if (clip == null) { clip = new AnimationClip { frameRate = 200f }; AssetDatabase.CreateAsset(clip, path); }
        var keys = frames.Select((f, i) => new ObjectReferenceKeyframe { time = i * interval, value = f }).ToList();
        keys.Add(new ObjectReferenceKeyframe { time = frames.Length * interval, value = loop ? frames[0] : frames.Last() });
        AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys.ToArray());
        var settings = AnimationUtility.GetAnimationClipSettings(clip); settings.loopTime = loop;
        AnimationUtility.SetAnimationClipSettings(clip, settings);
        foreach (string child in new[] { "ProcessNumber", "AlarmLamp" })
        {
            Vector2 anchor = child == "ProcessNumber" ? new Vector2(-15.5f, 10.5f) : new Vector2(-11.5f, 18.5f);
            foreach (string axis in new[] { "x", "y" })
            {
                var curve = new AnimationCurve(keys.Select((k, i) => new Keyframe(k.time,
                    (axis == "x" ? anchor.x : anchor.y + (offsets == null ? 0f : offsets[Mathf.Min(i, offsets.Length - 1)])) / 28f)).ToArray());
                for (int i = 0; i < curve.length; i++) AnimationUtility.SetKeyRightTangentMode(curve, i, AnimationUtility.TangentMode.Constant);
                AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve(child, typeof(Transform), "m_LocalPosition." + axis), curve);
            }
        }

        var machine = controller.layers[0].stateMachine;
        var state = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == stateName) ?? machine.AddState(stateName);
        state.motion = clip; state.writeDefaultValues = false;
        EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(clip); AssetDatabase.SaveAssetIfDirty(controller);
    }

    private static void Set(Object target, string field, object value)
    {
        var data = new SerializedObject(target);
        var property = data.FindProperty(field) ?? throw new Exception("Missing field: " + field);
        if (property.propertyType == SerializedPropertyType.ObjectReference) property.objectReferenceValue = value as Object;
        else if (property.propertyType == SerializedPropertyType.Boolean) property.boolValue = (bool)value;
        else property.floatValue = Convert.ToSingle(value);
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
