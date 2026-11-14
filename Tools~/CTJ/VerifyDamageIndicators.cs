using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.BattleSystem;
using _Scripts.UI.HitFeedback;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyDamageIndicators
{
    private const string Effects = "Assets/_Prefabs/Enemies/Effects/";
    private const string EnemyPath = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
    private const string OriginalScene = "CTJ.DamageIndicators.OriginalScene";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool success, string message) { if (!success) throw new Exception(message); }
    private static EnemyDamageIndicatorPopup[] Popups() => Object.FindObjectsByType<EnemyDamageIndicatorPopup>(FindObjectsSortMode.None);
    private static TMP_Text Label(EnemyDamageIndicatorPopup popup) => popup.GetComponentInChildren<TMP_Text>();
    private static float Damage(EnemyDamageIndicatorPopup popup) => float.Parse(Label(popup).text, CultureInfo.InvariantCulture);
    private static void Hit(EnemyBase enemy, float damage, bool critical = false) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage, IsCritical = critical }, enemy.transform.position, Vector2.right, Vector2.left);

    public static object CheckAssets()
    {
        var prefab = AssetDatabase.LoadAssetAtPath<EnemyDamageIndicatorPopup>(Effects + "EnemyDamageIndicator.prefab");
        Check(prefab != null && Label(prefab) != null && Label(prefab).font != null, "Popup / text / font missing");
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path =>
                AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null).ToArray();
        foreach (var path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var modules = root.GetComponentsInChildren<EnemyDamageIndicatorModule>(true);
            Check(modules.Length == 1, "One module required: " + path);
            var binder = modules[0].GetComponent<EnemyDamageIndicatorBinder>();
            Check(binder != null, "Binder missing: " + path);
            var moduleData = new SerializedObject(modules[0]);
            var binderData = new SerializedObject(binder);
            Check(moduleData.FindProperty("_viewModel").objectReferenceValue != null &&
                moduleData.FindProperty("_viewModel").objectReferenceValue == binderData.FindProperty("bindData").objectReferenceValue,
                "ViewModel mismatch: " + path);
            Check(binderData.FindProperty("_popupPrefab").objectReferenceValue == prefab, "Popup mismatch: " + path);
        }
        return new { passed = true, enemies = paths.Length, font = Label(prefab).font.name };
    }

    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying, "Stop play first");
        Check(SceneManager.sceneCount == 1, "Requires one saved scene");
        Scene scene = SceneManager.GetActiveScene();
        Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save the current scene yourself first");
        string path = scene.path;
        SessionState.SetString(OriginalScene, path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return path;
    }

    private static EnemyBase Spawn(Vector3 position)
    {
        var root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath), position, Quaternion.identity);
        var enemy = root.GetComponent<EnemyBase>();
        enemy.GetComponent<Rigidbody2D>().simulated = false;
        foreach (var module in root.GetComponentsInChildren<EnemyParticleModule>()) module.enabled = false;
        foreach (var module in root.GetComponentsInChildren<EnemyHitReactionModule>()) module.enabled = false;
        foreach (var module in root.GetComponentsInChildren<EnemyDeathModule>()) module.enabled = false;
        foreach (var module in root.GetComponentsInChildren<EnemyCurrencyRewardModule>()) module.enabled = false;
        return enemy;
    }

    public static async Task<object> RunPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        float originalTimeScale = Time.timeScale;
        float originalMaximumDelta = Time.maximumDeltaTime;
        Time.timeScale = 1f;
        Time.maximumDeltaTime = 0.025f;
        EnemyBase a = null, b = null;
        try
        {
            a = Spawn(Vector3.zero);
            b = Spawn(Vector3.right * 4f);
            var moduleA = a.GetModule<EnemyDamageIndicatorModule>();
            var moduleB = b.GetModule<EnemyDamageIndicatorModule>();
            var healthA = a.GetModule<INewHealthModule>();
            var healthB = b.GetModule<INewHealthModule>();
            float hpA = healthA.CurrentHealth, hpB = healthB.CurrentHealth;

            Hit(a, 2f);
            Hit(b, 2f, true);
            var first = Popups();
            Check(first.Length == 2, "Multiple enemies duplicated or lost one hit");
            var normal = first.Single(popup => Label(popup).color == Color.white);
            var critical = first.Single(popup => Label(popup).color != Color.white);
            Check(Damage(normal) == 2f && Damage(critical) == 2f, "Displayed wrong damage");
            Check(hpA - healthA.CurrentHealth == 2f && hpB - healthB.CurrentHealth == 2f, "Critical display changed actual damage");
            Check(Label(critical).color.r > 0.9f && Label(critical).color.g > 0.7f && Label(critical).color.b < 0.3f,
                "Critical not yellow");
            float startNormal = normal.transform.localScale.x;
            float startCritical = critical.transform.localScale.x;
            Check(startCritical > startNormal, "Critical not larger");

            Hit(a, 0f);
            Hit(a, float.NaN);
            Hit(a, -1f);
            healthA.ApplyHeal(1f);
            Check(Popups().Length == 2, "Zero/invalid damage or healing spawned an indicator");
            float minimum = startNormal, rebound = 0f, maxRise = 0f, startY = normal.transform.position.y;
            bool faded = false;
            float deadline = Time.realtimeSinceStartup + 3f;
            while (normal != null && Time.realtimeSinceStartup < deadline)
            {
                float age = (float)typeof(EnemyDamageIndicatorPopup).GetField("_age", Private).GetValue(normal);
                float scale = normal.transform.localScale.x;
                if (age < 0.16f) minimum = Mathf.Min(minimum, scale);
                if (age >= 0.16f && age < 0.3f) rebound = Mathf.Max(rebound, scale);
                maxRise = Mathf.Max(maxRise, normal.transform.position.y - startY);
                if (age > 0.45f && normal.GetComponent<CanvasGroup>().alpha < 0.85f) faded = true;
                await Awaitable.NextFrameAsync();
            }
            Check(normal == null && minimum < startNormal * 0.7f && rebound > minimum * 1.2f && maxRise > 0.8f && faded,
                $"Scale lifecycle failed: destroyed={normal == null}, start={startNormal}, min={minimum}, rebound={rebound}, rise={maxRise}, faded={faded}, capture={Time.captureFramerate}");
            await Task.Delay(180);
            Check(Popups().Length == 0, "Expired indicators remain");

            moduleA.ShowDamage(11f, false, Vector3.zero);
            moduleA.ShowDamage(12f, false, Vector3.zero);
            moduleB.ShowDamage(13f, true, Vector3.right);
            Check(Popups().Length == 3 && Popups().Select(Damage).OrderBy(x => x).SequenceEqual(new[] { 11f, 12f, 13f }),
                "Same-frame requests were overwritten or duplicated");
            foreach (var popup in Popups()) Object.Destroy(popup.gameObject);
            await Task.Delay(40);

            // 런타임 인스턴스만 방어 100으로 설정해 실제 감소량과 표시 수치를 비교합니다.
            var healthComponent = a.GetComponentInChildren<NewHealthModule>();
            var defenseData = (StatData)typeof(NewHealthModule).GetField("defenseStatData", Private).GetValue(healthComponent);
            typeof(NewHealthModule).GetField("_defenseStat", Private).SetValue(healthComponent, new Stat(defenseData, 100f));
            float beforeDefense = healthA.CurrentHealth;
            Hit(a, 10f);
            Check(Mathf.Approximately(beforeDefense - healthA.CurrentHealth, 5f) && Damage(Popups().Single()) == 5f,
                "Indicator ignored defense");
            Object.Destroy(Popups().Single().gameObject);
            await Task.Delay(40);

            moduleB.enabled = false;
            Hit(b, 1f);
            Check(Popups().Length == 0, "Disabled module emitted text");
            moduleB.enabled = true;
            Hit(b, 1f);
            Check(Popups().Length == 1, "Re-enabled module duplicated or missed a subscription");
            Object.Destroy(Popups().Single().gameObject);
            await Task.Delay(40);

            float remaining = healthB.CurrentHealth;
            Hit(b, 99999f, true);
            Check(b.IsDead && Popups().Length == 1 && Mathf.Approximately(Damage(Popups().Single()), remaining),
                "Lethal hit missing / wrong remaining HP");
            var finalPopup = Popups().Single();
            Check(finalPopup.transform.parent == null, "Popup is attached to dying enemy");
            Object.Destroy(b.gameObject);
            await Task.Delay(40);
            Check(finalPopup != null, "Enemy destruction removed final popup");
            await Task.Delay(1000);
            Check(Popups().Length == 0, "Final popup leaked");
            return new { passed = true, initialNormalScale = startNormal, initialCriticalScale = startCritical,
                minimum, rebound, maxRise, faded, sameFrameHits = 3, reducedDamage = 5,
                criticalDoesNotMultiplyDamage = true, lethalSurvivesEnemy = true, noRemainingPopups = true };
        }
        finally
        {
            Time.timeScale = originalTimeScale;
            Time.maximumDeltaTime = originalMaximumDelta;
            if (a != null) Object.Destroy(a.gameObject);
            if (b != null) Object.Destroy(b.gameObject);
            foreach (var popup in Popups()) Object.Destroy(popup.gameObject);
        }
    }

    public static async Task<object> CaptureStages()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        var objects = new List<GameObject>();
        RenderTexture rt = new RenderTexture(1000, 500, 24);
        Texture2D image = null;
        var previous = RenderTexture.active;
        try
        {
            var cameraRoot = new GameObject("Indicator preview camera");
            objects.Add(cameraRoot);
            var camera = cameraRoot.AddComponent<Camera>();
            camera.transform.position = new Vector3(0, 0.1f, -10);
            camera.orthographic = true;
            camera.orthographicSize = 1.8f;
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.085f);
            camera.targetTexture = rt;
            var prefab = AssetDatabase.LoadAssetAtPath<EnemyDamageIndicatorPopup>(Effects + "EnemyDamageIndicator.prefab");
            float[] stages = { 0f, 0.12f, 0.26f, 0.72f };
            for (int row = 0; row < 2; row++)
                for (int column = 0; column < stages.Length; column++)
                {
                    var popup = Object.Instantiate(prefab);
                    objects.Add(popup.gameObject);
                    typeof(EnemyDamageIndicatorPopup).GetField("_horizontalSpread", Private).SetValue(popup, 0f);
                    typeof(EnemyDamageIndicatorPopup).GetField("_horizontalDrift", Private).SetValue(popup, 0f);
                    popup.Show(new DamageIndicatorData(1, 24f, row == 1, new Vector3(-2.7f + column * 1.8f, 0.6f - row * 1.5f, 0f)));
                    typeof(EnemyDamageIndicatorPopup).GetMethod("ApplyFrame", Private).Invoke(popup, new object[] { stages[column] });
                    popup.enabled = false;
                }
            Canvas.ForceUpdateCanvases();
            await Task.Delay(100);
            camera.Render();
            RenderTexture.active = rt;
            image = new Texture2D(1000, 500, TextureFormat.RGB24, false);
            image.ReadPixels(new Rect(0, 0, 1000, 500), 0, 0);
            image.Apply();
            string path = "Tools~/CTJ/DamageIndicatorPreview.png";
            File.WriteAllBytes(path, image.EncodeToPNG());
            return new { preview = path, columns = "start / shrink / rebound / rise+fade", rows = "normal / critical" };
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var obj in objects) if (obj != null) Object.Destroy(obj);
            if (image != null) Object.Destroy(image);
            rt.Release(); Object.Destroy(rt);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;
    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(OriginalScene, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(OriginalScene);
        return path;
    }
}
