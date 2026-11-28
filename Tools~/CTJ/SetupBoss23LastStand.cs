using System;
using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.SkillSystem;
using _Scripts.UI.Enemy;
using UnityEditor;
using UnityEngine;

public static class SetupBoss23LastStand
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string SkillPath = "Assets/_Scripts/Agent/Enemies/EnemySkills/Enemy23/Boss23_ProductionControlDrone_LastStandEntry.asset";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        var so = AssetDatabase.LoadAssetAtPath<SkillSO>(SkillPath);
        if (so == null)
        {
            so = ScriptableObject.CreateInstance<SkillSO>();
            AssetDatabase.CreateAsset(so, SkillPath);
        }

        var root = PrefabUtility.LoadPrefabContents(Prefab);
        try
        {
            var module = root.GetComponentInChildren<EnemySkillModule>(true);
            var visual = root.GetComponentInChildren<DroneVisualModule>(true);
            var air = root.GetComponentInChildren<DroneAirTransitionSkill>(true);
            var bomb = root.GetComponentInChildren<DroneBombSkill>(true);
            var entry = root.GetComponentInChildren<DroneLastStandEntrySkill>(true);
            if (entry == null) entry = Child(module.transform, "LastStandEntry").gameObject.AddComponent<DroneLastStandEntrySkill>();

            var data = new SerializedObject(entry);
            data.FindProperty("<SkillSo>k__BackingField").objectReferenceValue = so;
            data.FindProperty("<BaseCoolTime>k__BackingField").floatValue = 0f;
            data.FindProperty("fallbackAttackDelay").floatValue = 0f;
            data.FindProperty("postBlinkDelay").floatValue = 0f;
            data.FindProperty("blinkBeforeAttack").boolValue = false;
            data.FindProperty("visual").objectReferenceValue = visual;
            data.FindProperty("bombardment").objectReferenceValue = bomb;
            var airData = new SerializedObject(air);
            foreach (string field in new[] { "projectilePrefab", "muzzle", "rearMuzzle" })
                data.FindProperty(field).objectReferenceValue = airData.FindProperty(field).objectReferenceValue;
            data.ApplyModifiedPropertiesWithoutUndo();

            data = new SerializedObject(module);
            var entries = data.FindProperty("skills");
            int index = -1;
            for (int i = 0; i < entries.arraySize; i++)
                if (entries.GetArrayElementAtIndex(i).FindPropertyRelative("skill").objectReferenceValue == entry) index = i;
            if (index < 0) { index = entries.arraySize; entries.arraySize++; }
            var slot = entries.GetArrayElementAtIndex(index);
            slot.FindPropertyRelative("skill").objectReferenceValue = entry;
            slot.FindPropertyRelative("useSkill").boolValue = true;
            slot.FindPropertyRelative("useForAutomaticSelection").boolValue = false;
            slot.FindPropertyRelative("requireGrounded").boolValue = false;
            slot.FindPropertyRelative("requireClearPath").boolValue = false;
            data.ApplyModifiedPropertiesWithoutUndo();
            Set(root.GetComponent<ProductionControlDrone>(), "lastStandEntry", entry);

            var sound = root.GetComponentInChildren<EnemySoundModule>(true);
            data = new SerializedObject(sound);
            var sounds = data.FindProperty("attackSounds");
            UnityEngine.Object cue = null;
            index = -1;
            for (int i = 0; i < sounds.arraySize; i++)
            {
                var item = sounds.GetArrayElementAtIndex(i);
                if (item.FindPropertyRelative("skill").objectReferenceValue == air)
                    cue = item.FindPropertyRelative("cue").objectReferenceValue;
                if (item.FindPropertyRelative("skill").objectReferenceValue == entry) index = i;
            }
            if (index < 0) { index = sounds.arraySize; sounds.arraySize++; }
            sounds.GetArrayElementAtIndex(index).FindPropertyRelative("skill").objectReferenceValue = entry;
            sounds.GetArrayElementAtIndex(index).FindPropertyRelative("cue").objectReferenceValue = cue;
            data.ApplyModifiedPropertiesWithoutUndo();

            var feedback = root.GetComponentInChildren<DroneLastStandFeedbackModule>(true);
            if (feedback == null) feedback = Child(root.transform, "LastStandFeedbackModule").gameObject.AddComponent<DroneLastStandFeedbackModule>();
            var visualRoot = new SerializedObject(visual).FindProperty("movementVisual").objectReferenceValue as Transform;
            var canvasRoot = Child(visualRoot, "InvulnerableShield", true);
            var canvas = canvasRoot.GetComponent<Canvas>();
            if (canvas == null) canvas = canvasRoot.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            var body = ((Animator)new SerializedObject(visual).FindProperty("animator").objectReferenceValue).GetComponent<SpriteRenderer>();
            canvas.sortingLayerID = body.sortingLayerID;
            canvas.sortingOrder = body.sortingOrder - 1;
            canvasRoot.localScale = Vector3.one * 0.01f;
            var rect = (RectTransform)canvasRoot;
            rect.sizeDelta = Vector2.one * 280f;
            var graphicRoot = Child(canvasRoot, "CircleAndRings", true);
            if (graphicRoot.GetComponent<CanvasRenderer>() == null) graphicRoot.gameObject.AddComponent<CanvasRenderer>();
            var shield = graphicRoot.GetComponent<DroneShieldGraphic>();
            if (shield == null) shield = graphicRoot.gameObject.AddComponent<DroneShieldGraphic>();
            var graphicRect = (RectTransform)graphicRoot;
            graphicRect.anchorMin = Vector2.zero; graphicRect.anchorMax = Vector2.one;
            graphicRect.offsetMin = graphicRect.offsetMax = Vector2.zero;
            shield.raycastTarget = false;
            shield.enabled = false;
            Set(feedback, "shield", shield);

            var sparksRoot = Child(visualRoot, "GroggySparks");
            var sparks = sparksRoot.GetComponent<ParticleSystem>();
            if (sparks == null) sparks = sparksRoot.gameObject.AddComponent<ParticleSystem>();
            sparks.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            var main = sparks.main;
            main.playOnAwake = false; main.loop = true; main.duration = 1f;
            main.maxParticles = 80;
            main.simulationSpace = ParticleSystemSimulationSpace.World;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.25f, 0.5f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(1.6f, 3.8f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.09f, 0.2f);
            main.gravityModifier = 0.65f;
            var palette = new Gradient();
            palette.SetKeys(new[] { new GradientColorKey(new Color(1f, 0.95f, 0.15f), 0f),
                new GradientColorKey(new Color(1f, 0.78f, 0.04f), 0.75f),
                new GradientColorKey(new Color(1f, 0.32f, 0.015f), 0.76f),
                new GradientColorKey(new Color(1f, 0.5f, 0.025f), 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 1f) });
            main.startColor = new ParticleSystem.MinMaxGradient(palette) { mode = ParticleSystemGradientMode.RandomColor };
            var emission = sparks.emission; emission.rateOverTime = 60f;
            var shape = sparks.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Circle; shape.radius = 0.65f;
            var velocity = sparks.velocityOverLifetime; velocity.enabled = true; velocity.z = 0f;
            var color = sparks.colorOverLifetime; color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(1f, 0.6f), new GradientAlphaKey(0f, 1f) });
            color.color = new ParticleSystem.MinMaxGradient(fade);
            var limit = sparks.limitVelocityOverLifetime; limit.enabled = true; limit.limit = 4f; limit.dampen = 0.1f;
            var size = sparks.sizeOverLifetime; size.enabled = true;
            size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.Linear(0f, 1f, 1f, 0f));
            var renderer = sparks.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.velocityScale = 0.025f; renderer.lengthScale = 2f;
            renderer.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Graphics/Materials/Enemy/AttackPreparationParticle.mat");
            renderer.sortingLayerID = body.sortingLayerID; renderer.sortingOrder = body.sortingOrder + 2;
            Set(feedback, "malfunctionSparks", sparks);

            PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            return new { skills = module.Skills.Count, shield = "sequential purple circle + two rotating blue arcs", groggy = 5f, damageMultiplier = 2f };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    private static Transform Child(Transform parent, string name, bool rect = false)
    {
        var child = parent.Find(name);
        if (child != null) return child;
        child = new GameObject(name, rect ? typeof(RectTransform) : typeof(Transform)).transform;
        child.SetParent(parent, false);
        return child;
    }

    private static void Set(UnityEngine.Object target, string field, UnityEngine.Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(field).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
