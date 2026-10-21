using System;
using System.Reflection;
using CTJ.Enemies.Bosses;
using DevLib.ModuleSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyArenaGizmos
    {
        private static int _checks;
        private static readonly MethodInfo CenterMethod = typeof(BossArenaModule).GetMethod("GetGizmoCenter", BindingFlags.Instance | BindingFlags.NonPublic);
        private static readonly MethodInfo BoundsMethod = typeof(BossArenaModule).GetMethod("GetGizmoBounds", BindingFlags.Instance | BindingFlags.NonPublic);

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit mode only");
            _checks = 0;
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("Temporary arena gizmo verification");
                SceneManager.MoveGameObjectToScene(root, preview);
                root.AddComponent<ModuleOwner>();
                root.transform.position = new Vector3(30f, 40f, 0f);
                GameObject child = new GameObject("ArenaModule");
                SceneManager.MoveGameObjectToScene(child, preview);
                child.transform.SetParent(root.transform, false);
                child.transform.localPosition = new Vector3(7f, 9f, 0f);
                BossArenaModule arena = child.AddComponent<BossArenaModule>();
                Check(Center(arena) == new Vector2(30f, 40f), "Edit mode uses owner position, not zero or module child offset");
                Bounds bounds = BoundsOf(arena);
                Check(bounds.size == new Vector3(16f, 6.5f, 0f), "Half width 8 draws total width 16");
                Check(bounds.min == new Vector3(22f, 36f, 0f) && bounds.max == new Vector3(38f, 42.5f, 0f), "Floor and laser height boundaries");
                root.transform.localScale = new Vector3(2f, 3f, 1f);
                root.transform.rotation = Quaternion.Euler(0f, 0f, 30f);
                Check(BoundsOf(arena) == bounds, "World-space bounds are not scaled or rotated with the boss");

                Set(arena, "halfWidth", 12f); Set(arena, "floorDistance", 2f); Set(arena, "laserHeight", 3f);
                Check(BoundsOf(arena).size == new Vector3(24f, 5f, 0f), "Editing dimensions updates preview immediately");
                GameObject anchor = new GameObject("Temporary arena anchor");
                SceneManager.MoveGameObjectToScene(anchor, preview);
                anchor.transform.position = new Vector3(-10f, -20f, 0f);
                Set(arena, "centerAnchor", anchor.transform);
                Check(Center(arena) == new Vector2(-10f, -20f), "Explicit anchor wins in edit mode");
                anchor.transform.position += Vector3.right;
                Check(Center(arena) == new Vector2(-9f, -20f), "Anchor edits update preview immediately");

                Set(arena, "centerAnchor", null);
                child.transform.SetParent(null);
                child.transform.position = new Vector3(5f, 6f, 0f);
                Check(Center(arena) == new Vector2(5f, 6f), "Standalone module has a safe preview before owner wiring");
                Set(arena, "halfWidth", -1f); Set(arena, "floorDistance", -1f); Set(arena, "laserHeight", -1f);
                Check(Vector3.Distance(BoundsOf(arena).size, new Vector3(2f, 0.1f, 0f)) < 0.0001f, "Preview uses the same minimum values as runtime");
                return new { passed = true, checks = _checks, scope = "edit preview / owner and anchor / dimensions / unscaled world space / standalone fallback" };
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        public static object RunPlay()
        {
            Check(EditorApplication.isPlaying && SceneManager.GetActiveScene().path == "Assets/CTJ/Prototypes/Bosses/BossPatternTest.unity", "Isolated Play only");
            _checks = 0;
            GameObject root = new GameObject("Temporary runtime arena gizmo verification");
            GameObject anchor = new GameObject("Temporary runtime arena anchor");
            root.SetActive(false);
            try
            {
                root.transform.position = new Vector3(30f, 40f, 0f);
                ModuleOwner owner = root.AddComponent<ModuleOwner>();
                GameObject child = new GameObject("ArenaModule");
                child.transform.SetParent(root.transform, false);
                BossArenaModule arena = child.AddComponent<BossArenaModule>();
                anchor.transform.position = new Vector3(10f, 20f, 0f);
                Set(arena, "centerAnchor", anchor.transform);
                root.SetActive(true);
                Vector2 originalCenter = arena.Center;
                Check(originalCenter == new Vector2(10f, 20f), "Normal initialization reads configured anchor");
                Check(Center(arena) == originalCenter && BoundsOf(arena) == arena.LaserBounds, "Gizmo matches initialized runtime bounds");
                root.transform.position += Vector3.right * 50f;
                anchor.transform.position += Vector3.up * 20f;
                Check(Center(arena) == originalCenter && BoundsOf(arena) == arena.LaserBounds, "Boss and anchor movement do not move the latched arena");
                Set(arena, "halfWidth", 10f); Set(arena, "floorDistance", 3f); Set(arena, "laserHeight", 4f);
                Check(BoundsOf(arena) == arena.LaserBounds && BoundsOf(arena).size == new Vector3(20f, 7f, 0f), "Play-mode dimension edits still match collision geometry");
                Set(arena, "centerAnchor", null);
                arena.Initialize(owner);
                Vector2 spawnCenter = arena.Center;
                root.transform.position += Vector3.up * 30f;
                Check(Center(arena) == spawnCenter && BoundsOf(arena) == arena.LaserBounds, "Unanchored spawn center also stays fixed while flying");
                return new { passed = true, checks = _checks, scope = "runtime bounds / anchor and spawn snapshot / moving boss / live dimension edits" };
            }
            finally { Object.Destroy(root); Object.Destroy(anchor); }
        }

        private static Vector2 Center(BossArenaModule arena) => (Vector2)CenterMethod.Invoke(arena, null);
        private static Bounds BoundsOf(BossArenaModule arena) => (Bounds)BoundsMethod.Invoke(arena, null);
        private static void Set(Object target, string field, object value)
        {
            SerializedObject settings = new SerializedObject(target);
            SerializedProperty property = settings.FindProperty(field);
            if (value is float number) property.floatValue = number;
            else property.objectReferenceValue = (Object)value;
            settings.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Check(bool condition, string message)
        {
            if (!condition) throw new InvalidOperationException(message);
            _checks++;
        }
    }
}
