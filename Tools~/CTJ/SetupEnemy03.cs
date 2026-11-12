using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

// Unity Pipeline run_script 전용. 원본 프리팹과 현재 씬은 저장하지 않습니다.
public static class SetupEnemy03
{
    public const string PrefabPath = "Assets/_Prefabs/Enemies/Enemy/Enemy03/Enemy03_TwistedHuman.prefab";
    private const string StatsPath = "Assets/_Scripts/Agent/Enemies/EnemyStatGroups/Enemy03_TwistedHuman_Stats.asset";
    private const string SkillFolder = "Assets/_Scripts/Agent/Enemies/EnemySkills/Enemy03";
    private const string AnimationFolder = "Assets/_Graphics/Animations/Enemy/Enemy03";

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
            throw new Exception("Enemy03 already exists. Preserve Inspector changes.");
        Folder("Assets/_Prefabs/Enemies/Enemy/Enemy03");
        Folder(SkillFolder);
        Folder(AnimationFolder);

        var stats = Copy("Assets/_Scripts/Agent/Enemies/EnemyStatGroups/Enemy01_TwistedArm_Stats.asset", StatsPath);
        var statData = new SerializedObject(stats);
        var values = statData.FindProperty("<Stats>k__BackingField");
        values.GetArrayElementAtIndex(0).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 80f;
        values.GetArrayElementAtIndex(1).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 0.45f;
        values.GetArrayElementAtIndex(2).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = 8f;
        statData.ApplyModifiedPropertiesWithoutUndo();
        AssetDatabase.SaveAssetIfDirty(stats);
        var slashSO = Copy("Assets/_Scripts/Agent/Enemies/EnemySkills/Melee/Enemy01_TwistedArm_Melee.asset",
            SkillFolder + "/Enemy03_Slash.asset");
        var leapSO = Copy("Assets/_Scripts/Agent/Enemies/EnemySkills/Melee/Enemy01_TwistedArm_Melee.asset",
            SkillFolder + "/Enemy03_Leap.asset");
        var controller = (AnimatorController)Copy("Assets/_Graphics/Animations/Enemy/Enemy01/Enemy01_Zombie.controller",
            AnimationFolder + "/Enemy03_Zombie.controller");
        var stateMachine = controller.layers[0].stateMachine;
        var attackState = stateMachine.states.Single(s => s.state.name == "Attack").state;
        var sourceClip = (AnimationClip)attackState.motion;
        var attackClip = Object.Instantiate(sourceClip);
        attackClip.name = "Enemy03_Slash";
        float factor = 0.12f / sourceClip.length;
        foreach (var binding in AnimationUtility.GetCurveBindings(attackClip))
        {
            var curve = AnimationUtility.GetEditorCurve(attackClip, binding);
            var keys = curve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                keys[i].time *= factor;
                keys[i].inTangent /= factor;
                keys[i].outTangent /= factor;
            }
            curve.keys = keys;
            AnimationUtility.SetEditorCurve(attackClip, binding, curve);
        }
        foreach (var binding in AnimationUtility.GetObjectReferenceCurveBindings(attackClip))
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(attackClip, binding);
            for (int i = 0; i < keys.Length; i++) keys[i].time *= factor;
            AnimationUtility.SetObjectReferenceCurve(attackClip, binding, keys);
        }
        AnimationUtility.SetAnimationEvents(attackClip, Array.Empty<AnimationEvent>());
        var settings = AnimationUtility.GetAnimationClipSettings(attackClip);
        settings.stopTime = 0.12f;
        settings.loopTime = false;
        AnimationUtility.SetAnimationClipSettings(attackClip, settings);
        AssetDatabase.CreateAsset(attackClip, AnimationFolder + "/Enemy03_Slash.anim");
        attackState.motion = attackClip;
        var idleMotion = stateMachine.states.Single(s => s.state.name == "Idle").state.motion;
        var flightState = stateMachine.AddState("Leap", new Vector3(650, 120));
        flightState.motion = idleMotion;
        flightState.writeDefaultValues = false;
        var landingState = stateMachine.AddState("LeapLand", new Vector3(650, 240));
        landingState.motion = idleMotion;
        landingState.writeDefaultValues = false;
        EditorUtility.SetDirty(controller);
        AssetDatabase.SaveAssetIfDirty(controller);

        var root = PrefabUtility.LoadPrefabContents("Assets/_Prefabs/Enemies/Enemy/Enemy01/Enemy01_TwistedArm.prefab");
        try
        {
            root.name = "Enemy03_TwistedHuman";
            root.transform.localScale = Vector3.one;
            root.GetComponent<Rigidbody2D>().collisionDetectionMode = CollisionDetectionMode2D.Continuous;
            var enemy = root.GetComponent<EnemyBase>();
            Set(enemy, "detectionRange", 9f);
            Set(enemy, "chaseReleaseRange", 12f);
            Set(enemy, "moveSpeed", 3.8f);
            var movement = root.GetComponentInChildren<EnemyMovementModule>(true);
            var body = (BoxCollider2D)new SerializedObject(movement).FindProperty("bodyCollider").objectReferenceValue;
            float visualScale = 1.8f / body.size.y;
            foreach (var collider in root.GetComponentsInChildren<BoxCollider2D>(true))
            {
                if (collider != body && collider.gameObject != root) continue;
                collider.size *= visualScale;
                collider.offset *= visualScale;
            }
            var facing = (Transform)new SerializedObject(enemy).FindProperty("facingRoot").objectReferenceValue;
            facing.localScale = Vector3.one * visualScale;
            var animator = root.GetComponentInChildren<Animator>(true);
            animator.runtimeAnimatorController = controller;
            var pose = new GameObject("LeapPose").transform;
            pose.SetParent(facing, false);
            animator.transform.SetParent(pose, false);
            var hitbox = root.GetComponentsInChildren<BoxCollider2D>(true).Single(c => c.name == "AttackHitbox");
            hitbox.size = new Vector2(1.6f, 1.2f) / visualScale;
            hitbox.offset = new Vector2(0.8f, 0.6f) / visualScale;
            Set(root.GetComponentInChildren<EnemyHitReactionModule>(true), "hitStunDuration", 0.25f);
            Set(root.GetComponentInChildren<EnemyPatrolModule>(true), "halfWidth", 3f);
            Set(root.GetComponentInChildren<EnemyPatrolModule>(true), "patrolSpeed", 3.8f);
            Set(movement, "jumpHeight", 0f); // 일반 추적은 걷기만. 도약은 스킬이 담당합니다.
            var statsModule = root.GetComponentsInChildren<MonoBehaviour>(true).Single(c => c != null && c.GetType().Name == "StatModule");
            Set(statsModule, "baseStats", stats);
            var module = root.GetComponentInChildren<EnemySkillModule>(true);
            var oldAttack = root.GetComponentInChildren<EnemyMeleeSkill>(true);
            var slashObject = oldAttack.gameObject;
            Object.DestroyImmediate(oldAttack);
            slashObject.name = "SlashAttack";
            var slash = slashObject.AddComponent<EnemyTimedMeleeSkill>();
            ConfigureAttack(slash, slashSO, 2.2f, 0.45f, animator);
            Set(slash, "attackDelayStat", AssetDatabase.LoadMainAssetAtPath(AssetDatabase.GUIDToAssetPath("9d471adb7a7344f89ab2d30e31d80402")));
            Set(slash, "attackHitbox", hitbox);
            var leapObject = new GameObject("LeapAttack");
            leapObject.transform.SetParent(module.transform, false);
            var leap = leapObject.AddComponent<EnemyLeapSkill>();
            ConfigureAttack(leap, leapSO, 5f, 0.7f, animator);
            Set(leap, "requiredCoolingSkill", slash);
            Set(leap, "bodyCollider", body);
            Set(leap, "poseRoot", pose);
            var moduleData = new SerializedObject(module);
            var entries = moduleData.FindProperty("skills");
            entries.arraySize = 2;
            Entry(entries.GetArrayElementAtIndex(0), slash, 0f, 1.5f, false);
            Entry(entries.GetArrayElementAtIndex(1), leap, 3f, 6f, true);
            moduleData.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { prefab = PrefabPath, stats = StatsPath, controller = AssetDatabase.GetAssetPath(controller) };
    }

    private static void ConfigureAttack(Component skill, Object so, float cooldown, float preparation, Animator animator)
    {
        Set(skill, "<SkillSo>k__BackingField", so);
        Set(skill, "<BaseCoolTime>k__BackingField", cooldown);
        Set(skill, "fallbackAttackDelay", preparation);
        Set(skill, "postBlinkDelay", 0f);
        Set(skill, "blinkBeforeAttack", false);
        Set(skill, "attackAnimator", animator);
    }

    private static void Entry(SerializedProperty entry, Component skill, float min, float max, bool requireUsable)
    {
        entry.FindPropertyRelative("skill").objectReferenceValue = skill;
        entry.FindPropertyRelative("useSkill").boolValue = true;
        entry.FindPropertyRelative("useForAutomaticSelection").boolValue = true;
        entry.FindPropertyRelative("requireUsableToEnterAttack").boolValue = requireUsable;
        entry.FindPropertyRelative("minRange").floatValue = min;
        entry.FindPropertyRelative("maxRange").floatValue = max;
        entry.FindPropertyRelative("maxHeightDifference").floatValue = 0.5f;
        entry.FindPropertyRelative("requireGrounded").boolValue = true;
        entry.FindPropertyRelative("requireClearPath").boolValue = true;
    }

    private static void Set(Object target, string property, object value)
    {
        var data = new SerializedObject(target);
        var p = data.FindProperty(property) ?? throw new Exception(target.name + ": " + property);
        if (value is float f) p.floatValue = f;
        else if (value is bool b) p.boolValue = b;
        else p.objectReferenceValue = (Object)value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    private static Object Copy(string source, string destination)
    {
        if (!AssetDatabase.CopyAsset(source, destination)) throw new Exception("Copy failed: " + destination);
        var result = AssetDatabase.LoadMainAssetAtPath(destination);
        result.name = System.IO.Path.GetFileNameWithoutExtension(destination);
        EditorUtility.SetDirty(result);
        AssetDatabase.SaveAssetIfDirty(result);
        return result;
    }

    private static void Folder(string path)
    {
        if (AssetDatabase.IsValidFolder(path)) return;
        int split = path.LastIndexOf('/');
        Folder(path.Substring(0, split));
        AssetDatabase.CreateFolder(path.Substring(0, split), path.Substring(split + 1));
    }
}
