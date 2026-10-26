using System;
using System.Collections.Generic;
using System.Linq;
using CTJ.Enemies;
using CTJ.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class BuildEnemyTemporaryStatusUI
    {
        private const string UIPath = "Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab";
        private static readonly string[] Paths = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged",
            "Prefabs/Enemy01_TwistedArm", "Prefabs/Enemy02_ProjectileRobot", "Bosses/RobotEyeBoss" };

        public static object Build()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            Check(AssetDatabase.LoadMainAssetAtPath(UIPath) == null, "UI already exists; do not overwrite custom settings");
            foreach (string relative in Paths)
            {
                var asset = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + relative + ".prefab");
                Check(asset != null && asset.transform.Find("TemporaryStatusUI") == null, "Missing prefab / UI already attached: " + relative);
            }
            CreateUI();
            GameObject uiPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(UIPath);
            var results = new List<object>();
            foreach (string relative in Paths)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    EnemyHitReactionModule reaction = root.GetComponentInChildren<EnemyHitReactionModule>();
                    var untouched = root.GetComponentsInChildren<Component>(true)
                        .Where(c => c != null && !(c is Transform) && c != reaction)
                        .ToDictionary(c => c, c => EditorJsonUtility.ToJson(c));
                    bool boss = relative.StartsWith("Bosses/");
                    if (!boss) Set(reaction, "hitStunDuration", 0.3f);
                    // 텍스처의 투명 여백 대신 몸통 판정 위에 배치합니다. 런타임 판정 참조는 만들지 않습니다.
                    float top = BodyTop(root);
                    Vector2 offset = new Vector2(0f, top - root.transform.position.y + 0.25f);
                    GameObject ui = (GameObject)PrefabUtility.InstantiatePrefab(uiPrefab, root.transform);
                    ui.name = "TemporaryStatusUI";
                    Set(ui.GetComponent<EnemyTemporaryStatusUI>(), "worldOffset", offset);
                    ui.transform.position = root.transform.position + (Vector3)offset;
                    Vector3 scale = root.transform.lossyScale;
                    ui.transform.localScale = new Vector3(0.01f / scale.x, 0.01f / scale.y, 0.01f / scale.z);
                    foreach (var pair in untouched)
                        Check(EditorJsonUtility.ToJson(pair.Key) == pair.Value, "Unexpected existing component change: " + pair.Key);
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved); Check(saved, "Prefab save failed");
                    results.Add(new { prefab = relative, offset, hitStun = boss ? 0f : 0.3f, existingComponentsPreserved = true });
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return results;
        }

        // 초기 배치 보정용입니다. 수동으로 조정한 World Offset을 유지하려면 재실행하지 마세요.
        public static object AlignAboveBodies()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            var results = new List<object>();
            foreach (string relative in Paths)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var ui = root.GetComponentInChildren<EnemyTemporaryStatusUI>(); Check(ui != null, "UI missing");
                    Vector2 offset = new Vector2(0f, BodyTop(root) - root.transform.position.y + 0.25f);
                    Set(ui, "worldOffset", offset); ui.transform.position = root.transform.position + (Vector3)offset;
                    PrefabUtility.SaveAsPrefabAsset(root, path, out bool saved); Check(saved, "Save failed");
                    results.Add(new { prefab = relative, offsetY = offset.y });
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return results.ToArray();
        }

        private static float BodyTop(GameObject root)
        {
            Collider2D body = root.GetComponent<Collider2D>();
            return body != null ? body.bounds.max.y : root.GetComponentsInChildren<SpriteRenderer>(true).Max(s => s.bounds.max.y);
        }

        private static void CreateUI()
        {
            Scene preview = EditorSceneManager.NewPreviewScene();
            try
            {
                GameObject root = new GameObject("EnemyTemporaryStatusUI", typeof(RectTransform));
                SceneManager.MoveGameObjectToScene(root, preview);
                root.layer = 5;
                RectTransform rect = root.GetComponent<RectTransform>(); rect.sizeDelta = new Vector2(160f, 28f);
                rect.localScale = Vector3.one * 0.01f;
                Canvas canvas = root.AddComponent<Canvas>(); canvas.renderMode = RenderMode.WorldSpace;
                canvas.overrideSorting = true; canvas.sortingOrder = 50;
                EnemyTemporaryStatusUI display = root.AddComponent<EnemyTemporaryStatusUI>();
                Sprite square = AssetDatabase.LoadAllAssetsAtPath("Assets/CTJ/Prototypes/Sprites/TestWhite.png").OfType<Sprite>().First();
                Image hpBackground = Bar(root.transform, "HealthBackground", square, new Vector2(160f, 14f),
                    new Vector2(0f, 5f), new Color(0.06f, 0.07f, 0.09f, 0.9f), false);
                Image hp = Bar(hpBackground.transform, "HealthFill", square, new Vector2(158f, 12f),
                    Vector2.zero, new Color(0.2f, 0.85f, 0.3f), true);
                Image stunBackground = Bar(root.transform, "ReactionBackground", square, new Vector2(160f, 8f),
                    new Vector2(0f, -8f), new Color(0.06f, 0.07f, 0.09f, 0.9f), false);
                Image stun = Bar(stunBackground.transform, "ReactionFill", square, new Vector2(158f, 6f),
                    Vector2.zero, Color.white, true);
                stun.fillAmount = 0f;
                Set(display, "healthFill", hp); Set(display, "reactionFill", stun);
                PrefabUtility.SaveAsPrefabAsset(root, UIPath, out bool saved); Check(saved, "UI save failed");
            }
            finally { EditorSceneManager.ClosePreviewScene(preview); }
        }

        private static Image Bar(Transform parent, string name, Sprite sprite, Vector2 size, Vector2 point, Color color, bool fill)
        {
            GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            obj.layer = 5; obj.transform.SetParent(parent, false);
            Image image = obj.GetComponent<Image>(); image.sprite = sprite; image.color = color;
            image.raycastTarget = false; image.type = fill ? Image.Type.Filled : Image.Type.Simple;
            image.fillMethod = Image.FillMethod.Horizontal; image.fillOrigin = (int)Image.OriginHorizontal.Left;
            image.rectTransform.sizeDelta = size; image.rectTransform.anchoredPosition = point;
            return image;
        }

        public static object Validate()
        {
            foreach (string relative in Paths)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + relative + ".prefab");
                var displays = root.GetComponentsInChildren<EnemyTemporaryStatusUI>(true);
                Check(displays.Length == 1 && displays[0].transform.parent == root.transform, "UI must be a single removable root child");
                Check(PrefabUtility.GetPrefabAssetPathOfNearestInstanceRoot(displays[0].gameObject) == UIPath, "Reusable nested UI prefab");
                var so = new SerializedObject(displays[0]);
                Image hp = so.FindProperty("healthFill").objectReferenceValue as Image;
                Image reaction = so.FindProperty("reactionFill").objectReferenceValue as Image;
                Check(hp != null && reaction != null && hp.type == Image.Type.Filled && reaction.type == Image.Type.Filled,
                    "UI fill references");
                Check(Mathf.Approximately(reaction.rectTransform.rect.height, hp.rectTransform.rect.height * 0.5f) &&
                    Mathf.Approximately(reaction.rectTransform.rect.width, hp.rectTransform.rect.width), "Half-height white gauge");
                Check(reaction.color == Color.white && displays[0].GetComponent<Canvas>().renderMode == RenderMode.WorldSpace,
                    "White gauge / Canvas mode");
                Check(!displays[0].GetComponentsInChildren<Graphic>(true).Any(g => g.raycastTarget) &&
                    displays[0].GetComponent<GraphicRaycaster>() == null, "Noninteractive UI");
                var hit = new SerializedObject(root.GetComponentInChildren<EnemyHitReactionModule>());
                Check(Mathf.Approximately(hit.FindProperty("hitStunDuration").floatValue, relative.StartsWith("Bosses/") ? 0f : 0.3f), "Stun duration");
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                {
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script");
                    foreach (Component component in child.GetComponents<Component>())
                    {
                        var serialized = new SerializedObject(component); var p = serialized.GetIterator();
                        while (p.NextVisible(true))
                            if (p.propertyType == SerializedPropertyType.ObjectReference)
                                Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0,
                                    "Broken reference: " + component + " / " + p.propertyPath);
                    }
                }
            }
            return new { passed = true, prefabs = 5, scope = "nested removable Canvas / HP and half-height white gauge / references / stun defaults" };
        }

        private static void Set(Object target, string field, object value)
        {
            var so = new SerializedObject(target); var p = so.FindProperty(field); Check(p != null, "Missing field " + field);
            if (value is float f) p.floatValue = f;
            else if (value is Vector2 v) p.vector2Value = v;
            else if (value is Object obj) p.objectReferenceValue = obj;
            else throw new InvalidOperationException("Unsupported value");
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
