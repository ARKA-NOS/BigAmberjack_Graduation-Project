// unity cmd run_script --file Tools~/CTJ/VerifyCoinTrail.cs --entry VerifyCoinTrail.CheckAssets
// BeginPlay -> RunPlay -> StopPlay -> RestoreScene 순서. 씬/프리팹을 저장하지 않습니다.
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Currency;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyCoinTrail
{
    private const string CoinPath = "Assets/_Prefabs/Enemies/Effects/EnemyRewardCoin.prefab";
    private const string SceneKey = "CTJ.CoinTrail.OriginalScene";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);

    public static object CheckAssets()
    {
        Check(!EditorApplication.isPlaying, "Edit Mode required");
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        Check(prefab != null && prefab.GetComponent<EnemyCoinTrail>() != null, "Coin / trail reference missing");
        Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) == 0, "Missing script");
        var settings = new SerializedObject(prefab.GetComponent<EnemyCoinTrail>());
        Material material = settings.FindProperty("trailMaterial").objectReferenceValue as Material;
        Check(material != null && material.shader != null, "Material / shader missing");
        Check(!ShaderUtil.ShaderHasError(material.shader), "Shader compile error");
        AnimationCurve profile = settings.FindProperty("widthProfile").animationCurveValue;
        Check(profile.Evaluate(1f) == 0f, "Tail must taper to one point");
        return new { passed = true, lifetime = settings.FindProperty("trailLifetime").floatValue,
            width = settings.FindProperty("trailWidth").floatValue, shader = material.shader.name };
    }
    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying, "Already playing");
        Check(SceneManager.sceneCount == 1, "Requires one saved scene; will not alter a multi-scene setup");
        Scene original = SceneManager.GetActiveScene();
        Check(!original.isDirty && !string.IsNullOrEmpty(original.path), "Save the current scene yourself first");
        string originalPath = original.path;
        SessionState.SetString(SceneKey, originalPath);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return new { original = originalPath };
    }

    public static async Task<object> RunPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        var floor = new GameObject("CTJ trail test floor");
        floor.layer = 6;
        floor.transform.position = new Vector3(0f, -1.5f, 0f);
        floor.AddComponent<BoxCollider2D>().size = new Vector2(40f, 0.3f);
        var collector = new GameObject("CTJ trail test collector");
        collector.transform.position = new Vector3(5f, 0f, 0f);
        collector.AddComponent<BoxCollider2D>().size = new Vector2(0.7f, 2f);
        var currency = new CurrencyData(null, 0);
        int payouts = 0;
        currency.OnCurrencyChanged += (current, previous) => payouts++;
        var coins = new List<EnemyRewardCoin>();
        bool scatterTrail = false, bounceTrail = false, homingTrail = false, collectionStopped = false, faded = false;
        bool toggled = false, cleared = false, noPrematureReward = true;
        int maxPoints = 0;
        float longest = 0f;
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        try
        {
            for (int i = 0; i < 4; i++)
            {
                EnemyRewardCoin coin = Object.Instantiate(prefab, new Vector3(i * 0.1f, 0.6f, 0f), Quaternion.identity).GetComponent<EnemyRewardCoin>();
                coins.Add(coin);
                if (i == 3) coin.GetComponent<EnemyCoinTrail>().enabled = false;
                Call(coin, "SetCollector", collector.transform);
                Call(coin, "Launch", currency, 3, (i + 0.5f) / 4f);
            }
            Physics2D.SyncTransforms();
            float start = Time.time;
            while (Time.time - start < 8f)
            {
                bool alive = false;
                foreach (EnemyRewardCoin coin in coins)
                {
                    if (coin == null) continue;
                    alive = true;
                    TrailRenderer trail = coin.GetComponentInChildren<TrailRenderer>();
                    if (trail == null) continue;
                    maxPoints = Mathf.Max(maxPoints, trail.positionCount);
                    float length = 0f;
                    for (int n = 1; n < trail.positionCount; n++) length += Vector3.Distance(trail.GetPosition(n - 1), trail.GetPosition(n));
                    longest = Mathf.Max(longest, length);
                    if (!coin.IsCollecting && trail.positionCount > 2)
                    {
                        if (coin.IsHoming) homingTrail = true;
                        else if (coin.BounceCount > 0) bounceTrail = true;
                        else scatterTrail = true;
                    }
                    if (coin.IsCollecting)
                    {
                        Check(!trail.emitting, "Still emitting after collection");
                        collectionStopped = true;
                        if (!trail.enabled && trail.positionCount == 0) faded = true;
                    }
                }
                if (!collectionStopped && currency.Count != 0) noPrematureReward = false;
                if (!toggled && Time.time - start > 0.18f)
                {
                    var component = coins[0].GetComponent<EnemyCoinTrail>();
                    component.enabled = false;
                    Check(coins[0].GetComponentInChildren<TrailRenderer>().positionCount == 0, "Disable retained trail");
                    component.enabled = true;
                    Check(coins[0].GetComponentsInChildren<TrailRenderer>().Length == 1, "Duplicate trail after re-enable");
                    cleared = true;
                    toggled = true;
                }
                // 도착 후에도 움직이는 대상을 재현하고 새 잔상 방출 여부를 확인합니다.
                if (collectionStopped) collector.transform.position += Vector3.right * 0.01f;
                if (!alive) break;
                await Task.Delay(16);
            }
            Check(coins.TrueForAll(coin => coin == null), "Coins failed to finish");
            Check(currency.Count == 12 && payouts == 4, "Reward lost or paid twice");
            Check(noPrematureReward && scatterTrail && bounceTrail && homingTrail && collectionStopped && faded && cleared,
                "A trail lifecycle stage was not observed");
            return new { passed = true, currency = currency.Count, payouts, scatterTrail, bounceTrail, homingTrail,
                collectionStopped, faded, cleared, noPrematureReward, maxPoints, longest };
        }
        finally
        {
            foreach (EnemyRewardCoin coin in coins) if (coin != null) Object.Destroy(coin.gameObject);
            Object.Destroy(floor);
            Object.Destroy(collector);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;
    public static async Task<object> RunVisualPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        var objects = new List<GameObject>();
        var trails = new List<TrailRenderer>();
        var rt = new RenderTexture(960, 540, 24);
        Texture2D capture = null;
        var previous = RenderTexture.active;
        try
        {
            var camGo = new GameObject("CTJ trail camera");
            objects.Add(camGo);
            var cam = camGo.AddComponent<Camera>();
            cam.transform.position = new Vector3(0, 0, -10);
            cam.orthographic = true;
            cam.orthographicSize = 2.5f;
            cam.clearFlags = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.055f, 0.065f, 0.085f);
            cam.targetTexture = rt;
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
            for (int i = 0; i < 3; i++)
            {
                var coin = Object.Instantiate(prefab, new Vector3(-3.5f, 1.5f - i * 1.5f, 0), Quaternion.identity);
                objects.Add(coin);
                coin.GetComponent<EnemyCoinTrail>().BeginTrail(coin.GetComponent<SpriteRenderer>());
                trails.Add(coin.GetComponentInChildren<TrailRenderer>());
            }
            float start = Time.time;
            while (Time.time - start < 0.42f)
            {
                float t = Mathf.Clamp01((Time.time - start) / 0.42f);
                for (int i = 0; i < 3; i++)
                {
                    float curve = i == 0 ? 0f : Mathf.Sin(t * Mathf.PI * 2f) * (i == 1 ? 0.3f : 0.5f);
                    objects[i + 1].transform.position = new Vector3(Mathf.Lerp(-3.5f, 2.8f, t), 1.5f - i * 1.5f + curve, 0);
                }
                await Task.Delay(8);
            }
            cam.Render();
            RenderTexture.active = rt;
            capture = new Texture2D(960, 540, TextureFormat.RGB24, false);
            capture.ReadPixels(new Rect(0, 0, 960, 540), 0, 0);
            capture.Apply();
            File.WriteAllBytes("Tools~/CTJ/CoinTrailPreview.png", capture.EncodeToPNG());
            var mesh = new Mesh();
            trails[0].BakeMesh(mesh, cam, true);
            int vertices = mesh.vertexCount;
            Vector2[] uv = mesh.uv;
            Color[] colors = mesh.colors;
            Object.Destroy(mesh);
            return new { points = trails[0].positionCount, vertices, uvFirst = uv.Length > 0 ? uv[0].ToString() : "none",
                colorFirst = colors.Length > 0 ? colors[0].ToString() : "none", materialSupported = trails[0].sharedMaterial.shader.isSupported,
                pipeline = UnityEngine.Rendering.GraphicsSettings.currentRenderPipeline?.name };
        }
        finally
        {
            RenderTexture.active = previous;
            foreach (var go in objects) if (go != null) Object.Destroy(go);
            if (capture != null) Object.Destroy(capture);
            rt.Release(); Object.Destroy(rt);
        }
    }
    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(SceneKey, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(SceneKey);
        return new { restored = path };
    }
}
