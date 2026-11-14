using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.DevLib.BattleSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyHitCameraShake
{
    private const string EnemyPath = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy01/Enemy01_TwistedArm.prefab";
    private const string CameraPath = "Assets/_Prefabs/Player/PlayerCam.prefab";
    private const string SceneKey = "CTJ.HitCameraShake.OriginalScene";
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private static CinemachineImpulseManager Manager => CinemachineImpulseManager.Instance;
    private static List<CinemachineImpulseManager.ImpulseEvent> Events =>
        (List<CinemachineImpulseManager.ImpulseEvent>)typeof(CinemachineImpulseManager).GetField("m_ActiveEvents", Private).GetValue(Manager);
    private static int Count => Events?.Count ?? 0;
    private static void Check(bool success, string message) { if (!success) throw new Exception(message); }
    private static void Hit(EnemyBase enemy, float damage, bool critical = false) => enemy.ApplyDamage(
        new DamageData { DamageAmount = damage, IsCritical = critical }, enemy.transform.position, Vector2.right, Vector2.left);
    private static Vector3 Sample(Vector3 position, int channel = 1)
    {
        Manager.GetImpulseAt(position, true, channel, out var offset, out var rotation);
        Check(Quaternion.Angle(rotation, Quaternion.identity) < 0.001f && Mathf.Abs(offset.z) < 0.00001f, "Unexpected Z/rotation shake");
        return offset;
    }

    public static object CheckAssets()
    {
        var paths = AssetDatabase.FindAssets("t:Prefab", new[] { "Assets/_Prefabs/Enemies" })
            .Select(AssetDatabase.GUIDToAssetPath).Where(path =>
                AssetDatabase.LoadAssetAtPath<GameObject>(path).GetComponent<EnemyBase>() != null).ToArray();
        foreach (string path in paths)
        {
            var root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            Check(root.GetComponentsInChildren<EnemyHitCameraShakeModule>(true).Length == 1, "One module required: " + path);
            foreach (var transform in root.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(transform.gameObject) == 0, "Missing script: " + path);
            var module = root.GetComponentInChildren<EnemyHitCameraShakeModule>(true);
            Check(module.enabled && module.gameObject.activeSelf, "Inactive module: " + path);
            var data = new SerializedObject(module);
            Check(data.FindProperty("_impulseChannel").intValue == 1, "Wrong channel: " + path);
            Check(data.FindProperty("_criticalStrength").floatValue > data.FindProperty("_normalStrength").floatValue, "Critical not stronger: " + path);
            Check(Mathf.Approximately(data.FindProperty("_normalStrength").floatValue, 0.08f) &&
                Mathf.Approximately(data.FindProperty("_criticalStrength").floatValue, 0.18f),
                "Hit shake strength mismatch: " + path);
            Check(Mathf.Approximately(data.FindProperty("_directionJitterDegrees").floatValue, 35f), "Direction jitter missing: " + path);
            Check(Mathf.Approximately(data.FindProperty("_lethalStrengthMultiplier").floatValue, 3.2f) &&
                Mathf.Abs(data.FindProperty("_lethalDurationMultiplier").floatValue - 1.7f) < 0.001f,
                "Lethal shake settings missing: " + path);
        }
        var listener = AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath).GetComponent<CinemachineImpulseListener>();
        Check(listener != null && listener.enabled && listener.Use2DDistance && (listener.ChannelMask & 1) != 0, "Player camera listener mismatch");
        return new { passed = true, enemyPrefabs = paths.Length, channel = 1, playerListenerGain = listener.Gain };
    }

    public static object BeginPlay()
    {
        Check(!EditorApplication.isPlaying && SceneManager.sceneCount == 1, "One saved scene in edit mode required");
        var scene = SceneManager.GetActiveScene();
        Check(!scene.isDirty && !string.IsNullOrEmpty(scene.path), "Save the current scene yourself first");
        string path = scene.path;
        SessionState.SetString(SceneKey, path);
        EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        EditorApplication.isPlaying = true;
        return path;
    }

    public static async Task<object> RunPlay()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated play scene required");
        float previousClock = CinemachineCore.CurrentTimeOverride;
        bool previousTimeMode = Manager.IgnoreTimeScale;
        float previousIntensity = EnemyHitCameraShakeManager.Intensity;
        GameObject root = null, cameraRoot = null, target = null;
        try
        {
            EnemyHitCameraShakeManager.SetIntensity(1f);
            // 검증용 빈 씬에서만 시간을 고정합니다. 게임 코드/씬 에셋은 변경하지 않습니다.
            Manager.Clear();
            Manager.IgnoreTimeScale = false;
            CinemachineCore.CurrentTimeOverride = 100f;
            root = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(EnemyPath));
            var enemy = root.GetComponent<EnemyBase>();
            root.GetComponent<Rigidbody2D>().simulated = false;
            foreach (var component in root.GetComponentsInChildren<MonoBehaviour>())
                if (component is EnemyParticleModule || component is EnemyHitReactionModule || component is EnemyDeathModule ||
                    component is EnemyCurrencyRewardModule || component is EnemyDamageIndicatorModule) component.enabled = false;
            var module = enemy.GetModule<EnemyHitCameraShakeModule>();
            var health = enemy.GetModule<INewHealthModule>();
            float initialHealth = health.CurrentHealth;

            // 같은 방향의 타격을 반복해도 방향이 달라지고 설정 각도를 넘지 않는지 확인합니다.
            var previousRandomState = UnityEngine.Random.state;
            var angles = new List<float>();
            try
            {
                UnityEngine.Random.InitState(20261111);
                for (int i = 0; i < 12; i++)
                {
                    module.PlayShake(false, Vector2.right);
                    Check(Count == 1, "Direction preview should emit one impulse");
                    var signal = Events[0].SignalSource;
                    signal.GetSignal(signal.SignalDuration * 0.5f, out var offset, out _);
                    float angle = Vector2.SignedAngle(Vector2.right, offset);
                    Check(Mathf.Abs(angle) <= 35.01f && offset.magnitude > 0.07f,
                        "Random direction escaped configured angle or changed strength");
                    angles.Add(angle);
                    CinemachineCore.CurrentTimeOverride += 1f;
                    Sample(Vector3.zero);
                    Check(Count == 0, "Direction preview impulse did not expire");
                }
                Check(angles.Min() < -15f && angles.Max() > 15f && angles.Max() - angles.Min() > 35f,
                    "Repeated hits still shake in nearly the same direction");
                typeof(EnemyHitCameraShakeModule).GetField("_directionJitterDegrees", Private).SetValue(module, 0f);
                module.PlayShake(false, Vector2.right);
                var fixedSignal = Events[0].SignalSource;
                fixedSignal.GetSignal(fixedSignal.SignalDuration * 0.5f, out var fixedOffset, out _);
                Check(Mathf.Abs(Vector2.SignedAngle(Vector2.right, fixedOffset)) < 0.001f,
                    "Zero jitter did not preserve the original direction");
                CinemachineCore.CurrentTimeOverride += 1f;
                Sample(Vector3.zero);
                typeof(EnemyHitCameraShakeModule).GetField("_directionJitterDegrees", Private).SetValue(module, 35f);
            }
            finally { UnityEngine.Random.state = previousRandomState; }
            CinemachineCore.CurrentTimeOverride = 100f;

            target = new GameObject("Stationary camera target");
            cameraRoot = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(CameraPath));
            var camera = cameraRoot.GetComponent<CinemachineCamera>();
            camera.Follow = target.transform;
            camera.InternalUpdateCameraState(Vector3.up, -1f);

            Hit(enemy, 2f);
            Check(Count == 1, "Normal damage must emit exactly one impulse");
            float normalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 100f + normalDuration * 0.5f;
            var normal = Sample(Vector3.zero);
            Check(normal.magnitude > 0.07f && normal.magnitude < 0.09f, "Normal signal outside configured range");
            camera.InternalUpdateCameraState(Vector3.up, 1f / 60f);
            float actualCameraOffset = camera.State.PositionCorrection.magnitude;
            Check(actualCameraOffset > 0.02f, "Existing PlayerCam listener did not apply the hit impulse");
            Check(Sample(new Vector3(30, 0, -10)).sqrMagnitude < 0.000001f, "Distant hit shakes camera");
            Check(Sample(Vector3.zero, 64).sqrMagnitude < 0.000001f, "Hit leaked into boss laser channel");
            float partial = Sample(new Vector3(16, 0, -10)).magnitude;
            Check(partial > 0f && partial < normal.magnitude, "Distance falloff missing");
            CinemachineCore.CurrentTimeOverride = 101f;
            Check(Sample(Vector3.zero).sqrMagnitude == 0 && Count == 0, "Normal impulse did not expire");

            EnemyHitCameraShakeManager.SetIntensity(0.5f);
            module.PlayShake(false, Vector2.right);
            CinemachineCore.CurrentTimeOverride = 101f + normalDuration * 0.5f;
            float halfIntensity = Sample(Vector3.zero).magnitude;
            Check(Mathf.Abs(halfIntensity / normal.magnitude - 0.5f) < 0.01f,
                "Global intensity did not scale the hit shake");
            CinemachineCore.CurrentTimeOverride = 101.5f;
            Sample(Vector3.zero);
            EnemyHitCameraShakeManager.SetIntensity(0f);
            module.PlayShake(false, Vector2.right, true);
            Check(Count == 0, "Zero global intensity emitted a lethal impulse");
            EnemyHitCameraShakeManager.SetIntensity(2f);
            Check(Mathf.Approximately(EnemyHitCameraShakeManager.Intensity, 1f), "Intensity maximum was not clamped");
            EnemyHitCameraShakeManager.SetIntensity(float.NaN);
            Check(Mathf.Approximately(EnemyHitCameraShakeManager.Intensity, 1f), "Invalid intensity changed the setting");
            CinemachineCore.CurrentTimeOverride = 101f;

            Hit(enemy, 2f, true);
            Check(Count == 1, "Critical damage must emit exactly one impulse");
            float criticalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 101f + criticalDuration * 0.5f;
            float critical = Sample(Vector3.zero).magnitude;
            Check(Mathf.Abs(critical / normal.magnitude - 2.25f) < 0.01f && criticalDuration > normalDuration, "Critical intensity/duration mismatch");
            Check(Mathf.Approximately(initialHealth - health.CurrentHealth, 4f), "Visual critical changed damage calculation");
            CinemachineCore.CurrentTimeOverride = 102f;
            Sample(Vector3.zero);

            module.PlayShake(false, Vector2.right, true);
            Check(Count == 1, "Normal lethal preview missing");
            float normalLethalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 102f + normalLethalDuration * 0.5f;
            float normalLethal = Sample(Vector3.zero).magnitude;
            Check(Mathf.Abs(normalLethal / normal.magnitude - 3.2f) < 0.01f &&
                Mathf.Abs(normalLethalDuration / normalDuration - 1.7f) < 0.01f,
                "Normal lethal shake did not increase strength and duration");
            CinemachineCore.CurrentTimeOverride = 102.8f;
            Sample(Vector3.zero);

            Hit(enemy, 0f); Hit(enemy, -1f); Hit(enemy, float.NaN);
            health.ApplyHeal(1f);
            Check(Count == 0, "Invalid damage or healing caused a shake");
            module.enabled = false;
            Hit(enemy, 1f);
            Check(Count == 0, "Disabled module emitted an impulse");
            module.enabled = true;
            module.Initialize(enemy);
            Hit(enemy, 1f);
            Check(Count == 1, "Reinitialize / re-enable duplicated subscriptions");
            CinemachineCore.CurrentTimeOverride = 103.5f;
            Sample(Vector3.zero);

            health.ApplyHeal(100f);
            Check(!enemy.IsDead, "Zero-strength test needs a surviving enemy");
            typeof(EnemyHitCameraShakeModule).GetField("_normalStrength", Private).SetValue(module, 0f);
            Hit(enemy, 1f);
            Check(Count == 0, "Zero strength emitted an impulse");
            typeof(EnemyHitCameraShakeModule).GetField("_normalStrength", Private).SetValue(module, 0.08f);
            module.PlayShake(false, new Vector2(float.NaN, 0f));
            Check(Count == 1, "Invalid direction did not fall back safely");
            CinemachineCore.CurrentTimeOverride = 103.56f;
            Check(Sample(Vector3.zero).magnitude > 0.02f, "Fallback direction produced invalid signal");
            CinemachineCore.CurrentTimeOverride = 104.5f;
            Sample(Vector3.zero);

            Hit(enemy, 99999f, true);
            Check(enemy.IsDead && Count == 1, "Lethal hit missing or duplicated");
            float criticalLethalDuration = Events[0].SignalSource.SignalDuration;
            CinemachineCore.CurrentTimeOverride = 104.5f + criticalLethalDuration * 0.5f;
            float criticalLethal = Sample(Vector3.zero).magnitude;
            Check(Mathf.Abs(criticalLethal / critical - 3.2f) < 0.01f &&
                Mathf.Abs(criticalLethalDuration / criticalDuration - 1.7f) < 0.01f,
                "Actual critical lethal hit did not increase strength and duration");
            Object.Destroy(root);
            await Awaitable.NextFrameAsync();
            Check(root == null && Sample(Vector3.zero).magnitude > 0.1f, "Enemy destruction cut off lethal shake");
            CinemachineCore.CurrentTimeOverride = 105.5f;
            Check(Sample(Vector3.zero).sqrMagnitude == 0 && Count == 0, "Lethal impulse leaked");
            camera.InternalUpdateCameraState(Vector3.up, 1f / 60f);
            Check(camera.State.PositionCorrection.sqrMagnitude < 0.000001f, "Camera did not settle after shake");
            return new { passed = true, normalPeakSample = normal.magnitude, criticalPeakSample = critical,
                normalDuration, criticalDuration, normalLethal, normalLethalDuration,
                criticalLethal, criticalLethalDuration, actualCameraOffset, distanceFalloffSample = partial,
                randomDirectionMin = angles.Min(), randomDirectionMax = angles.Max(),
                lethalSurvivesEnemy = true, disabledAndZeroStrengthSuppressed = true, noDuplicateSubscriptions = true,
                globalHalfIntensity = halfIntensity, globalMuteSuppressed = true,
                invalidDamageAndHealingSuppressed = true, cameraSettled = true };
        }
        finally
        {
            Manager.Clear();
            Manager.IgnoreTimeScale = previousTimeMode;
            EnemyHitCameraShakeManager.SetIntensity(previousIntensity);
            CinemachineCore.CurrentTimeOverride = previousClock;
            if (root != null) Object.Destroy(root);
            if (cameraRoot != null) Object.Destroy(cameraRoot);
            if (target != null) Object.Destroy(target);
        }
    }

    public static void StopPlay() => EditorApplication.isPlaying = false;
    public static object RestoreScene()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Stop play first");
        string path = SessionState.GetString(SceneKey, "");
        Check(!string.IsNullOrEmpty(path), "Original scene unknown");
        EditorSceneManager.OpenScene(path, OpenSceneMode.Single);
        SessionState.EraseString(SceneKey);
        return path;
    }
}
