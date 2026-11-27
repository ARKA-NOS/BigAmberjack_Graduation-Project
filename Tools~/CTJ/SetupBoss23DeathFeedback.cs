using System;
using _Scripts.Agent.Enemies;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

public static class SetupBoss23DeathFeedback
{
    private const string Effects = "Assets/_Prefabs/Enemies/Effects/";
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";

    private static void SetReference(Object target, string name, Object value)
    {
        var data = new SerializedObject(target);
        data.FindProperty(name).objectReferenceValue = value;
        data.ApplyModifiedPropertiesWithoutUndo();
    }

    public static object Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        string viewPath = Effects + "EnemyDeathScreenFlash.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(viewPath) == null)
        {
            var root = new GameObject("EnemyDeathScreenFlash", typeof(RectTransform), typeof(Canvas), typeof(CanvasGroup));
            try
            {
                root.layer = LayerMask.NameToLayer("UI");
                var canvas = root.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 500;
                var group = root.GetComponent<CanvasGroup>();
                group.interactable = false; group.blocksRaycasts = false;
                var child = new GameObject("White Flash", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                child.layer = root.layer;
                child.transform.SetParent(root.transform, false);
                var rect = child.GetComponent<RectTransform>();
                rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
                rect.offsetMin = rect.offsetMax = Vector2.zero;
                var image = child.GetComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0.35f);
                image.raycastTarget = false;
                var view = root.AddComponent<EnemyDeathScreenFlash>();
                SetReference(view, "image", image);
                PrefabUtility.SaveAsPrefabAsset(root, viewPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        string flashPath = Effects + "EnemyDeathScreenFlashModule.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(flashPath) == null)
        {
            var root = new GameObject("DeathScreenFlashModule");
            try
            {
                var module = root.AddComponent<EnemyDeathScreenFlashModule>();
                SetReference(module, "flashPrefab", AssetDatabase.LoadAssetAtPath<EnemyDeathScreenFlash>(viewPath));
                PrefabUtility.SaveAsPrefabAsset(root, flashPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        string slowPath = Effects + "EnemyDeathSlowMotionModule.prefab";
        if (AssetDatabase.LoadAssetAtPath<GameObject>(slowPath) == null)
        {
            var root = new GameObject("DeathSlowMotionModule");
            try
            {
                root.AddComponent<EnemyDeathSlowMotionModule>();
                PrefabUtility.SaveAsPrefabAsset(root, slowPath);
            }
            finally { Object.DestroyImmediate(root); }
        }

        var boss = PrefabUtility.LoadPrefabContents(Boss);
        try
        {
            var enemy = boss.GetComponent<EnemyBase>();
            var data = new SerializedObject(enemy);
            data.FindProperty("completeDeathAnimationBeforeReward").boolValue = true;
            data.ApplyModifiedPropertiesWithoutUndo();
            if (boss.GetComponentInChildren<EnemyDeathScreenFlashModule>(true) == null) Attach(flashPath, boss.transform);
            if (boss.GetComponentInChildren<EnemyDeathSlowMotionModule>(true) == null) Attach(slowPath, boss.transform);
            PrefabUtility.SaveAsPrefabAsset(boss, Boss);
            return new { boss = Boss, bodyType = boss.GetComponent<Rigidbody2D>().bodyType.ToString(),
                flash = viewPath, flashModule = flashPath, slowModule = slowPath };
        }
        finally { PrefabUtility.UnloadPrefabContents(boss); }
    }

    private static void Attach(string path, Transform parent)
    {
        var child = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(path), parent);
        child.transform.localPosition = Vector3.zero;
        child.transform.localRotation = Quaternion.identity;
        child.transform.localScale = Vector3.one;
    }
}
