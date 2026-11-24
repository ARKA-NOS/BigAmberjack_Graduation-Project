using System;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEngine;

public static class TuneBoss23Spacing
{
    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        const string path = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
        var root = PrefabUtility.LoadPrefabContents(path);

        try
        {
            Set(root.GetComponent<ProductionControlDrone>(), "airAttackAltitude", 4.5f);
            var arena = root.GetComponentInChildren<BossArenaModule>(true);
            float floor = new SerializedObject(arena).FindProperty("floorDistance").floatValue;

            Set(arena, "laserHeight", 4.5f - floor);
            Set(root.GetComponentInChildren<DroneChargeSkill>(true), "damageStartRatio", 0.5f);
            Set(root.GetComponentInChildren<DroneSalvoSkill>(true), "preferredDistance", 3f);
            Set(root.GetComponentInChildren<DroneSalvoSkill>(true), "distanceTolerance", 0.2f);
            Set(root.GetComponentInChildren<DroneBombSkill>(true), "normalRadiusPixels", 52.5f);
            Set(root.GetComponentInChildren<DroneBombSkill>(true), "lastStandRadiusPixels", 63f);
            PrefabUtility.SaveAsPrefabAsset(root, path);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { altitude = 4.5f, rearSafeRatio = 0.5f, salvoDistance = 3f, bombRadii = new[] { 1.875f, 2.25f } };
    }

    private static void Set(UnityEngine.Object target, string field, float value)
    {
        var data = new SerializedObject(target);

        data.FindProperty(field).floatValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }
}
