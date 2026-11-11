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
    private const string ChestCoinPath = "Assets/_Prefabs/Chest/ChestRewardCoin.prefab";
    private static readonly string[] EnemyPaths =
    {
        "Assets/_Prefabs/Enemies/Enemy/Enemy01/Enemy01_TwistedArm.prefab",
        "Assets/_Prefabs/Enemies/Enemy/Enemy02/Enemy02_ProjectileRobot.prefab",
        "Assets/_Prefabs/Enemies/Enemy/Enemy04/Enemy04_MechanicArcher.prefab",
        "Assets/_Prefabs/Enemies/Test/Catto_Melee.prefab",
        "Assets/_Prefabs/Enemies/Test/MadGhost_Ranged.prefab",
        "Assets/_Prefabs/Enemies/Bosses/EyeBoss/RobotEyeBoss.prefab"
    };
    private const string SceneKey = "CTJ.CoinTrail.OriginalScene";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
    }
    private static object Call(object target, string name, params object[] args) =>
        target.GetType().GetMethod(name, Private).Invoke(target, args);

    public static object GetEditorState() => new
    {
        playing = EditorApplication.isPlaying,
        scene = SceneManager.GetActiveScene().path,
        dirty = SceneManager.GetActiveScene().isDirty
    };

    public static object CheckAssets()
    {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestCoinPath);
        Check(prefab != null && prefab.GetComponent<EnemyCoinTrail>() != null, "Coin / trail reference missing");
        Check(chestPrefab != null && chestPrefab.GetComponent<EnemyRewardCoin>() != null &&
            chestPrefab.GetComponent<EnemyCoinTrail>() != null, "Chest coin variant / trail reference missing");
        Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(prefab) == 0, "Missing script");
        var settings = new SerializedObject(prefab.GetComponent<EnemyCoinTrail>());
        Material material = settings.FindProperty("trailMaterial").objectReferenceValue as Material;
        Check(material != null && material.shader != null, "Material / shader missing");
        Check(!ShaderUtil.ShaderHasError(material.shader), "Shader compile error");
        AnimationCurve profile = settings.FindProperty("widthProfile").animationCurveValue;
        Check(profile.Evaluate(1f) == 0f, "Tail must taper to one point");
        var coinSettings = new SerializedObject(prefab.GetComponent<EnemyRewardCoin>());
        var chestSettings = new SerializedObject(chestPrefab.GetComponent<EnemyRewardCoin>());
        float enemyLaunchBoost = 0f;
        foreach (string path in EnemyPaths)
        {
            GameObject enemyPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            EnemyCurrencyRewardModule reward = enemyPrefab != null
                ? enemyPrefab.GetComponentInChildren<EnemyCurrencyRewardModule>(true) : null;
            Check(reward != null, "Enemy coin reward module missing: " + path);
            float multiplier = new SerializedObject(reward)
                .FindProperty("deathCoinLaunchMultiplier").floatValue;
            Check(multiplier > 1f, "Enemy death coin launch boost missing: " + path);
            if (enemyLaunchBoost == 0f) enemyLaunchBoost = multiplier;
        }
        Check(coinSettings.FindProperty("coinDeathTime").floatValue > 0f &&
            chestSettings.FindProperty("coinDeathTime").floatValue > 0f,
            "Coin lifetime missing");
        var trailSettings = new SerializedObject(prefab.GetComponent<EnemyCoinTrail>());
        Check(trailSettings.FindProperty("fadeStartDistance").floatValue >
            trailSettings.FindProperty("hideDistance").floatValue,
            "Trail proximity fade distances invalid");
        Check(Mathf.Approximately(trailSettings.FindProperty("fadeStartDistance").floatValue, 4.5f),
            "Trail fade should begin farther away");
        Check(coinSettings.FindProperty("minimumLaunchElevation").floatValue > 0f &&
            chestSettings.FindProperty("minimumLaunchElevation").floatValue > 0f &&
            coinSettings.FindProperty("launchGravityPause").floatValue > 0f &&
            chestSettings.FindProperty("launchGravityPause").floatValue > 0f,
            "Upward launch or brief gravity pause missing");
        Check(Mathf.Approximately(coinSettings.FindProperty("gravityScale").floatValue, 3f) &&
            Mathf.Approximately(chestSettings.FindProperty("gravityScale").floatValue, 3f),
            "Coin gravity was not restored");
        return new { passed = true, lifetime = settings.FindProperty("trailLifetime").floatValue,
            gravity = coinSettings.FindProperty("gravityScale").floatValue,
            launchForce = coinSettings.FindProperty("scatterForceMultiplier").floatValue,
            directionMode = "random angle within four upward sectors",
            gravityPause = coinSettings.FindProperty("launchGravityPause").floatValue,
            enemyLaunchBoost,
            enemyPrefabCount = EnemyPaths.Length,
            coinDeathTime = coinSettings.FindProperty("coinDeathTime").floatValue,
            trailFadeStart = trailSettings.FindProperty("fadeStartDistance").floatValue,
            trailHideDistance = trailSettings.FindProperty("hideDistance").floatValue,
            width = settings.FindProperty("trailWidth").floatValue, shader = material.shader.name,
            chestCoin = chestPrefab.name };
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
        bool bounceTrail = false, homingTrail = false, collectionStopped = false, faded = false;
        bool toggled = false, cleared = false, noPrematureReward = true;
        bool launchedAcrossUpperArc = true, noTrailBeforeBounce = true;
        bool fixedLaunchStrength = true, randomizedDirection = false;
        bool gravityPausedInitially = false, gravityResumed = false;
        bool fixedTrailLifetime = true, equalNearAndFarHomingSpeed = false;
        bool proximityFade = false;
        int maxPoints = 0;
        float longest = 0f, maxScatterRadius = 0f, firstBounceTimeTotal = 0f;
        int firstBounceSamples = 0;
        var launchPositions = new Vector3[8];
        var firstBounceSeen = new bool[8];
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CoinPath);
        GameObject chestPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(ChestCoinPath);
        EnemyRewardCoin timeoutCoin = null, baselineProbe = null, boostedProbe = null;
        try
        {
            for (int i = 0; i < 8; i++)
            {
                GameObject source = i < 4 ? prefab : chestPrefab;
                EnemyRewardCoin coin = Object.Instantiate(source, new Vector3(i * 0.1f, 0.6f, 0f), Quaternion.identity).GetComponent<EnemyRewardCoin>();
                coins.Add(coin);
                launchPositions[i] = coin.transform.position;
                if (i == 3) coin.GetComponent<EnemyCoinTrail>().enabled = false;
                Call(coin, "SetCollector", collector.transform);
                float launchBoost = i < 4 ? 1.45f : 1f;
                Call(coin, "Launch", currency, 3, (i % 4 + 0.5f) / 4f, launchBoost);
                Vector2 velocity = (Vector2)typeof(EnemyRewardCoin).GetField("_velocity", Private).GetValue(coin);
                Vector2 radialVelocity = velocity - Vector2.up * (0.5f * launchBoost);
                float expectedSpeed = (i < 4 ? 4.175f : 5.375f) * launchBoost;
                fixedLaunchStrength &= Mathf.Abs(radialVelocity.magnitude - expectedSpeed) < 0.001f;
                float baseAngle = (20f + (i % 4 + 0.5f) * 35f) * Mathf.Deg2Rad;
                Vector2 baseDirection = new Vector2(Mathf.Cos(baseAngle), Mathf.Sin(baseAngle));
                randomizedDirection |= Vector2.Angle(baseDirection, radialVelocity) > 5f;
                float launchAngle = Mathf.Atan2(radialVelocity.y, radialVelocity.x) * Mathf.Rad2Deg;
                launchedAcrossUpperArc &= launchAngle >= 20f && launchAngle <= 160f && velocity.y > 0f &&
                    Mathf.Sign(radialVelocity.x) == (i % 4 < 2 ? 1f : -1f);
                noTrailBeforeBounce &= coin.GetComponentInChildren<TrailRenderer>() == null;
            }
            Check(launchedAcrossUpperArc && randomizedDirection && fixedLaunchStrength && noTrailBeforeBounce,
                "Upward random direction, fixed strength, or pre-bounce trail failed");
            var pauseProbe = coins[1];
            var velocityField = typeof(EnemyRewardCoin).GetField("_velocity", Private);
            var airTimeField = typeof(EnemyRewardCoin).GetField("_airTime", Private);
            float gravityBoost = (float)typeof(EnemyRewardCoin).GetField("_scatterGravityMultiplier", Private).GetValue(pauseProbe);
            Check(Mathf.Abs(gravityBoost - 1.45f * 1.45f) < 0.001f,
                "Enemy scatter gravity did not compensate for launch boost");
            Vector3 pauseStartPosition = pauseProbe.transform.position;
            Vector2 pauseStartVelocity = (Vector2)velocityField.GetValue(pauseProbe);
            Call(pauseProbe, "FixedUpdate");
            gravityPausedInitially = Mathf.Abs(((Vector2)velocityField.GetValue(pauseProbe)).y - pauseStartVelocity.y) < 0.0001f;
            airTimeField.SetValue(pauseProbe, 0.12f);
            Call(pauseProbe, "FixedUpdate");
            gravityResumed = ((Vector2)velocityField.GetValue(pauseProbe)).y < pauseStartVelocity.y;
            pauseProbe.transform.position = pauseStartPosition;
            velocityField.SetValue(pauseProbe, pauseStartVelocity);
            airTimeField.SetValue(pauseProbe, 0f);
            Check(gravityPausedInitially && gravityResumed, "Launch gravity pause did not end correctly");
            var speedProbe = coins[0];
            typeof(EnemyRewardCoin).GetField("_homingSpeed", Private).SetValue(speedProbe, 16f);
            Vector3 launchPosition = speedProbe.transform.position;
            speedProbe.transform.position = new Vector3(4f, 0f, 0f);
            Call(speedProbe, "TickHoming", 0.02f);
            float nearStep = speedProbe.transform.position.x - 4f;
            speedProbe.transform.position = Vector3.zero;
            Call(speedProbe, "TickHoming", 0.02f);
            float farStep = speedProbe.transform.position.x;
            equalNearAndFarHomingSpeed = Mathf.Abs(nearStep - farStep) < 0.001f && nearStep > 0f;
            speedProbe.transform.position = launchPosition;
            Check(equalNearAndFarHomingSpeed, "Homing speed should not depend on target distance");
            Physics2D.SyncTransforms();
            float start = Time.time;
            while (Time.time - start < 10f)
            {
                bool alive = false;
                for (int index = 0; index < coins.Count; index++)
                {
                    EnemyRewardCoin coin = coins[index];
                    if (coin == null) continue;
                    alive = true;
                    if (coin.BounceCount == 0 && !coin.IsHoming)
                        maxScatterRadius = Mathf.Max(maxScatterRadius,
                            Vector2.Distance(coin.transform.position, launchPositions[index]));
                    else if (coin.BounceCount > 0 && !firstBounceSeen[index])
                    {
                        firstBounceSeen[index] = true;
                        firstBounceTimeTotal += Time.time - start;
                        firstBounceSamples++;
                    }
                    TrailRenderer trail = coin.GetComponentInChildren<TrailRenderer>();
                    if (trail == null) continue;
                    fixedTrailLifetime &= Mathf.Abs(trail.time - 0.45f) < 0.001f;
                    maxPoints = Mathf.Max(maxPoints, trail.positionCount);
                    float length = 0f;
                    for (int n = 1; n < trail.positionCount; n++) length += Vector3.Distance(trail.GetPosition(n - 1), trail.GetPosition(n));
                    longest = Mathf.Max(longest, length);
                    if (!coin.IsCollecting && trail.positionCount > 2)
                    {
                        if (coin.IsHoming) homingTrail = true;
                        else if (coin.BounceCount > 0) bounceTrail = true;
                        else noTrailBeforeBounce = false;
                    }
                    if (coin.IsCollecting)
                    {
                        Check(!trail.emitting, "Still emitting after collection");
                        collectionStopped = true;
                        if (!trail.enabled && trail.positionCount == 0) faded = true;
                    }
                }
                if (!collectionStopped && currency.Count != 0) noPrematureReward = false;
                if (!toggled && coins[0] != null && coins[0].BounceCount > 0)
                {
                    var component = coins[0].GetComponent<EnemyCoinTrail>();
                    TrailRenderer trail = coins[0].GetComponentInChildren<TrailRenderer>();
                    var properties = new MaterialPropertyBlock();
                    int tint = Shader.PropertyToID("_Color");
                    component.SetCollectorDistance(4f);
                    trail.GetPropertyBlock(properties);
                    float farAlpha = properties.GetColor(tint).a;
                    component.SetCollectorDistance(1.5f);
                    trail.GetPropertyBlock(properties);
                    float middleAlpha = properties.GetColor(tint).a;
                    component.SetCollectorDistance(0.5f);
                    trail.GetPropertyBlock(properties);
                    float nearAlpha = properties.GetColor(tint).a;
                    bool hidden = !trail.emitting && trail.positionCount == 0;
                    component.SetCollectorDistance(4f);
                    proximityFade = farAlpha > middleAlpha && middleAlpha > nearAlpha &&
                        nearAlpha < 0.001f && hidden && trail.emitting;
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
            Check(currency.Count == 24 && payouts == 8, "Reward lost or paid twice");
            Check(maxScatterRadius > 1.5f && firstBounceSamples == 8 &&
                firstBounceTimeTotal / firstBounceSamples > 0.3f,
                "Scatter spread or air time was too small");
            Check(noPrematureReward && noTrailBeforeBounce && fixedTrailLifetime && proximityFade &&
                bounceTrail && homingTrail && collectionStopped && faded && cleared,
                "A trail lifecycle stage was not observed");

            var probeCurrency = new CurrencyData(null, 0);
            var savedRandomState = UnityEngine.Random.state;
            try
            {
                baselineProbe = Object.Instantiate(prefab, new Vector3(-4f, 0.6f, 0f), Quaternion.identity)
                    .GetComponent<EnemyRewardCoin>();
                boostedProbe = Object.Instantiate(prefab, new Vector3(4f, 0.6f, 0f), Quaternion.identity)
                    .GetComponent<EnemyRewardCoin>();
                UnityEngine.Random.InitState(731);
                Call(baselineProbe, "Launch", probeCurrency, 0, 0.125f, 1f);
                UnityEngine.Random.InitState(731);
                Call(boostedProbe, "Launch", probeCurrency, 0, 0.125f, 1.45f);
            }
            finally { UnityEngine.Random.state = savedRandomState; }
            float baselineInitialSpeed = ((Vector2)velocityField.GetValue(baselineProbe)).magnitude;
            float boostedInitialSpeed = ((Vector2)velocityField.GetValue(boostedProbe)).magnitude;
            float baselineRange = -1f, boostedRange = -1f;
            float rangeStart = Time.time;
            while (Time.time - rangeStart < 3f && (baselineRange < 0f || boostedRange < 0f))
            {
                if (baselineRange < 0f && baselineProbe.BounceCount > 0)
                    baselineRange = Mathf.Abs(baselineProbe.transform.position.x + 4f);
                if (boostedRange < 0f && boostedProbe.BounceCount > 0)
                    boostedRange = Mathf.Abs(boostedProbe.transform.position.x - 4f);
                await Task.Delay(16);
            }
            bool similarScatterRange = baselineRange > 0.1f && boostedRange > 0.1f &&
                Mathf.Abs(boostedRange - baselineRange) / baselineRange < 0.25f;
            Check(Mathf.Abs(boostedInitialSpeed / baselineInitialSpeed - 1.45f) < 0.001f &&
                similarScatterRange, "Enemy coin boost changed scatter range too much");

            var timeoutCurrency = new CurrencyData(null, 0);
            int timeoutPayouts = 0;
            float timeoutPayoutAt = -1f;
            float timeoutStart = Time.time;
            timeoutCurrency.OnCurrencyChanged += (current, previous) =>
            {
                timeoutPayouts++;
                timeoutPayoutAt = Time.time - timeoutStart;
            };
            timeoutCoin = Object.Instantiate(prefab, new Vector3(0f, 3f, 0f), Quaternion.identity)
                .GetComponent<EnemyRewardCoin>();
            typeof(EnemyRewardCoin).GetField("coinDeathTime", Private).SetValue(timeoutCoin, 0.25f);
            Call(timeoutCoin, "Launch", timeoutCurrency, 5, 0.5f, 1f);
            bool timeoutBeforeGround = true;
            while (timeoutCoin != null && Time.time - timeoutStart < 1f)
            {
                timeoutBeforeGround &= timeoutCoin.BounceCount == 0;
                await Task.Delay(16);
            }
            Check(timeoutCoin == null && timeoutBeforeGround && timeoutCurrency.Count == 5 &&
                timeoutPayouts == 1 && timeoutPayoutAt >= 0.25f && timeoutPayoutAt < 0.6f,
                "Timed-out coin did not disappear and pay exactly once");
            return new { passed = true, currency = currency.Count, payouts, launchedAcrossUpperArc,
                randomizedDirection, fixedLaunchStrength, gravityPausedInitially, gravityResumed,
                noTrailBeforeBounce, equalNearAndFarHomingSpeed, nearStep, farStep, fixedTrailLifetime,
                bounceTrail, homingTrail, proximityFade,
                collectionStopped, faded, cleared, noPrematureReward, maxScatterRadius,
                baselineInitialSpeed, boostedInitialSpeed, baselineRange, boostedRange,
                similarScatterRange, timeoutPayoutAt, timeoutPayouts, timeoutBeforeGround,
                firstBounceTime = firstBounceTimeTotal / firstBounceSamples, maxPoints, longest };
        }
        finally
        {
            foreach (EnemyRewardCoin coin in coins) if (coin != null) Object.Destroy(coin.gameObject);
            if (timeoutCoin != null) Object.Destroy(timeoutCoin.gameObject);
            if (baselineProbe != null) Object.Destroy(baselineProbe.gameObject);
            if (boostedProbe != null) Object.Destroy(boostedProbe.gameObject);
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
