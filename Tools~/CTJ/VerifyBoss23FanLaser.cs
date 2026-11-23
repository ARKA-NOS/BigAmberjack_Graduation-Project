using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.Agent.Enemies.Skills;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Testing.Enemy;
using _Scripts.UI.HitFeedback;
using UnityEditor;
using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyBoss23FanLaser
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    private const string BossPath = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(BossPath);
        var animator = root.GetComponentInChildren<Animator>(true);
        var laser = new SerializedObject(root.GetComponentInChildren<DroneDownwardLaserSkill>(true));
        var warning = (AttackTelegraphUI)laser.FindProperty("telegraphPrefab").objectReferenceValue;
        Check(warning != null && warning.GetComponentInChildren<ConvexPolygonGraphic>(true) != null, "Missing polygon warning");
        var beam = (SpriteRenderer)laser.FindProperty("beam").objectReferenceValue;
        Check(beam.sharedMaterial.shader.name == "Enemy/DroneLaserFade" && !ShaderUtil.ShaderHasError(beam.sharedMaterial.shader), "Beam shader invalid");
        var body = (SpriteRenderer)laser.FindProperty("bodyRenderer").objectReferenceValue;
        var shake = (BossLaserShake)laser.FindProperty("cameraShake").objectReferenceValue;
        Check(body != null && shake != null && beam.sortingLayerID == body.sortingLayerID && beam.sortingOrder < body.sortingOrder,
            "Missing shake reference or beam is above body");
        int markings = 0;
        foreach (var clip in animator.runtimeAnimatorController.animationClips)
        {
            var numberX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("ProcessNumber", typeof(Transform), "m_LocalPosition.x"));
            if (numberX == null) continue;
            var lampX = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("AlarmLamp", typeof(Transform), "m_LocalPosition.x"));
            var numberY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("ProcessNumber", typeof(Transform), "m_LocalPosition.y"));
            var lampY = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("AlarmLamp", typeof(Transform), "m_LocalPosition.y"));
            Check(numberX.length == lampX.length && numberY.length == lampY.length, "Marking track mismatch");
            for (int i = 0; i < numberX.length; i++)
            {
                Check(Mathf.Abs(numberX.keys[i].value - lampX.keys[i].value + 4f / 28f) < 0.00001f &&
                    Mathf.Abs(numberY.keys[i].value - lampY.keys[i].value + 8f / 28f) < 0.00001f,
                    "Number shift or cel motion mismatch");
            }
            markings++;
        }
        Check(markings == 10, "Missing animated marking tracks");
        return new { markings, shaderErrors = false, warningValid = true, bodyOrder = body.sortingOrder, beamOrder = beam.sortingOrder,
            width = laser.FindProperty("width").floatValue, maxSpeed = laser.FindProperty("maxSpeed").floatValue,
            minSpeed = laser.FindProperty("minSpeed").floatValue, fadeOut = laser.FindProperty("fadeOutDuration").floatValue,
            shakeAmplitude = new SerializedObject(shake).FindProperty("amplitude").floatValue, prefabActive = root.activeSelf,
            prefabScale = root.transform.localScale.ToString() };
    }

    public static object Enter()
    {
        Check(!EditorApplication.isPlaying && !EditorApplication.isCompiling && !SceneManager.GetActiveScene().isDirty &&
            SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use saved EnemyTestScene in Edit Mode");
        EditorApplication.EnterPlaymode();
        return "Starting fan laser verification";
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene");
        var roots = SceneManager.GetActiveScene().GetRootGameObjects()
            .Where(root => root.activeSelf && root.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var original in roots) original.SetActive(false);
        var floor = new GameObject("Fan laser test floor") { layer = LayerMask.NameToLayer("Ground") };
        floor.transform.position = Vector3.down * 0.1f;
        floor.AddComponent<BoxCollider2D>().size = new Vector2(30f, 0.2f);
        var cameraRoot = new GameObject("Fan laser preview camera");
        var camera = cameraRoot.AddComponent<Camera>();
        cameraRoot.AddComponent<AudioListener>();
        camera.enabled = false;
        camera.orthographic = true;
        camera.orthographicSize = 4.8f;
        camera.transform.position = new Vector3(0f, 4f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
        var listener = cameraRoot.AddComponent<CinemachineExternalImpulseListener>();
        listener.ChannelMask = BossLaserShake.ImpulseChannel;
        listener.Gain = 1f;
        listener.Use2DDistance = true;
        listener.ReactionSettings = default;
        var sheet = new Texture2D(1280, 720, TextureFormat.RGBA32, false);
        var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(BossPath);
        var results = new List<object>();
        GameObject root = null, player = null;
        object gradient = null;
        try
        {
            for (int scenario = 0; scenario < 6; scenario++)
            {
                player = new GameObject("Fan laser player probe");
                var body = player.AddComponent<BoxCollider2D>();
                body.isTrigger = true; body.size = new Vector2(0.35f, 1.35f); body.offset = Vector2.up * 0.675f;
                var probe = player.AddComponent<EnemyDamageProbe>();
                player.transform.position = Vector3.right * (scenario == 0 ? -7.5f : scenario == 1 ? 7.5f : 0f);
                root = Object.Instantiate(prefab, Vector3.up * 1.05f, Quaternion.identity);
                root.SetActive(true); // The user's prefab is intentionally disabled.
                var boss = root.GetComponent<ProductionControlDrone>();
                var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>();
                var shake = laser.GetComponent<BossLaserShake>();
                if (scenario == 4)
                {
                    var settings = new SerializedObject(laser);
                    settings.FindProperty("width").floatValue = 1.4f;
                    settings.FindProperty("maxSpeed").floatValue = settings.FindProperty("minSpeed").floatValue = 90f;
                    settings.FindProperty("fadeOutDuration").floatValue = 0f;
                    settings.ApplyModifiedPropertiesWithoutUndo();
                }
                var arena = root.GetComponentInChildren<BossArenaModule>();
                var skills = root.GetComponentInChildren<EnemySkillModule>();
                var flight = root.GetComponentInChildren<BossFlightModule>();
                var beam = root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>();
                boss.SetTarget(player.transform);
                root.GetComponentInChildren<NewHealthModule>().ApplyDamage(330f);
                if (scenario > 0)
                {
                    flight.Place(new Vector2(arena.Center.x, arena.FloorY + 6.5f));
                    typeof(ProductionControlDrone).GetField("_state", Private).SetValue(boss,
                        Enum.Parse(typeof(ProductionControlDrone).GetField("_state", Private).FieldType, "AirAttack"));
                    Check(skills.TryUseSpecifiedSkill(laser), "Could not start specified laser");
                }
                await Until(() => laser.IsPreparing, 7f);
                Check(Mathf.Abs(root.transform.position.x - arena.Center.x) < 0.02f &&
                    Mathf.Abs(root.transform.position.y - arena.FloorY - 6.5f) < 0.02f, "Laser did not ascend to center at new height");
                var warning = root.GetComponentsInChildren<AttackTelegraphUI>().Single(view => view.IsVisible);
                Check(warning.GetComponentInChildren<ConvexPolygonGraphic>() != null, "Laser warning is not a sector polygon");
                Canvas.ForceUpdateCanvases();
                var mesh = warning.GetComponentInChildren<ConvexPolygonGraphic>().canvasRenderer.GetMesh();
                Check(mesh != null && mesh.vertexCount > 8, "Fan warning did not render");
                float safeLeft = arena.LaserLeft.x + 1.25f, safeRight = arena.LaserRight.x - 1.25f;
                var points = (List<Vector2>)typeof(DroneDownwardLaserSkill).GetField("_warningPolygon", Private).GetValue(laser);
                Check(points.All(p => p.x >= safeLeft - 0.001f && p.x <= safeRight + 0.001f && p.y >= arena.FloorY - 0.001f),
                    "Warning enters safe strips or below floor");
                Vector2 origin = (Vector2)typeof(DroneDownwardLaserSkill).GetField("_origin", Private).GetValue(laser);
                if (scenario == 0) Capture(camera, sheet, 0);
                object motion = CheckMotion(laser, beam);
                await Until(() => beam.enabled, 2f);
                float firedAt = Time.time;
                float startedAt = (float)typeof(DroneDownwardLaserSkill).GetField("_beamStartedAt", Private).GetValue(laser);
                float duration = (float)typeof(DroneDownwardLaserSkill).GetField("_sweepDuration", Private).GetValue(laser);
                float minStrength = 1f, maxStrength = 0f, impulsePeak = 0f, listenerPeak = 0f;
                int fadeSamples = 0;
                float minAngle = 100f, maxAngle = -100f;
                bool leftCapture = false, centerCapture = false, rightCapture = false;
                if (scenario == 3)
                {
                    Vector2 direction = new Vector2(Mathf.Sin(-10f * Mathf.Deg2Rad), -Mathf.Cos(-10f * Mathf.Deg2Rad));
                    player.transform.position = origin + direction * 3f - Vector2.up * 0.675f;
                    Physics2D.SyncTransforms();
                    typeof(DroneDownwardLaserSkill).GetField("_beamStartedAt", Private).SetValue(laser, Time.time - 0.8f);
                    laser.UpdateSkill();
                    Check(probe.ReceivedHitCount == 1, "Angular sweep skipped a target on a late frame");
                    startedAt = Time.time - 0.8f;
                }
                if (scenario == 5)
                {
                    await Awaitable.NextFrameAsync();
                    skills.StopAttack();
                    Check(!beam.enabled && !shake.IsShaking && !laser.IsUsing, "Cancelled laser leaked beam or shake");
                    results.Add(new { scenario, cancellationClean = true });
                    Object.Destroy(root); Object.Destroy(player); root = player = null;
                    await Awaitable.NextFrameAsync();
                    continue;
                }
                while (laser.IsUsing && Time.time - firedAt < 2f)
                {
                    if (beam.enabled)
                    {
                        float attackElapsed = Time.time - startedAt;
                        if (attackElapsed < duration)
                        {
                            Check(shake.IsShaking && Mathf.Abs(shake.Strength - 1f) < 0.0001f, "Sweep shake strength varies or stops early");
                            minStrength = Mathf.Min(minStrength, shake.Strength);
                            maxStrength = Mathf.Max(maxStrength, shake.Strength);
                            CinemachineImpulseManager.Instance.GetImpulseAt(camera.transform.position, true, BossLaserShake.ImpulseChannel,
                                out var offset, out var rotation);
                            impulsePeak = Mathf.Max(impulsePeak, offset.magnitude);
                            var applied = (Vector3)typeof(CinemachineExternalImpulseListener).GetField("m_ImpulsePosLastFrame", Private).GetValue(listener);
                            listenerPeak = Mathf.Max(listenerPeak, applied.magnitude);
                            Check(Quaternion.Angle(rotation, Quaternion.identity) < 0.001f, "Unexpected shake rotation");
                        }
                        else if (beam.color.a > 0f && beam.color.a < 1f) fadeSamples++;
                        float angle = Mathf.DeltaAngle(0f, beam.transform.eulerAngles.z);
                        minAngle = Mathf.Min(minAngle, angle); maxAngle = Mathf.Max(maxAngle, angle);
                        Bounds sprite = beam.sprite.bounds;
                        foreach (float x in new[] { sprite.min.x, sprite.max.x })
                        foreach (float y in new[] { sprite.min.y, sprite.max.y })
                        {
                            Vector3 corner = beam.transform.TransformPoint(new Vector3(x, y));
                            Check(corner.x >= safeLeft - 0.001f && corner.x <= safeRight + 0.001f, "Visible beam enters safe strip");
                        }
                        var block = new MaterialPropertyBlock(); beam.GetPropertyBlock(block);
                        Vector2 locked = block.GetVector("_BeamOrigin");
                        Check(Vector2.Distance(locked, origin) < 0.001f, "Laser pivot followed the player");
                        float expectedWidth = scenario == 4 ? 1.4f : 0.8f;
                        Check(Mathf.Abs(beam.transform.TransformVector(Vector3.right * beam.sprite.bounds.size.x).magnitude - expectedWidth) < 0.001f,
                            "Beam world width differs from configured width");
                        if (scenario == 0)
                        {
                            float elapsed = Time.time - firedAt;
                            if (!leftCapture && elapsed >= 0.1f) { Capture(camera, sheet, 1); leftCapture = true; }
                            if (!centerCapture && elapsed >= 0.6f) { Capture(camera, sheet, 2); centerCapture = true; }
                            if (!rightCapture && elapsed >= 1.1f) { Capture(camera, sheet, 3); rightCapture = true; }
                        }
                    }
                    await Awaitable.NextFrameAsync();
                }
                Check(!laser.IsUsing && !beam.enabled, "Laser did not finish");
                Check(!shake.IsShaking, "Shake survived laser finish");
                Check(impulsePeak > 0.01f && listenerPeak > 0.01f && minStrength == 1f && maxStrength == 1f,
                    "Continuous shake did not reach the camera listener");
                if (scenario < 3) Check(fadeSamples > 0, "No rendered fade frame before laser disappears");
                if (scenario < 2) Check(probe.ReceivedHitCount == 0, "Player in safe strip was damaged");
                else Check(probe.ReceivedHitCount == 1 && probe.TotalDamage == 18f, "Wrong sweep damage or repeated hit");
                if (scenario < 3) Check(minAngle < -55f && maxAngle > 55f, "Beam did not sweep both sides");
                results.Add(new { scenario, minAngle, maxAngle, hits = probe.ReceivedHitCount, damage = probe.TotalDamage,
                    motion, minStrength, maxStrength, impulsePeak, listenerPeak, fadeSamples });
                if (scenario == 0)
                {
                    listener.enabled = false;
                    gradient = CheckGradient(camera, laser, beam, origin, arena.FloorY);
                    listener.enabled = true;
                    sheet.Apply(); System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23FanLaserPreview.png", sheet.EncodeToPNG());
                }
                Object.Destroy(root); Object.Destroy(player); root = player = null;
                await Awaitable.NextFrameAsync();
            }
            return new { results, gradient, prefabScale = prefab.transform.localScale.ToString(),
                prefabActive = prefab.activeSelf, preview = "Tools~/CTJ/Boss23FanLaserPreview.png" };
        }
        finally
        {
            if (root != null) Object.Destroy(root);
            if (player != null) Object.Destroy(player);
            Object.Destroy(floor); Object.Destroy(cameraRoot); Object.Destroy(sheet);
            foreach (var original in roots) if (original != null) original.SetActive(true);
        }
    }

    private static object CheckMotion(DroneDownwardLaserSkill laser, SpriteRenderer beam)
    {
        var settings = new SerializedObject(laser);
        float duration = (float)typeof(DroneDownwardLaserSkill).GetField("_sweepDuration", Private).GetValue(laser);
        float start = settings.FindProperty("maxSpeed").floatValue, end = settings.FindProperty("minSpeed").floatValue;
        var evaluate = typeof(DroneDownwardLaserSkill).GetMethod("EvaluateSweepAngle", Private);
        float Angle(float time) => (float)evaluate.Invoke(laser, new object[] { time });
        float step = duration * 0.0005f;
        float earlySpeed = (Angle(step) - Angle(0f)) / step;
        float lateSpeed = (Angle(duration) - Angle(duration - step)) / step;
        Check(Mathf.Abs(earlySpeed - start) < 0.2f && Mathf.Abs(lateSpeed - end) < 0.2f, "Angular speed endpoints incorrect");
        float previousSpeed = float.PositiveInfinity;
        for (int i = 1; i <= 100; i++)
        {
            float speed = (Angle(duration * i / 100f) - Angle(duration * (i - 1) / 100f)) * 100f / duration;
            Check(speed <= previousSpeed + 0.01f && speed >= end - 0.01f, "Sweep speed is not smoothly decreasing");
            previousSpeed = speed;
        }
        Color tint = beam.color;
        var update = typeof(DroneDownwardLaserSkill).GetMethod("UpdateBeam", Private);
        float fade = settings.FindProperty("fadeOutDuration").floatValue;
        var alpha = new List<float>();
        foreach (float t in new[] { 0f, 0.25f, 0.5f, 0.75f, 1f })
        {
            update.Invoke(laser, new object[] { 0f, duration + (fade > 0f ? fade * t : t * 0.01f) });
            alpha.Add(beam.color.a);
        }
        Check(alpha[0] > 0.99f && alpha[4] < 0.001f, "Fade endpoints incorrect");
        if (fade > 0f) Check(alpha[1] > alpha[2] && alpha[2] > alpha[3] && alpha[3] > 0f, "Fade is abrupt");
        beam.color = tint;
        return new { duration, earlySpeed, lateSpeed, fadeSeconds = fade, fadeAlpha = alpha };
    }

    private static object CheckGradient(Camera camera, DroneDownwardLaserSkill laser, SpriteRenderer beam, Vector2 origin, float floor)
    {
        int mask = camera.cullingMask, layer = beam.gameObject.layer;
        Color background = camera.backgroundColor;
        float size = camera.orthographicSize;
        Vector3 position = camera.transform.position;
        var rt = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
        var texture = new Texture2D(512, 512, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            beam.gameObject.layer = 30; beam.enabled = true;
            camera.cullingMask = 1 << 30; camera.backgroundColor = Color.clear;
            camera.orthographicSize = 4.5f; camera.transform.position = new Vector3(origin.x, (origin.y + floor) * 0.5f, -10f);
            typeof(DroneDownwardLaserSkill).GetMethod("UpdateBeam", Private).Invoke(laser, new object[] { 0f, 0.4f });
            camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 512, 512), 0, 0); texture.Apply();
            float near = Alpha(new Vector2(origin.x, floor + 0.1f));
            float middle = Alpha(new Vector2(origin.x, floor + 0.5f));
            float core = Alpha(new Vector2(origin.x, floor + 1.3f));
            Check(near < middle && middle < core && core > 0.8f, $"Floor gradient not rendered: {near}/{middle}/{core}");
            return new { near, middle, core, shaderErrors = ShaderUtil.ShaderHasError(beam.sharedMaterial.shader) };

            float Alpha(Vector2 world)
            {
                Vector3 uv = camera.WorldToViewportPoint(world);
                return texture.GetPixel(Mathf.Clamp((int)(uv.x * 512), 0, 511), Mathf.Clamp((int)(uv.y * 512), 0, 511)).a;
            }
        }
        finally
        {
            beam.enabled = false; beam.gameObject.layer = layer;
            camera.cullingMask = mask; camera.backgroundColor = background; camera.orthographicSize = size;
            camera.transform.position = position; camera.targetTexture = null; RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt); Object.Destroy(texture);
        }
    }

    private static void Capture(Camera camera, Texture2D sheet, int index)
    {
        var rt = RenderTexture.GetTemporary(640, 360, 24);
        var texture = new Texture2D(640, 360, TextureFormat.RGBA32, false);
        var previous = RenderTexture.active;
        try
        {
            Canvas.ForceUpdateCanvases(); camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
            texture.ReadPixels(new Rect(0, 0, 640, 360), 0, 0); texture.Apply();
            sheet.SetPixels32(index % 2 * 640, (1 - index / 2) * 360, 640, 360, texture.GetPixels32());
        }
        finally
        {
            camera.targetTexture = null; RenderTexture.active = previous;
            RenderTexture.ReleaseTemporary(rt); Object.Destroy(texture);
        }
    }

    private static async Task Until(Func<bool> condition, float seconds)
    {
        float deadline = Time.realtimeSinceStartup + seconds;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), "Fan laser verification timed out");
    }
    private static void Check(bool condition, string message) { if (!condition) throw new Exception(message); }
}
