using System;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.UI.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SetupBoss23ChargeWarning
{
    private const string Folder = "Assets/_Prefabs/Enemies/Bosses/Enemy23/";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var bomb = AssetDatabase.LoadAssetAtPath<GameObject>(Folder + "Boss23_TargetBombWarning.prefab")
            .GetComponent<DroneBombTargetGraphic>();
        var bombData = new SerializedObject(bomb);
        Color edgeColor = bomb.color;
        Color fillColor = edgeColor;

        fillColor.a *= bombData.FindProperty("fillOpacity").floatValue;
        float thickness = bombData.FindProperty("lineWidth").floatValue;
        var warning = new GameObject("Boss23_ChargeWarning", typeof(RectTransform), typeof(Canvas), typeof(AttackTelegraphUI));
        GameObject savedWarning;

        try
        {
            var canvas = warning.GetComponent<Canvas>();

            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingLayerID = bomb.GetComponent<Canvas>().sortingLayerID;
            canvas.sortingOrder = bomb.GetComponent<Canvas>().sortingOrder;
            canvas.enabled = false;

            var fill = Image(warning.transform, "Fill", Vector2.zero, Vector2.one, Vector2.zero, fillColor);

            Image(warning.transform, "Top", Vector2.up, Vector2.one, new Vector2(0f, thickness), edgeColor);
            Image(warning.transform, "Bottom", Vector2.zero, Vector2.right, new Vector2(0f, thickness), edgeColor);
            Image(warning.transform, "Left", Vector2.zero, Vector2.up, new Vector2(thickness, 0f), edgeColor);
            Image(warning.transform, "Right", Vector2.right, Vector2.one, new Vector2(thickness, 0f), edgeColor);

            var data = new SerializedObject(warning.GetComponent<AttackTelegraphUI>());

            data.FindProperty("areaImage").objectReferenceValue = fill;
            data.FindProperty("areaColor").colorValue = fillColor;
            data.FindProperty("blinkDuration").floatValue = 0f;
            data.ApplyModifiedPropertiesWithoutUndo();
            savedWarning = PrefabUtility.SaveAsPrefabAsset(warning, Folder + "Boss23_ChargeWarning.prefab");
        }
        finally { Object.DestroyImmediate(warning); }

        var root = PrefabUtility.LoadPrefabContents(Folder + "Boss23_ProductionControlDrone.prefab");

        try
        {
            var boss = new SerializedObject(root.GetComponent<ProductionControlDrone>());

            boss.FindProperty("airAttackAltitude").floatValue = 4.5f;
            boss.ApplyModifiedPropertiesWithoutUndo();

            var arena = new SerializedObject(root.GetComponentInChildren<BossArenaModule>(true));

            arena.FindProperty("laserHeight").floatValue = 4.5f - arena.FindProperty("floorDistance").floatValue;
            arena.ApplyModifiedPropertiesWithoutUndo();

            var skill = new SerializedObject(root.GetComponentInChildren<DroneChargeSkill>(true));

            skill.FindProperty("telegraphPrefab").objectReferenceValue = savedWarning.GetComponent<AttackTelegraphUI>();
            skill.ApplyModifiedPropertiesWithoutUndo();
            PrefabUtility.SaveAsPrefabAsset(root, Folder + "Boss23_ProductionControlDrone.prefab");
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { airAttackAltitude = 4.5f, fillColor, edgeColor, thickness, warning = AssetDatabase.GetAssetPath(savedWarning) };
    }

    private static Image Image(Transform parent, string name, Vector2 min, Vector2 max, Vector2 size, Color color)
    {
        var node = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        var rect = node.GetComponent<RectTransform>();

        rect.SetParent(parent, false);
        rect.anchorMin = min;
        rect.anchorMax = max;
        rect.anchoredPosition = Vector2.zero;
        rect.sizeDelta = size;

        var image = node.GetComponent<Image>();

        image.color = color;
        image.raycastTarget = false;

        return image;
    }
}
