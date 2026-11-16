using System;
using System.Collections.Generic;
using _Scripts.Agent.Enemies;
using _Scripts.UI.HitFeedback;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class SetupDamageIndicators
{
    public const string ViewModelPath = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicatorViewModel.asset";
    public const string PopupPath = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicator.prefab";
    public const string ModulePath = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicatorModule.prefab";
    private const string MaterialPath = "Assets/_Graphics/Materials/Enemy/EnemyDamageIndicatorText.mat";
    private static readonly string[] EnemyPaths =
    {
        "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy01/Enemy01_TwistedArm.prefab",
        "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy02/Enemy02_ProjectileRobot.prefab",
        "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy04/Enemy04_MechanicArcher.prefab",
        "Assets/_Prefabs/Enemies/Legacy/Test/Catto_Melee.prefab",
        "Assets/_Prefabs/Enemies/Legacy/Test/MadGhost_Ranged.prefab",
        "Assets/_Prefabs/Enemies/Legacy/Bosses/EyeBoss/RobotEyeBoss.prefab"
    };

    private static void SetReference(Object component, string name, Object value)
    {
        var serialized = new SerializedObject(component);
        var property = serialized.FindProperty(name);
        if (property == null) throw new Exception("Missing field: " + name);
        property.objectReferenceValue = value;
        serialized.ApplyModifiedPropertiesWithoutUndo();
    }

    public static object Create()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        // 최초 구성만 허용해 기존 인스펙터 설정을 덮어쓰지 않습니다.
        if (AssetDatabase.LoadAssetAtPath<Object>(ModulePath) != null)
            throw new Exception("Already configured; inspect existing assets instead of overwriting settings");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Graphics/Font/NanumSquareRoundB SDF.asset");
        if (font == null || !font.HasCharacters("0123456789."))
            font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        if (font == null) throw new Exception("Existing font missing");

        var viewModel = ScriptableObject.CreateInstance<DamageIndicatorViewModel>();
        AssetDatabase.CreateAsset(viewModel, ViewModelPath);
        var material = new Material(font.material) { name = "EnemyDamageIndicatorText" };
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.06f, 0.04f, 0.03f, 1f));
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);
        AssetDatabase.CreateAsset(material, MaterialPath);

        var root = new GameObject("EnemyDamageIndicator", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
        try
        {
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(240f, 80f);
            root.transform.localScale = Vector3.one * 0.01f;
            Canvas canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.overrideSorting = true;
            canvas.sortingOrder = 100;
            CanvasGroup group = root.GetComponent<CanvasGroup>();
            group.interactable = false;
            group.blocksRaycasts = false;
            var textObject = new GameObject("Damage", typeof(RectTransform), typeof(UnityEngine.CanvasRenderer));
            textObject.transform.SetParent(root.transform, false);
            RectTransform rect = textObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
            var text = textObject.AddComponent<TextMeshProUGUI>();
            text.font = font;
            text.fontSharedMaterial = material;
            text.fontSize = 36f;
            text.alignment = TextAlignmentOptions.Center;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Overflow;
            text.raycastTarget = false;
            text.text = "24";
            var popup = root.AddComponent<EnemyDamageIndicatorPopup>();
            SetReference(popup, "_label", text);
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        }
        finally { Object.DestroyImmediate(root); }

        root = new GameObject("DamageIndicatorModule");
        try
        {
            var module = root.AddComponent<EnemyDamageIndicatorModule>();
            var binder = root.AddComponent<EnemyDamageIndicatorBinder>();
            SetReference(module, "_viewModel", viewModel);
            SetReference(binder, "bindData", viewModel);
            SetReference(binder, "_popupPrefab", AssetDatabase.LoadAssetAtPath<EnemyDamageIndicatorPopup>(PopupPath));
            PrefabUtility.SaveAsPrefabAsset(root, ModulePath);
        }
        finally { Object.DestroyImmediate(root); }

        var modulePrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ModulePath);
        var changed = new List<string>();
        foreach (string path in EnemyPaths)
        {
            GameObject enemy = PrefabUtility.LoadPrefabContents(path);
            try
            {
                if (enemy.GetComponentInChildren<EnemyDamageIndicatorModule>(true) != null)
                    throw new Exception("Module already exists: " + path);
                var module = (GameObject)PrefabUtility.InstantiatePrefab(modulePrefab, enemy.transform);
                module.transform.localPosition = Vector3.zero;
                module.transform.localRotation = Quaternion.identity;
                module.transform.localScale = Vector3.one;
                PrefabUtility.SaveAsPrefabAsset(enemy, path);
                changed.Add(path);
            }
            finally { PrefabUtility.UnloadPrefabContents(enemy); }
        }
        return new { created = new[] { ViewModelPath, MaterialPath, PopupPath, ModulePath }, attached = changed, font = font.name };
    }

    public static object ImproveReadability()
    {
        if (EditorApplication.isPlaying) throw new Exception("Exit play mode first");
        var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/_Graphics/Font/NanumSquareRoundB SDF.asset");
        if (font == null || !font.HasCharacters("0123456789.")) throw new Exception("Existing numeric font missing");
        var material = AssetDatabase.LoadAssetAtPath<Material>(MaterialPath);
        material.shader = font.material.shader;
        material.CopyPropertiesFromMaterial(font.material);
        material.SetColor(ShaderUtilities.ID_OutlineColor, new Color(0.06f, 0.04f, 0.03f, 1f));
        material.SetFloat(ShaderUtilities.ID_OutlineWidth, 0.08f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        var root = PrefabUtility.LoadPrefabContents(PopupPath);
        try
        {
            TMP_Text text = root.GetComponentInChildren<TMP_Text>();
            text.font = font;
            text.fontSharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, PopupPath);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
        return new { font = font.name, material = MaterialPath };
    }
}
