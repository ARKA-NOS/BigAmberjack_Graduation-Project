using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using Lean.Pool;
using Newtonsoft.Json;
using Unity.Profiling;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

// Run through Unity Pipeline script run. All scene objects and quality settings are temporary.
public static class BloodPerformanceBenchmark
{
    private const string ScenePath = "Assets/_Scenes/Test/EnemyTestScene.unity";
    private const string DataPath = "Tools~/CTJ/BloodPerformanceResults_2026-11-23.json";
    private const string SceneKey = "CTJ.BloodBench.Scene";
    private const string ConfigKey = "CTJ.BloodBench.Config";
    private const string FixtureName = "__CTJ_BloodPerformanceFixture";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const int Layer = 31;
    private const int Repeats = 5;
    private const float Window = 0.9f;

    private delegate EnemyHitParticle SpawnDelegate(ParticleSystem prefab, Vector3 position, Quaternion rotation,
        Scene scene, float scale, float lifetime, EnemyParticleModule owner, float speed, float count);
    private delegate void PlayDelegate(EnemyHitParticle self, ParticleSystem root, float lifetime, float speed, float count);
    private static readonly SpawnDelegate PoolSpawn = (SpawnDelegate)Delegate.CreateDelegate(typeof(SpawnDelegate),
        typeof(EnemyHitParticle).GetMethod("Spawn", BindingFlags.Static | BindingFlags.NonPublic));
    private static readonly PlayDelegate Play = (PlayDelegate)Delegate.CreateDelegate(typeof(PlayDelegate),
        typeof(EnemyHitParticle).GetMethod("Play", Private));
    private static readonly Action<EnemyHitParticle> Release = (Action<EnemyHitParticle>)Delegate.CreateDelegate(typeof(Action<EnemyHitParticle>),
        typeof(EnemyHitParticle).GetMethod("Release", Private));

    [Serializable] public sealed class Config
    {
        public int vSync, frameRate;
        public float timeScale, fixedDelta, maximumDelta;
        public bool background;
        public int[] activeRoots;
    }
    [Serializable] public sealed class Stats
    {
        public int count;
        public double mean, median, p95, max;
    }
    [Serializable] public sealed class Trial
    {
        public string batch, payload, mode;
        public int repeat, concurrent, particles, sampleFrames, fixedCalls, collections0, collections1, collections2, retainedClones;
        public float measuredWindowSeconds;
        public double spawnMs, spawnMonoUsedDeltaKB, releaseRequestMs, releaseMonoUsedDeltaKB, cleanupMainMaxMs;
        public double fixedPerCallMs, trailPerCallMs, managedFrameTotalKB, unityMemoryDeltaMB;
        public Stats wallMs, mainMs, gpuMs, bloodFrameMs, trailFrameMs, gcFrameKB;
        public Stats lateBloodFrameMs, lateTrailFrameMs;
    }
    [Serializable] public sealed class Report
    {
        public string generated, unity, cpu, gpu, graphicsApi;
        public int ramMB, processorCount, width = 1280, height = 720, repeats = Repeats;
        public float sampleWindowSeconds = Window;
        public bool gpuTimingFeature;
        public string allocationMetric = "Spawn/release memory is Mono used-memory delta (approximate), not exact allocated bytes. Frame GC includes editor activity. Thread allocation API failed a 1MiB calibration and is not used.";
        public string comparison = "Current effect code; legacy-style Instantiate/Destroy versus warmed Lean Pool. Legacy payload=count1/speed1, current=count2/speed1.8. Not historical recorded performance.";
        public List<Trial> trials = new List<Trial>();
        public List<ColdTrial> coldTrials = new List<ColdTrial>();
    }
    [Serializable] public sealed class ColdTrial
    {
        public int concurrent, repeat, particles;
        public double spawnMs, spawnMonoUsedDeltaKB;
    }

    public static object Enter()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling && SceneManager.sceneCount == 1 &&
            !SceneManager.GetActiveScene().isDirty && PrefabStageUtility.GetCurrentPrefabStage() == null,
            "Stop Play Mode or save open scenes first");
        SessionState.SetString(SceneKey, SceneManager.GetActiveScene().path);
        EditorSceneManager.OpenScene(ScenePath);
        EditorApplication.EnterPlaymode();
        return "Starting EnemyTestScene performance measurements";
    }

    public static object Setup()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == ScenePath, "Use EnemyTestScene Play Mode");
        Check(GameObject.Find(FixtureName) == null, "Benchmark already initialized");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(root => root.activeSelf && root.GetComponent<LeanGameObjectPool>() == null).ToArray();
        var config = new Config { vSync = QualitySettings.vSyncCount, frameRate = Application.targetFrameRate,
            timeScale = Time.timeScale, fixedDelta = Time.fixedDeltaTime, maximumDelta = Time.maximumDeltaTime,
            background = Application.runInBackground, activeRoots = roots.Select(root => root.GetInstanceID()).ToArray() };
        SessionState.SetString(ConfigKey, JsonUtility.ToJson(config));
        foreach (var root in roots) root.SetActive(false);
        QualitySettings.vSyncCount = 0;
        Application.targetFrameRate = -1;
        Application.runInBackground = true;
        Time.timeScale = 1f;
        Time.fixedDeltaTime = 0.02f;
        Time.maximumDeltaTime = 0.1f;

        var fixture = new GameObject(FixtureName);
        Terrain(fixture, "floor", new Vector2(0f, -0.1f), new Vector2(30f, 0.2f));
        Terrain(fixture, "left wall", new Vector2(-10f, 5f), new Vector2(0.2f, 10f));
        Terrain(fixture, "right wall", new Vector2(10f, 5f), new Vector2(0.2f, 10f));
        Terrain(fixture, "ceiling", new Vector2(0f, 10.1f), new Vector2(20f, 0.2f));
        var cameraRoot = new GameObject("Benchmark camera") { layer = Layer };
        cameraRoot.transform.SetParent(fixture.transform);
        var camera = cameraRoot.AddComponent<Camera>();
        cameraRoot.AddComponent<AudioListener>();
        camera.orthographic = true;
        camera.orthographicSize = 6f;
        camera.transform.position = new Vector3(0f, 4f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.07f, 0.075f, 0.08f);
        camera.cullingMask = 1 << Layer;
        camera.targetTexture = new RenderTexture(1280, 720, 24) { name = "CTJ benchmark render" };
        camera.targetTexture.Create();

        var template = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(
            "Assets/_Prefabs/Enemies/Effects/DeathBloodBurst.prefab"));
        template.name = "Blood benchmark template";
        template.SetActive(false);
        template.transform.SetParent(fixture.transform);
        foreach (var node in template.GetComponentsInChildren<Transform>(true)) node.gameObject.layer = Layer;
        typeof(EnemyBloodParticle).GetField("UpdateMarker", BindingFlags.Static | BindingFlags.NonPublic).GetValue(null);
        var report = new Report { generated = DateTime.UtcNow.ToString("O"), unity = Application.unityVersion,
            cpu = SystemInfo.processorType, gpu = SystemInfo.graphicsDeviceName, graphicsApi = SystemInfo.graphicsDeviceType.ToString(),
            ramMB = SystemInfo.systemMemorySize, processorCount = SystemInfo.processorCount,
            gpuTimingFeature = FrameTimingManager.IsFeatureEnabled() };
        File.WriteAllText(DataPath, JsonConvert.SerializeObject(report, Formatting.Indented));
        return new { report.cpu, report.gpu, report.graphicsApi, report.gpuTimingFeature, width = 1280, height = 720 };
    }

    private static void Terrain(GameObject root, string name, Vector2 position, Vector2 size)
    {
        var item = new GameObject(name) { layer = LayerMask.NameToLayer("Ground") };
        item.transform.SetParent(root.transform);
        item.transform.position = position;
        item.AddComponent<BoxCollider2D>().size = size;
    }

    public static Task<object> Baseline() => Batch(0, false);
    public static Task<object> Legacy1() => Batch(1, false);
    public static Task<object> Legacy10() => Batch(10, false);
    public static Task<object> Legacy30() => Batch(30, false);
    public static Task<object> Current1() => Batch(1, true);
    public static Task<object> Current10() => Batch(10, true);
    public static Task<object> Current30() => Batch(30, true);
    public static Task<object> Steady30() => Batch(30, true, 4f, "steady");
    public static Task<object> Pilot() => Batch(2, true, 0.3f, "pilot", 1);

    public static async Task<object> ColdStart()
    {
        var fixture = GameObject.Find(FixtureName);
        Check(Application.isPlaying && fixture != null, "Initialize the benchmark first");
        var template = fixture.transform.Find("Blood benchmark template").GetComponent<ParticleSystem>();
        var results = new List<ColdTrial>();
        var active = new List<EnemyHitParticle>(30);
        var random = UnityEngine.Random.state;
        try
        {
            foreach (int count in new[] { 1, 10, 30 })
                for (int repeat = 0; repeat < Repeats; repeat++)
                {
                    foreach (var pool in Object.FindObjectsByType<LeanGameObjectPool>(FindObjectsSortMode.None))
                        if (pool.Prefab == template.gameObject) Object.Destroy(pool.gameObject);
                    await Awaitable.NextFrameAsync();
                    await Awaitable.NextFrameAsync();
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    UnityEngine.Random.InitState(2311000 + count * 100 + repeat);
                    long memoryBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                    long started = Stopwatch.GetTimestamp();
                    for (int i = 0; i < count; i++) active.Add(Spawn(true, template, i, 1.8f, 2f));
                    double elapsed = Elapsed(started);
                    long memoryDelta = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - memoryBefore;
                    int particles = active.Sum(item => item.GetComponent<ParticleSystem>().particleCount);
                    foreach (var item in active) Release(item);
                    active.Clear();
                    results.Add(new ColdTrial { concurrent = count, repeat = repeat + 1, particles = particles,
                        spawnMs = elapsed, spawnMonoUsedDeltaKB = memoryDelta / 1024d });
                }
            var report = JsonConvert.DeserializeObject<Report>(File.ReadAllText(DataPath));
            report.coldTrials = results;
            File.WriteAllText(DataPath, JsonConvert.SerializeObject(report, Formatting.Indented));
            return new { coldTrials = results.Count, results };
        }
        finally
        {
            foreach (var item in active) if (item != null) Release(item);
            UnityEngine.Random.state = random;
        }
    }

    private static async Task<object> Batch(int concurrent, bool current, float seconds = Window, string suffix = "", int repeats = Repeats)
    {
        var fixture = GameObject.Find(FixtureName);
        Check(Application.isPlaying && fixture != null, "Initialize the benchmark first");
        var template = fixture.transform.Find("Blood benchmark template").GetComponent<ParticleSystem>();
        string batch = (current ? "current" : "legacy") + concurrent + suffix;
        var trials = new List<Trial>();
        float count = current ? 2f : 1f;
        float speed = current ? 1.8f : 1f;
        var active = new List<EnemyHitParticle>(Mathf.Max(1, concurrent));
        var random = UnityEngine.Random.state;
        LeanGameObjectPool pool = null;
        try
        {
            // One full warm-up wave grows per-effect arrays and trail mesh buffers before pooled measurements.
            for (int i = 0; i < concurrent; i++) active.Add(Spawn(true, template, i, speed, count));
            if (active.Count > 0) pool = LeanPool.Links[active[0].gameObject];
            float warmUntil = Time.realtimeSinceStartup + 0.3f;
            while (Time.realtimeSinceStartup < warmUntil) await Awaitable.NextFrameAsync();
            foreach (var item in active) Release(item);
            active.Clear();
            await Awaitable.NextFrameAsync();

            for (int repeat = 0; repeat < repeats; repeat++)
                for (int mode = 0; mode < (concurrent == 0 ? 1 : 2); mode++)
                {
                    // Alternate order between repetitions to reduce editor/thermal drift bias.
                    bool pooled = (repeat % 2 == 0 ? mode : 1 - mode) == 0;
                    GC.Collect();
                    GC.WaitForPendingFinalizers();
                    await Awaitable.NextFrameAsync();
                    var frames = new List<double>(2048);
                    var mains = new List<double>(2048);
                    var gpus = new List<double>(2048);
                    var bloodFrames = new List<double>(2048);
                    var trailFrames = new List<double>(2048);
                    var gcFrames = new List<double>(2048);
                    var lateBloodFrames = new List<double>(2048);
                    var lateTrailFrames = new List<double>(2048);
                    int[] collections = { GC.CollectionCount(0), GC.CollectionCount(1), GC.CollectionCount(2) };
                    long memoryBefore = UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong();
                    UnityEngine.Random.InitState(2311000 + concurrent * 100 + repeat);
                    var options = ProfilerRecorderOptions.StartImmediately | ProfilerRecorderOptions.WrapAroundWhenCapacityReached |
                        ProfilerRecorderOptions.SumAllSamplesInFrame;
                    using var main = ProfilerRecorder.StartNew(ProfilerCategory.Internal, "Main Thread", 1, options);
                    using var gpu = ProfilerRecorder.StartNew(ProfilerCategory.Render, "GPU Frame Time", 1, options);
                    using var blood = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTJ.Blood.FixedUpdate", 1, options);
                    using var trails = ProfilerRecorder.StartNew(ProfilerCategory.Scripts, "CTJ.Blood.Trails", 1, options);
                    using var gc = ProfilerRecorder.StartNew(ProfilerCategory.Memory, "GC Allocated In Frame", 1, options);
                    Check(main.Valid && blood.Valid && trails.Valid && gc.Valid, "Required profiler counters unavailable");
                    long allocBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                    long spawnAt = Stopwatch.GetTimestamp();
                    for (int i = 0; i < concurrent; i++) active.Add(Spawn(pooled, template, i, speed, count));
                    double spawnMs = Elapsed(spawnAt);
                    long spawnAlloc = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - allocBefore;
                    int particles = 0;
                    foreach (var item in active) particles += item.GetComponent<ParticleSystem>().particleCount;
                    int calls = 0, trailCalls = 0;
                    double bloodTotal = 0, trailTotal = 0;
                    long frameAt = Stopwatch.GetTimestamp();
                    double started = Time.realtimeSinceStartupAsDouble;
                    while (Time.realtimeSinceStartupAsDouble - started < seconds)
                    {
                        await Awaitable.EndOfFrameAsync();
                        frames.Add(Elapsed(frameAt));
                        frameAt = Stopwatch.GetTimestamp();
                        if (main.LastValue > 0) mains.Add(main.LastValue / 1000000d);
                        if (gpu.Valid && gpu.LastValue > 0) gpus.Add(gpu.LastValue / 1000000d);
                        double b = blood.LastValue / 1000000d, t = trails.LastValue / 1000000d;
                        bloodFrames.Add(b); trailFrames.Add(t); gcFrames.Add(gc.LastValue / 1024d);
                        if (Time.realtimeSinceStartupAsDouble - started >= 2.6d)
                        {
                            lateBloodFrames.Add(b);
                            lateTrailFrames.Add(t);
                        }
                        bloodTotal += b; trailTotal += t;
                        if (blood.Count > 0) calls += (int)blood.GetSample(blood.Count - 1).Count;
                        if (trails.Count > 0) trailCalls += (int)trails.GetSample(trails.Count - 1).Count;
                        await Awaitable.NextFrameAsync();
                    }
                    allocBefore = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong();
                    long releaseAt = Stopwatch.GetTimestamp();
                    foreach (var item in active) Release(item);
                    active.Clear();
                    double releaseMs = Elapsed(releaseAt);
                    long releaseAlloc = UnityEngine.Profiling.Profiler.GetMonoUsedSizeLong() - allocBefore;
                    double cleanup = 0;
                    for (int frame = 0; frame < 3; frame++)
                    {
                        await Awaitable.NextFrameAsync();
                        await Awaitable.EndOfFrameAsync();
                        cleanup = Math.Max(cleanup, main.LastValue / 1000000d);
                    }
                    var trial = new Trial { batch = batch, payload = current ? "current" : "legacy", mode = concurrent == 0 ? "baseline" : pooled ? "pool-warm" : "instantiate-destroy",
                        repeat = repeat + 1, concurrent = concurrent, particles = particles, sampleFrames = frames.Count, measuredWindowSeconds = seconds,
                        spawnMs = spawnMs, spawnMonoUsedDeltaKB = spawnAlloc / 1024d, releaseRequestMs = releaseMs,
                        releaseMonoUsedDeltaKB = releaseAlloc / 1024d, cleanupMainMaxMs = cleanup,
                        wallMs = Summarize(frames), mainMs = Summarize(mains), gpuMs = Summarize(gpus),
                        bloodFrameMs = Summarize(bloodFrames), trailFrameMs = Summarize(trailFrames), gcFrameKB = Summarize(gcFrames),
                        lateBloodFrameMs = Summarize(lateBloodFrames), lateTrailFrameMs = Summarize(lateTrailFrames),
                        fixedCalls = calls, fixedPerCallMs = calls > 0 ? bloodTotal / calls : 0,
                        trailPerCallMs = trailCalls > 0 ? trailTotal / trailCalls : 0,
                        managedFrameTotalKB = gcFrames.Sum(),
                        collections0 = GC.CollectionCount(0) - collections[0], collections1 = GC.CollectionCount(1) - collections[1],
                        collections2 = GC.CollectionCount(2) - collections[2],
                        unityMemoryDeltaMB = (UnityEngine.Profiling.Profiler.GetTotalAllocatedMemoryLong() - memoryBefore) / 1048576d,
                        retainedClones = pool != null ? pool.Despawned : 0 };
                    trials.Add(trial);
                    await Awaitable.NextFrameAsync();
                }
            var report = JsonConvert.DeserializeObject<Report>(File.ReadAllText(DataPath));
            foreach (var trial in report.trials)
                if (trial.measuredWindowSeconds <= 0f) trial.measuredWindowSeconds = trial.batch.EndsWith("pilot") ? 0.3f : Window;
            report.trials.RemoveAll(trial => trial.batch == batch);
            report.trials.AddRange(trials);
            File.WriteAllText(DataPath, JsonConvert.SerializeObject(report, Formatting.Indented));
            return new { batch, trials = trials.Count, frames = trials.Sum(t => t.sampleFrames),
                pooledSpawnMs = trials.Where(t => t.mode == "pool-warm").Select(t => t.spawnMs).DefaultIfEmpty().Average(),
                instantiateSpawnMs = trials.Where(t => t.mode == "instantiate-destroy").Select(t => t.spawnMs).DefaultIfEmpty().Average(),
                maxBloodFrameMs = trials.Max(t => t.bloodFrameMs.max), data = DataPath };
        }
        finally
        {
            foreach (var item in active) if (item != null) Release(item);
            UnityEngine.Random.state = random;
        }
    }

    private static EnemyHitParticle Spawn(bool pooled, ParticleSystem prefab, int index, float speed, float count)
    {
        Vector3 position = new Vector3((index % 10 - 4.5f) * 1.1f, 0.9f + (index / 10) * 0.45f);
        if (pooled) return PoolSpawn(prefab, position, Quaternion.identity, SceneManager.GetActiveScene(), 1f, 5f, null, speed, count);
        // Reproduce the pre-pooling lifecycle with the current, otherwise identical effect implementation.
        var particle = Object.Instantiate(prefab, position, Quaternion.identity);
        particle.gameObject.SetActive(true);
        var lifetime = particle.GetComponent<EnemyHitParticle>();
        if (lifetime == null) lifetime = particle.gameObject.AddComponent<EnemyHitParticle>();
        Play(lifetime, particle, 5f, speed, count);
        return lifetime;
    }

    private static double Elapsed(long started) => (Stopwatch.GetTimestamp() - started) * 1000d / Stopwatch.Frequency;
    private static Stats Summarize(List<double> values)
    {
        if (values.Count == 0) return new Stats();
        double[] sorted = values.OrderBy(value => value).ToArray();
        return new Stats { count = sorted.Length, mean = values.Average(), median = sorted[sorted.Length / 2],
            p95 = sorted[Math.Max(0, (int)Math.Ceiling(sorted.Length * 0.95) - 1)], max = sorted[sorted.Length - 1] };
    }

    public static object Cleanup()
    {
        var fixture = GameObject.Find(FixtureName);
        if (fixture != null)
        {
            var template = fixture.transform.Find("Blood benchmark template").gameObject;
            foreach (var pool in Object.FindObjectsByType<LeanGameObjectPool>(FindObjectsSortMode.None))
                if (pool.Prefab == template) Object.Destroy(pool.gameObject);
            var camera = fixture.GetComponentInChildren<Camera>();
            if (camera != null && camera.targetTexture != null)
            {
                var texture = camera.targetTexture;
                camera.targetTexture = null;
                texture.Release();
                Object.Destroy(texture);
            }
            Object.Destroy(fixture);
        }
        string json = SessionState.GetString(ConfigKey, "");
        if (!string.IsNullOrEmpty(json))
        {
            var config = JsonUtility.FromJson<Config>(json);
            QualitySettings.vSyncCount = config.vSync;
            Application.targetFrameRate = config.frameRate;
            Application.runInBackground = config.background;
            Time.timeScale = config.timeScale;
            Time.fixedDeltaTime = config.fixedDelta;
            Time.maximumDeltaTime = config.maximumDelta;
            foreach (var root in SceneManager.GetActiveScene().GetRootGameObjects())
                if (config.activeRoots.Contains(root.GetInstanceID())) root.SetActive(true);
            SessionState.EraseString(ConfigKey);
        }
        return "Runtime benchmark settings restored";
    }
    public static object Exit() { EditorApplication.ExitPlaymode(); return "Exiting benchmark"; }
    public static object Restore()
    {
        Check(!Application.isPlaying && !SceneManager.GetActiveScene().isDirty, "Editor busy or scene changed");
        string previous = SessionState.GetString(SceneKey, "");
        if (!string.IsNullOrEmpty(previous)) EditorSceneManager.OpenScene(previous);
        SessionState.EraseString(SceneKey);
        return new { scene = SceneManager.GetActiveScene().path, dirty = SceneManager.GetActiveScene().isDirty };
    }
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new InvalidOperationException(message);
    }
}
