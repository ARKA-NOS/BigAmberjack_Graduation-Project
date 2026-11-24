using System;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEngine;

public static class TuneBoss23RushHover
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        const string path = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            var charge = root.GetComponentInChildren<DroneChargeSkill>(true);
            var visual = root.GetComponentInChildren<DroneVisualModule>(true);

            Set(charge, "dashDuration", 0.8f);
            Set(charge, "lastStandDashDuration", 0.55f);
            Set(visual, "salvoHoverHeight", 0.18f);
            Set(visual, "salvoHoverAmplitude", 0.1f);
            Set(visual, "salvoHoverFrequency", 2f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { dashDuration = 0.8f, enhancedDashDuration = 0.55f, hoverHeight = 0.18f, hoverAmplitude = 0.1f, hoverFrequency = 2f };
    }

    private static void Set(UnityEngine.Object target, string field, float value)
    {
        var data = new SerializedObject(target);

        data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
