using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using CTJ.UI;
using DevLib.BattleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using SDW.Scripts.RunEnd;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyBossHealthBar
    {
        private const string BossPath = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        private const string BarPath = "Assets/CTJ/Prototypes/Bosses/BossHealthBarCanvas.prefab";
        private const string ExistingBar = "Assets/CTJ/Prototypes/Prefabs/EnemyTemporaryStatusUI.prefab";

        public static object Configure()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            var font = new[] { "Assets/Lrw/Asset/UI/TitleUI/Font/AmberjackPixel.ttf", "Assets/SDW/Font/NanumSquareRoundB.ttf" }
                .Select(AssetDatabase.LoadAssetAtPath<Font>).FirstOrDefault(f => f != null && "눈깔 보스무적".All(c => c == ' ' || f.HasCharacter(c)));
            Check(font != null, "Existing Korean font required");
            GameObject root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(ExistingBar));
            try
            {
                root.name = "BossHealthBarCanvas";
                Object.DestroyImmediate(root.GetComponent<EnemyTemporaryStatusUI>());
                Object.DestroyImmediate(root.transform.Find("ReactionBackground").gameObject);
                var canvas = root.GetComponent<Canvas>(); canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.enabled = true; canvas.sortingOrder = 100;
                var rootRect = root.GetComponent<RectTransform>(); rootRect.localScale = Vector3.one;
                rootRect.localRotation = Quaternion.identity; rootRect.anchoredPosition3D = Vector3.zero;
                rootRect.anchorMin = Vector2.zero; rootRect.anchorMax = Vector2.one; rootRect.sizeDelta = Vector2.zero;
                var scaler = root.AddComponent<CanvasScaler>(); scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f); scaler.matchWidthOrHeight = 0.5f;

                var panel = Rect("Panel", root.transform, new Vector2(0.25f, 1f), new Vector2(0.75f, 1f),
                    new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(0f, 88f));
                var frame = Rect("Frame", panel, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(0.5f, 1f), new Vector2(0f, -34f), new Vector2(0f, 26f));
                var background = root.transform.Find("HealthBackground").GetComponent<Image>();
                // 흰 프레임이 배경을 통해 비치지 않도록 내부만 불투명하게 유지합니다.
                Color backgroundColor = background.color; backgroundColor.a = 1f; background.color = backgroundColor;
                var fill = background.transform.Find("HealthFill").GetComponent<Image>();
                var border = frame.gameObject.AddComponent<Image>(); border.sprite = background.sprite;
                border.color = new Color(0.25f, 0.28f, 0.32f, 1f); border.raycastTarget = false;
                background.transform.SetParent(frame, false); Stretch(background.rectTransform, 3f);
                Stretch(fill.rectTransform, 0f); fill.fillAmount = 1f;

                var nameRect = Rect("BossName", panel, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(0.5f, 1f), Vector2.zero, new Vector2(0f, 28f));
                var nameLabel = Label(nameRect, font, "눈깔 보스", 24, TextAnchor.UpperCenter);
                nameLabel.resizeTextForBestFit = true; nameLabel.resizeTextMinSize = 14; nameLabel.resizeTextMaxSize = 24;
                var statusRect = Rect("InvulnerableLabel", panel, new Vector2(0f, 1f), Vector2.one,
                    new Vector2(1f, 1f), new Vector2(0f, -64f), new Vector2(0f, 24f));
                var statusLabel = Label(statusRect, font, "무적", 18, TextAnchor.UpperRight);
                statusRect.gameObject.SetActive(false);
                var ui = root.AddComponent<BossHealthBarUI>(); var uiSettings = new SerializedObject(ui);
                uiSettings.FindProperty("healthFill").objectReferenceValue = fill;
                uiSettings.FindProperty("border").objectReferenceValue = border;
                uiSettings.FindProperty("nameLabel").objectReferenceValue = nameLabel;
                uiSettings.FindProperty("invulnerableLabel").objectReferenceValue = statusLabel;
                uiSettings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, BarPath);
            }
            finally { Object.DestroyImmediate(root); }

            GameObject boss = PrefabUtility.LoadPrefabContents(BossPath);
            try
            {
                var previous = boss.GetComponentInChildren<EnemyTemporaryStatusUI>(true);
                if (previous != null) Object.DestroyImmediate(previous.gameObject);
                var module = boss.GetComponentInChildren<BossHealthBarModule>(true);
                if (module == null)
                {
                    var go = new GameObject("HealthBarModule"); go.transform.SetParent(boss.transform, false);
                    module = go.AddComponent<BossHealthBarModule>();
                }
                var settings = new SerializedObject(module);
                settings.FindProperty("healthBarPrefab").objectReferenceValue = AssetDatabase.LoadAssetAtPath<BossHealthBarUI>(BarPath);
                settings.ApplyModifiedPropertiesWithoutUndo();
                settings = new SerializedObject(boss.GetComponent<RobotEyeBoss>());
                settings.FindProperty("displayName").stringValue = "눈깔 보스"; settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(boss, BossPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(boss); }
            return new { font = AssetDatabase.GetAssetPath(font), validation = RunEdit() };
        }

        public static object RunEdit()
        {
            var boss = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath);
            var ui = AssetDatabase.LoadAssetAtPath<BossHealthBarUI>(BarPath);
            Check(boss.GetComponentInChildren<BossHealthBarModule>() != null && boss.GetComponentInChildren<EnemyTemporaryStatusUI>(true) == null,
                "Replace boss world health/stagger with optional HUD module");
            Check(ui.GetComponent<Canvas>().renderMode == RenderMode.ScreenSpaceOverlay && ui.transform.Find("ReactionBackground") == null,
                "Screen overlay without stagger indicator");
            var panel = ui.transform.Find("Panel").GetComponent<RectTransform>();
            Check(panel.anchorMin == new Vector2(0.25f, 1f) && panel.anchorMax == new Vector2(0.75f, 1f) && panel.sizeDelta.x == 0f,
                "Half-screen width / top center anchors");
            foreach (var prefab in new[] { boss.gameObject, ui.gameObject })
            {
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
                foreach (Component component in prefab.GetComponentsInChildren<Component>(true))
                {
                    var p = new SerializedObject(component).GetIterator();
                    while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference)
                        Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0, "Missing reference: " + component.name + "/" + p.propertyPath);
                }
            }
            foreach (Graphic graphic in ui.GetComponentsInChildren<Graphic>(true)) Check(!graphic.raycastTarget, "HUD must not block clicks");
            Check(ui.GetComponentsInChildren<Text>(true).All(t => t.font != null && t.text.All(c => c == ' ' || t.font.HasCharacter(c))), "Korean labels supported");
            return new { passed = true, width = "50%", bossName = boss.DisplayName, normalEnemyBarPreserved = AssetDatabase.LoadAssetAtPath<EnemyTemporaryStatusUI>(ExistingBar) != null };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            var stage = new GameObject("Boss health HUD verification"); stage.SetActive(false);
            try
            {
                var cameraObject = new GameObject("Test gameplay camera"); cameraObject.transform.SetParent(stage.transform);
                cameraObject.tag = "MainCamera"; cameraObject.transform.position = new Vector3(7000f, 4f, -10f);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 6f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f);
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath), new Vector3(7000f, 4f), Quaternion.identity, stage.transform);
                SetFloat(boss, "firstVolleyDelay", 999f);
                boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                var target = new GameObject("Test target"); target.transform.SetParent(stage.transform); target.transform.position = new Vector3(7004f, 4f);
                target.AddComponent<BoxCollider2D>().isTrigger = true; target.AddComponent<EnemyDamageProbe>();
                stage.SetActive(true); boss.SetTarget(target.transform);
                await Until(() => boss.CurrentState == RobotEyeBossState.Waiting);
                var module = boss.GetModule<BossHealthBarModule>();
                var ui = OnlyUI(); var canvas = ui.GetComponent<Canvas>();
                var health = boss.GetModule<INewHealthModule>();
                var fill = Find<Image>(ui, "HealthFill"); var border = Find<Image>(ui, "Frame");
                var label = Find<Text>(ui, "InvulnerableLabel"); var name = Find<Text>(ui, "BossName");
                Check(canvas.renderMode == RenderMode.ScreenSpaceOverlay && ui.transform.parent == null && canvas.enabled, "Root overlay visible");
                Check(ui.gameObject.scene == boss.gameObject.scene && name.text == boss.DisplayName, "Owner scene / boss name");
                Check(fill.fillAmount == 1f && border.color != Color.white && !label.gameObject.activeSelf, "Normal full health");
                CheckLayout(ui, null, Screen.width, Screen.height);
                var firstCorners = Corners(border.rectTransform, null);
                boss.GetModule<BossFlightModule>().Place(boss.transform.position + new Vector3(2f, 2f));
                boss.transform.localScale = Vector3.one * 1.5f; await Frames(2);
                Check(firstCorners.Zip(Corners(border.rectTransform, null), (a, b) => Vector2.Distance(a, b) < 0.1f).All(x => x), "Boss motion / scale cannot move HUD");
                boss.transform.localScale = Vector3.one;
                Hit(boss, 250f); await Frames(2); Check(Mathf.Abs(fill.fillAmount - 0.9f) < 0.001f, "Damage event updates fill");
                health.ApplyHeal(125f); await Frames(2); Check(Mathf.Abs(fill.fillAmount - 0.95f) < 0.001f, "Healing event updates fill");
                var bossSettings = new SerializedObject(boss); bossSettings.FindProperty("displayName").stringValue = "눈깔 보스 테스트";
                bossSettings.ApplyModifiedPropertiesWithoutUndo(); await Frames(2);
                Check(name.text == "눈깔 보스 테스트", "Editable name refreshes");
                bossSettings.Update(); bossSettings.FindProperty("displayName").stringValue = "눈깔 보스"; bossSettings.ApplyModifiedPropertiesWithoutUndo();

                module.enabled = false; await Frames(2); Check(AllUI().Length == 0, "Disable module removes HUD");
                module.enabled = true; await Frames(2); ui = OnlyUI(); canvas = ui.GetComponent<Canvas>();
                fill = Find<Image>(ui, "HealthFill"); border = Find<Image>(ui, "Frame"); label = Find<Text>(ui, "InvulnerableLabel");
                Check(Mathf.Abs(fill.fillAmount - 0.95f) < 0.001f, "Reenabled module restores current HP");
                ui.enabled = false; Hit(boss, 125f); await Frames(2); Check(!canvas.enabled, "Disabled UI hides and unsubscribes");
                ui.enabled = true; await Frames(2); Check(Mathf.Abs(fill.fillAmount - 0.9f) < 0.001f, "Reenabled UI reads latest HP");
                string normal = await Render(ui, stage, "BossHealth_Normal.png", 1920, 1080);
                string compact = await Render(ui, stage, "BossHealth_720p.png", 1280, 720);
                string standard = await Render(ui, stage, "BossHealth_4by3.png", 1024, 768);

                Hit(boss, health.CurrentHealth - 1000f); await Frames(2);
                Check(boss.IsInvulnerable && border.color == Color.white && label.gameObject.activeSelf && label.text == "무적", "Invulnerable border and text");
                float hp = health.CurrentHealth; Hit(boss, 250f); await Frames(2);
                Check(health.CurrentHealth == hp && Mathf.Abs(fill.fillAmount - 0.4f) < 0.001f, "Invulnerable damage does not alter bar");
                string invulnerable = await Render(ui, stage, "BossHealth_Invulnerable.png", 1920, 1080);
                await Until(() => boss.HasFinishedLaserPhase, 40);
                await Frames(2);
                Check(!boss.IsInvulnerable && border.color != Color.white && !label.gameObject.activeSelf, "Normal border restored without HP change");
                boss.gameObject.SetActive(false); await Frames(2); Check(AllUI().Length == 0, "Inactive boss leaves no HUD");
                boss.gameObject.SetActive(true); await Frames(2); ui = OnlyUI();
                Hit(boss, 99999f);
                Check(boss.IsDead && !ui.gameObject.activeSelf, "Death event disables HUD synchronously");
                await Frames(1); Check(ui == null || !ui.gameObject.activeInHierarchy, "Death removes HUD");
                await Until(() => boss == null); await Frames(2); Check(AllUI().Length == 0, "Destroyed boss leaves no HUD");
                await CheckSceneUnload();
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, normal, compact, standard, invulnerable, errors,
                    verified = "50% layout / Korean text / HP and heal / real invulnerability / reenable / death / scene unload" };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); Application.logMessageReceived -= capture; }
        }

        public static async Task<object> RunDeathEvents()
        {
            RequirePlay();
            var errors = new List<string>();
            var order = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            var stage = new GameObject("HUD death event verification");
            int subscribersBefore = RunEndSubscriberCount();
            bool healthDeadInsideEvent = true;
            try
            {
                var boss = SpawnLifetimeBoss(stage);
                await Frames(2);
                var module = boss.GetModule<BossHealthBarModule>();
                var ui = OnlyUI();
                Check(ui.GetComponent<Canvas>().enabled, "Living boss HUD visible");
                Check(RunEndSubscriberCount() == subscribersBefore + 1, "One run-end subscription");
                module.enabled = false; await Frames(2);
                Check(AllUI().Length == 0 && RunEndSubscriberCount() == subscribersBefore, "Disable removes view and subscription");
                module.enabled = true; await Frames(2); ui = OnlyUI();
                Check(RunEndSubscriberCount() == subscribersBefore + 1, "Reenable subscribes once");
                var health = boss.GetModule<INewHealthModule>();
                HealthChanged onHealth = (current, delta, max) => { if (current <= 0f) order.Add("OnHealthChanged(0)"); };
                Action onDead = () =>
                {
                    order.Add("OnDead");
                    healthDeadInsideEvent = health.IsDead;
                    Check(!ui.gameObject.activeSelf && !ui.GetComponent<Canvas>().enabled, "HUD disabled inside death event, before next frame");
                };
                health.OnHealthChanged += onHealth; health.OnDead += onDead;
                try { Hit(boss, 99999f); }
                finally { health.OnHealthChanged -= onHealth; health.OnDead -= onDead; }
                Check(order.SequenceEqual(new[] { "OnHealthChanged(0)", "OnDead" }) && !healthDeadInsideEvent && health.IsDead,
                    "Shared module raises OnDead before setting IsDead");
                await Frames(2);
                Check(boss != null && boss.IsDead && AllUI().Length == 0, "HUD removed before delayed boss despawn");
                module.enabled = false; module.enabled = true; await Frames(2);
                Check(AllUI().Length == 0, "Dead boss cannot recreate HUD");
                Object.Destroy(boss.gameObject); await Frames(2);
                Check(RunEndSubscriberCount() == subscribersBefore, "Destroyed boss releases static event");

                foreach (RunEndReason reason in new[] { RunEndReason.PlayerDead, RunEndReason.BossCleared })
                {
                    boss = SpawnLifetimeBoss(stage); await Frames(2); module = boss.GetModule<BossHealthBarModule>(); ui = OnlyUI();
                    Check(ui.GetComponent<Canvas>().enabled, "New owner starts with fresh HUD");
                    RunEndEvents.RaiseRunEnded(reason);
                    Check(!boss.IsDead && !ui.gameObject.activeSelf && !ui.GetComponent<Canvas>().enabled, "Run end immediately hides living boss HUD: " + reason);
                    RunEndEvents.RaiseRunEnded(reason);
                    await Frames(2); Check(AllUI().Length == 0, "Result screen has no HUD after event: " + reason);
                    module.enabled = false; module.enabled = true;
                    boss.gameObject.SetActive(false); boss.gameObject.SetActive(true);
                    await Frames(2); Check(AllUI().Length == 0, "Run-ended HUD does not reappear");
                    Object.Destroy(boss.gameObject); await Frames(2);
                    Check(RunEndSubscriberCount() == subscribersBefore, "No static listener leak: " + reason);
                }

                boss = SpawnLifetimeBoss(stage);
                RunEndEvents.RaiseRunEnded(RunEndReason.PlayerDead);
                await Frames(2); Check(AllUI().Length == 0, "Run end before Start prevents late HUD creation");
                Object.Destroy(boss.gameObject); await Frames(2);
                await CheckSceneUnload();
                Check(RunEndSubscriberCount() == subscribersBefore, "Scene unload releases subscriptions");
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, order, healthDeadInsideEvent, errors,
                    verified = "same-event death hide / delayed despawn / living boss at both run endings / repeated event / reenable / next owner / event before Start / scene unload" };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); Application.logMessageReceived -= capture; }
        }

        private static RobotEyeBoss SpawnLifetimeBoss(GameObject stage)
        {
            stage.SetActive(false);
            var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath), new Vector3(7400f, 4f), Quaternion.identity, stage.transform);
            boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            SetFloat(boss, "deathDespawnDelay", 5f);
            stage.SetActive(true);
            boss.SetTarget(null);
            return boss;
        }

        private static int RunEndSubscriberCount()
        {
            var events = (System.Collections.IDictionary)typeof(EventBus<RunEndedEvent>).GetField("Events",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Static).GetValue(null);
            int count = 0;
            foreach (Delegate callback in events.Values) count += callback.GetInvocationList().Length;
            return count;
        }

        private static async Task CheckSceneUnload()
        {
            Scene scene = SceneManager.CreateScene("CTJ isolated HUD lifetime");
            var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath));
            SceneManager.MoveGameObjectToScene(boss.gameObject, scene);
            boss.SetTarget(null); await Frames(2);
            Check(OnlyUI().gameObject.scene == scene, "HUD moves into owner additive scene");
            AsyncOperation unloading = SceneManager.UnloadSceneAsync(scene);
            await Until(() => unloading.isDone); await Frames(2);
            Check(AllUI().Length == 0, "Scene unload cleans up HUD");
        }

        private static async Task<string> Render(BossHealthBarUI ui, GameObject stage, string filename, int width, int height)
        {
            var go = new GameObject("HUD preview camera"); go.transform.SetParent(stage.transform); go.transform.position = Vector3.back * 10f;
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 5f;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
            camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
            var rt = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
            var canvas = ui.GetComponent<Canvas>();
            var children = ui.GetComponentsInChildren<Transform>(true); int[] layers = children.Select(c => c.gameObject.layer).ToArray();
            foreach (Transform child in children) child.gameObject.layer = 31;
            canvas.renderMode = RenderMode.ScreenSpaceCamera; canvas.worldCamera = camera; canvas.planeDistance = 1f;
            var texture = new Texture2D(width, height, TextureFormat.RGBA32, false); RenderTexture previous = RenderTexture.active;
            try
            {
                await Frames(4); Canvas.ForceUpdateCanvases(); CheckLayout(ui, camera, width, height);
                RenderTexture.active = rt; texture.ReadPixels(new UnityEngine.Rect(0, 0, width, height), 0, 0); texture.Apply();
                string path = Application.dataPath + "/CTJ/Tools~/" + filename; File.WriteAllBytes(path, texture.EncodeToPNG()); return path;
            }
            finally
            {
                RenderTexture.active = previous; canvas.renderMode = RenderMode.ScreenSpaceOverlay; canvas.worldCamera = null;
                for (int i = 0; i < children.Length; i++) children[i].gameObject.layer = layers[i];
                Object.Destroy(texture); rt.Release(); Object.Destroy(rt); Object.Destroy(go);
            }
        }

        private static void CheckLayout(BossHealthBarUI ui, Camera camera, float width, float height)
        {
            Vector2[] bar = Corners(Find<Image>(ui, "Frame").rectTransform, camera);
            Vector2[] title = Corners(Find<Text>(ui, "BossName").rectTransform, camera);
            Vector2[] invulnerable = Corners(Find<Text>(ui, "InvulnerableLabel").rectTransform, camera);
            Check(Mathf.Abs(bar[3].x - bar[0].x - width * 0.5f) < 1f && Mathf.Abs((bar[0].x + bar[3].x) * 0.5f - width * 0.5f) < 1f,
                "Half-screen width centered");
            Check(bar[1].y < height && bar[0].y > height * 0.8f && title[0].y > bar[1].y, "Title above bar at top");
            Check(invulnerable[1].y < bar[0].y && Mathf.Abs(invulnerable[2].x - bar[2].x) < 1f &&
                Find<Text>(ui, "InvulnerableLabel").alignment == TextAnchor.UpperRight, "Invulnerable label below/right");
        }
        private static Vector2[] Corners(RectTransform rect, Camera camera)
        {
            var corners = new Vector3[4]; rect.GetWorldCorners(corners); return corners.Select(c => RectTransformUtility.WorldToScreenPoint(camera, c)).ToArray();
        }
        private static T Find<T>(Component root, string name) where T : Component => root.GetComponentsInChildren<T>(true).Single(c => c.name == name);
        private static BossHealthBarUI[] AllUI() => Object.FindObjectsByType<BossHealthBarUI>(FindObjectsInactive.Include, FindObjectsSortMode.None);
        private static BossHealthBarUI OnlyUI() { var all = AllUI(); Check(all.Length == 1, "Exactly one HUD, actual " + all.Length); return all[0]; }
        private static void Hit(RobotEyeBoss boss, float damage) => boss.ApplyDamage(new DamageData { DamageAmount = damage }, boss.transform.position, Vector2.left, Vector2.right);
        private static void SetFloat(Object obj, string field, float value) { var so = new SerializedObject(obj); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo(); }
        private static async Task Frames(int count) { int end = Time.frameCount + count; await Until(() => Time.frameCount >= end, 5); }
        private static async Task Until(Func<bool> condition, int seconds = 12)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Verification timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play required");

        private static RectTransform Rect(string name, Transform parent, Vector2 min, Vector2 max, Vector2 pivot, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name, typeof(RectTransform)); go.layer = 5;
            var rect = go.GetComponent<RectTransform>(); rect.SetParent(parent, false);
            rect.anchorMin = min; rect.anchorMax = max; rect.pivot = pivot; rect.anchoredPosition = position; rect.sizeDelta = size;
            return rect;
        }
        private static void Stretch(RectTransform rect, float inset)
        {
            rect.anchorMin = Vector2.zero; rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.one * inset; rect.offsetMax = Vector2.one * -inset;
        }
        private static Text Label(RectTransform rect, Font font, string text, int size, TextAnchor alignment)
        {
            var label = rect.gameObject.AddComponent<Text>(); label.font = font; label.text = text; label.fontSize = size;
            label.alignment = alignment; label.color = Color.white; label.raycastTarget = false; label.supportRichText = false;
            return label;
        }
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
