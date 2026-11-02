using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using DevLib.BattleSystem;
using Unity.Cinemachine;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyBossLaserAsset
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        private const string Art = "Assets/CTJ/Assets/Laser/laser_beam.png";

        public static object RunEdit()
        {
            AssetDatabase.ImportAsset(Prefab, ImportAssetOptions.ForceUpdate);
            var boss = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Prefab);
            var laser = boss.GetComponentInChildren<BossLaserSkill>();
            var settings = new SerializedObject(laser);
            var shape = (BossSpriteBeam)settings.FindProperty("spriteBeam").objectReferenceValue;
            var beam = shape.GetComponent<SpriteRenderer>();
            var sprite = AssetDatabase.LoadAllAssetsAtPath(Art).OfType<Sprite>().Single(s => s.name == "laser_beam_0");
            Check(beam.sprite == sprite && beam.color == Color.white && !beam.flipY, "Native laser sprite / tint / orientation");
            Check(new SerializedObject(shape).FindProperty("uniformScale").floatValue == 0.75f && settings.FindProperty("beamWidth") == null, "2.5x uniform scale / no independent width");
            Check(laser.SafeZoneWidth == 1.25f, "2.5 characters at 0.5 world width");
            var player = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Lrw/GameModule/Player/Prefabs/Player.prefab");
            Check(player.GetComponent<CapsuleCollider2D>().size.x == 0.5f && player.transform.localScale.x == 1f, "Shared player width reference (read only)");
            Check(new SerializedObject(boss.GetComponentInChildren<BossLaserShake>()).FindProperty("amplitude").floatValue == 0.8f, "Stronger shake");
            Check(new SerializedObject(boss).FindProperty("laserSweepSpeed").floatValue == 27f, "Traversal speed reduced by 10 percent");
            Check(Mathf.Approximately(sprite.rect.height / sprite.pixelsPerUnit, 12.09f), "Expected original asset length");
            Check(Mathf.Abs(shape.HeightFromFloor - 8.988725f) < 0.001f && shape.GetComponent<PolygonCollider2D>().pathCount == 1, "Raised firing height / single core polygon");
            Vector2[] core = shape.GetComponent<PolygonCollider2D>().GetPath(0);
            Check(core.Length == 4 && Vector2.Distance(core[0], new Vector2(-2.578f, 4.1106f)) < 0.001f &&
                Vector2.Distance(core[2], new Vector2(3.0936f, -5.0778f)) < 0.001f, "Serialized collider matches runtime core");
            foreach (Transform child in boss.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
            foreach (Component component in boss.GetComponentsInChildren<Component>(true))
            {
                var p = new SerializedObject(component).GetIterator();
                while (p.Next(true)) if (p.propertyType == SerializedPropertyType.ObjectReference)
                    Check(p.objectReferenceValue != null || p.objectReferenceInstanceIDValue == 0, "Missing reference: " + component.name + "/" + p.propertyPath);
            }
            return new { passed = true, sprite = sprite.name, nativeLength = sprite.rect.height / sprite.pixelsPerUnit, pixelsPerUnit = sprite.pixelsPerUnit,
                pivot = sprite.pivot.ToString() };
        }

        public static Task<object> RunPlay() => RunScenario(false);
        public static Task<object> RunSafeSpaces() => RunScenario(true);
        private static async Task<object> RunScenario(bool staySafe)
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play required");
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            var stage = new GameObject("Laser asset verification"); stage.SetActive(false);
            RenderTexture rt = null;
            float maxCameraOffset = 0f;
            Action<ScriptableRenderContext, Camera> observeCamera = null;
            try
            {
                var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Prefab), new Vector3(8000f, 5f), Quaternion.identity, stage.transform);
                Set(boss, "firstVolleyDelay", 999f);
                boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                var laser = boss.GetComponentInChildren<BossLaserSkill>();
                var settings = new SerializedObject(laser);
                var shape = (BossSpriteBeam)settings.FindProperty("spriteBeam").objectReferenceValue;
                var beam = shape.GetComponent<SpriteRenderer>();
                var firePoint = (Transform)settings.FindProperty("firePoint").objectReferenceValue;
                var shake = boss.GetComponentInChildren<BossLaserShake>();
                var target = new GameObject("Laser test target"); target.transform.SetParent(stage.transform);
                target.layer = 7; target.transform.position = new Vector3(8000f, 0.8f);
                var bodyCollider = target.AddComponent<BoxCollider2D>(); bodyCollider.isTrigger = true;
                bodyCollider.size = new Vector2(0.5f, 0.95f);
                var probe = target.AddComponent<EnemyDamageProbe>();
                var solid = AssetDatabase.LoadAssetAtPath<Sprite>(AssetDatabase.GUIDToAssetPath("d458cb5e3a4c2bf46bcfea7f0950736b"));
                var marker = target.AddComponent<SpriteRenderer>(); marker.sprite = solid; marker.color = Color.green;
                marker.sharedMaterial = beam.sharedMaterial; marker.sortingOrder = 1;
                marker.drawMode = SpriteDrawMode.Sliced; marker.size = bodyCollider.size;
                Box(stage, solid, beam.sharedMaterial, "Floor", new Vector2(8000f, -2f), new Vector2(26f, 4f));
                Box(stage, solid, beam.sharedMaterial, "LeftWall", new Vector2(7988f, 6f), new Vector2(2f, 12f));
                Box(stage, solid, beam.sharedMaterial, "RightWall", new Vector2(8012f, 6f), new Vector2(2f, 12f));
                var cameraGo = new GameObject("Laser preview camera"); cameraGo.transform.SetParent(stage.transform); cameraGo.tag = "MainCamera";
                cameraGo.transform.position = new Vector3(8000f, 5f, -10f);
                Vector3 baseCameraPosition = cameraGo.transform.position;
                var camera = cameraGo.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 7f;
                camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f);
                camera.allowHDR = false; camera.allowMSAA = false;
                cameraGo.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                var listener = cameraGo.AddComponent<CinemachineExternalImpulseListener>();
                listener.ChannelMask = BossLaserShake.ImpulseChannel; listener.Gain = 1f;
                listener.Use2DDistance = true; listener.UseLocalSpace = false;
                listener.ReactionSettings = default;
                // 리스너가 Update에서 이전 흔들림을 되돌린 뒤 LateUpdate에서 적용하므로 렌더링 시점에 측정합니다.
                observeCamera = (context, renderedCamera) =>
                {
                    if (renderedCamera == camera && laser != null && laser.IsFiring)
                        maxCameraOffset = Mathf.Max(maxCameraOffset, Vector3.Distance(cameraGo.transform.position, baseCameraPosition));
                };
                RenderPipelineManager.endCameraRendering += observeCamera;
                rt = new RenderTexture(1280, 720, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
                stage.SetActive(true); boss.SetTarget(target.transform);
                await Until(() => boss.CurrentState == RobotEyeBossState.Waiting);
                var arena = boss.GetModule<BossArenaModule>();
                Check(Mathf.Approximately(arena.LaserLeft.y - arena.FloorY, shape.HeightFromFloor), "Boss height follows original shape");
                boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, boss.transform.position, Vector2.left, Vector2.right);
                int prepared = -1; int[] hits = new int[3]; int previousHits = 0;
                bool[] seen = new bool[3]; bool[] movedWithoutBeam = new bool[3];
                bool[] fadeInSeen = new bool[3], fullBeamSeen = new bool[3], fadeOutSeen = new bool[3], weakShakeSeen = new bool[3];
                float[] lastFadeAlpha = { 1f, 1f, 1f };
                float[] firstFireX = new float[3];
                float safeUntil = float.PositiveInfinity;
                string preview = null;
                DateTime deadline = DateTime.UtcNow.AddSeconds(40);
                while (!boss.HasFinishedLaserPhase)
                {
                    Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Laser phase timeout: " + boss.CurrentState);
                    int pass = boss.CompletedTraversals;
                    if (laser.IsPreparing && pass != prepared)
                    {
                        if (prepared >= 0) hits[prepared] = probe.ReceivedHitCount - previousHits;
                        previousHits = probe.ReceivedHitCount; prepared = pass;
                        Bounds safeSpace = laser.SafeZoneBounds;
                        Vector2 safe = new Vector2(pass % 2 == 0 ? safeSpace.max.x - 0.27f : safeSpace.min.x + 0.27f, arena.FloorY + 0.5f);
                        target.transform.position = safe;
                        safeUntil = float.PositiveInfinity;
                        Check(Mathf.Abs(safeSpace.size.x - 1.25f) < 0.001f && !Contains(laser.DangerPolygon, safe), "Wall safe strip is empty");
                        Check(laser.DangerPolygon.All(p => pass % 2 == 0 ? p.x >= safeSpace.max.x - 0.001f : p.x <= safeSpace.min.x + 0.001f), "No warning inside safe strip");
                        Check(Contains(laser.DangerPolygon, new Vector2(arena.Center.x, arena.FloorY + 0.8f)), "Warning reaches floor target");
                        Check(!beam.enabled, "No beam before firing");
                    }
                    if (laser.IsTraversalReady && !laser.IsFiring)
                    {
                        Check(!beam.enabled && !shape.GetComponent<PolygonCollider2D>().enabled && !shake.IsShaking, "Movement before clearance has no beam / collider / shake");
                        Check(probe.ReceivedHitCount == previousHits, "No damage while leaving wall");
                        if (Mathf.Abs(boss.transform.position.x - arena.GetLaserStart(pass).x) > 0.1f) movedWithoutBeam[pass] = true;
                    }
                    if (laser.IsFiring)
                    {
                        if (!seen[pass])
                        {
                            safeUntil = Time.time + 0.12f;
                            firstFireX[pass] = boss.transform.position.x;
                            Check(shake.Strength > 0.9f, "Shake starts strongly");
                        }
                        seen[pass] = true;
                        if (staySafe || pass == 2 || Time.time <= safeUntil)
                            Check(probe.ReceivedHitCount == previousHits, "Player remains safe even as beam starts");
                        else target.transform.position = new Vector3(arena.Center.x, arena.FloorY + 0.5f);
                        Check(Mathf.Abs(laser.VisualBeamLength - 9.0675f) < 0.001f, "Image height is exactly 2.5x");
                        Check(Mathf.Abs(shape.FloorContactY - arena.FloorY) < 0.001f, "Visible flat impact base touches floor");
                        Check(Mathf.Abs(beam.transform.lossyScale.x - 0.75f) < 0.001f && Mathf.Abs(beam.transform.lossyScale.y - 0.75f) < 0.001f,
                            "Identical X/Y scale preserves body and flare");
                        Check(Quaternion.Angle(beam.transform.rotation, Quaternion.identity) < 0.01f && beam.flipX == (pass % 2 == 1), "No added rotation / horizontal mirror");
                        Check(shake.IsShaking, "Shake runs during firing");
                        if (shape.Opacity > 0f && shape.Opacity < 1f)
                        {
                            fadeInSeen[pass] = true;
                            Check(!shape.CanDamage, "Faint starting beam cannot damage");
                        }
                        if (shape.CanDamage) { fullBeamSeen[pass] = true; Check(shape.Opacity == 1f, "Damage starts at full opacity"); }
                        if (shake.Strength < 0.3f) weakShakeSeen[pass] = true;
                        Check(pass % 2 == 0 ? beam.bounds.min.x >= laser.SafeZoneBounds.max.x - 0.001f : beam.bounds.max.x <= laser.SafeZoneBounds.min.x + 0.001f,
                            "Entire image including bottom flare stays outside safe space");
                        Check(Mathf.Abs(boss.transform.position.y - arena.LaserLeft.y) < 0.01f, "Lower traversal height stays fixed");
                        Check(laser.BeamDirection.y < 0f && Mathf.Sign(laser.BeamDirection.x) == (pass % 2 == 0 ? 1f : -1f), "Original slant / alternates");
                        Check(laser.DangerPolygon.All(p => p.y >= arena.FloorY - 0.001f), "Warning clipped to arena floor");
                        if (preview == null && pass == 0 && boss.transform.position.x >= arena.LaserLeft.x + 3f)
                            preview = SavePreview(rt);
                    }
                    if (shape.IsFadingOut)
                    {
                        int ended = boss.CompletedTraversals - 1;
                        Check(ended >= 0 && !shape.CanDamage && !shape.GetComponent<PolygonCollider2D>().enabled, "Fade out is visual only");
                        Check(shape.Opacity <= lastFadeAlpha[ended] + 0.0001f, "Fade out alpha decreases");
                        lastFadeAlpha[ended] = shape.Opacity;
                        if (shape.Opacity > 0f && shape.Opacity < 1f) fadeOutSeen[ended] = true;
                    }
                    await Task.Yield();
                }
                hits[prepared] = probe.ReceivedHitCount - previousHits;
                Check(seen.All(v => v) && boss.CompletedTraversals == 3 && !beam.enabled, "Three passes then beam off");
                Check(movedWithoutBeam.All(v => v), "Boss travels from wall before firing on all passes");
                Check(fadeInSeen.All(v => v) && fullBeamSeen.All(v => v) && fadeOutSeen.All(v => v) && weakShakeSeen.All(v => v),
                    "Each pass fades in / has active damage / fades out / weakens shake");
                Check(staySafe ? hits.All(h => h == 0) : hits[0] == 1 && hits[1] == 1 && hits[2] == 0, "Damage once per traversal / safe area: " + string.Join(",", hits));
                await Frames(3);
                Check(maxCameraOffset > 0.2f && !shake.IsShaking && Vector3.Distance(cameraGo.transform.position, baseCameraPosition) < 0.001f,
                    "Strong camera movement only while firing / restored afterwards: " + maxCameraOffset);
                Check(!CinemachineImpulseManager.Instance.GetImpulseAt(baseCameraPosition, true, BossLaserShake.ImpulseChannel, out _, out _), "No remaining laser impulse");
                Check(errors.Count == 0, string.Join(" | ", errors));
                if (staySafe) return new { passed = true, staySafe, hits, firstFireX, safeWidth = laser.SafeZoneWidth, movedWithoutBeam,
                    fadeInSeen, fullBeamSeen, fadeOutSeen, weakShakeSeen, maxCameraOffset, errors };
                var targetCollider = target.GetComponent<BoxCollider2D>(); targetCollider.size = Vector2.one * 0.03f;
                var overlaps = new List<Collider2D>();
                Vector2 origin = new Vector2(arena.Center.x, arena.FloorY + shape.HeightFromFloor);
                shape.Configure(true); shape.Show(origin);
                await Until(() => shape.CanDamage);
                target.transform.position = SpritePoint(beam, new Vector2(0.97f, 0.85f));
                shape.OverlapAt(origin, overlaps); Check(!overlaps.Contains(targetCollider), "Transparent image corner is not a hitbox");
                target.transform.position = SpritePoint(beam, new Vector2(0.47f, 0.55f));
                shape.OverlapAt(origin, overlaps); Check(overlaps.Contains(targetCollider), "Visible beam body hits");
                target.transform.position = SpritePoint(beam, new Vector2(0.6f, 0.06f));
                shape.OverlapAt(origin, overlaps); Check(!overlaps.Contains(targetCollider), "Floor contact tip is decorative only");
                target.transform.position = SpritePoint(beam, new Vector2(0.2f, 0.1f));
                shape.OverlapAt(origin, overlaps); Check(!overlaps.Contains(targetCollider), "Wide bottom splash is decorative only");
                shape.Configure(false); shape.Show(origin);
                target.transform.position = SpritePoint(beam, new Vector2(0.47f, 0.55f));
                shape.OverlapAt(origin, overlaps); Check(overlaps.Contains(targetCollider), "Mirrored white core hits");
                target.transform.position = SpritePoint(beam, new Vector2(0.6f, 0.06f));
                shape.OverlapAt(origin, overlaps); Check(!overlaps.Contains(targetCollider), "Mirrored floor tip is decorative only");
                shape.FadeOut();
                Check(!shape.CanDamage && shape.IsFadingOut, "Ending immediately removes damage");
                shape.Hide(); Check(!beam.enabled && !shape.IsFadingOut && shape.Opacity == 0f, "Forced cleanup during fade");
                shape.Configure(true); shape.Show(origin); await Until(() => shape.CanDamage);
                var shapeSettings = new SerializedObject(shape); shapeSettings.FindProperty("uniformScale").floatValue = 0.9f;
                shapeSettings.ApplyModifiedPropertiesWithoutUndo();
                laser.MatchArenaHeight(arena); origin.y = arena.LaserLeft.y;
                shape.Show(origin); Check(Mathf.Abs(shape.FloorContactY - arena.FloorY) < 0.001f, "Scaling changes boss height, not image aspect");
                shapeSettings.Update(); shapeSettings.FindProperty("uniformScale").floatValue = 0.75f; shapeSettings.ApplyModifiedPropertiesWithoutUndo();
                laser.MatchArenaHeight(arena); origin.y = arena.LaserLeft.y;
                boss.GetModule<BossFlightModule>().Place(origin); shape.Show(origin);
                camera.orthographicSize = 6.2f; cameraGo.transform.position = new Vector3(origin.x + 1.25f, arena.FloorY + 4.8f, -10f);
                target.transform.position = new Vector3(origin.x + 4.8f, arena.FloorY + 0.6f);
                string closePreview;
                var closeRT = new RenderTexture(512, 640, 24, RenderTextureFormat.ARGB32);
                try
                {
                    camera.targetTexture = closeRT;
                    await Frames(3); closePreview = SavePreview(closeRT, "BossLaserShape_Close.png");
                }
                finally { camera.targetTexture = rt; closeRT.Release(); Object.Destroy(closeRT); }
                shape.Hide();
                Check(errors.Count == 0, string.Join(" | ", errors));
                // 좁은 방에서는 원본 형태나 안전 폭을 압축하지 않고 경고를 비웁니다.
                laser.ConfigureTraversal(new Bounds(new Vector3(origin.x, origin.y * 0.5f), new Vector3(1f, origin.y)),
                    new Vector2(origin.x - 0.5f, origin.y), new Vector2(origin.x + 0.5f, origin.y));
                Check(laser.DangerPolygon.Count == 0, "Tiny room keeps safe space instead of shrinking it");
                return new { passed = true, uniformScale = 0.75f, spriteSize = shape.WorldSize.ToString(),
                    firingHeightFromFloor = shape.HeightFromFloor, hits, firstFireX, safeWidth = laser.SafeZoneWidth, movedWithoutBeam,
                    fadeInSeen, fullBeamSeen, fadeOutSeen, weakShakeSeen, maxCameraOffset, preview, closePreview, errors };
            }
            finally
            {
                if (observeCamera != null) RenderPipelineManager.endCameraRendering -= observeCamera;
                stage.SetActive(false); Object.Destroy(stage); Application.logMessageReceived -= capture;
                if (rt != null) { rt.Release(); Object.Destroy(rt); }
            }
        }

        private static bool Contains(IReadOnlyList<Vector2> polygon, Vector2 point)
        {
            bool inside = false;
            for (int i = 0, j = polygon.Count - 1; i < polygon.Count; j = i++)
            {
                Vector2 a = polygon[i], b = polygon[j];
                if ((a.y > point.y) != (b.y > point.y) && point.x < (b.x - a.x) * (point.y - a.y) / (b.y - a.y) + a.x) inside = !inside;
            }
            return inside;
        }
        private static void Box(GameObject root, Sprite sprite, Material material, string name, Vector2 position, Vector2 size)
        {
            var go = new GameObject(name); go.transform.SetParent(root.transform); go.transform.position = position;
            go.transform.localScale = new Vector3(size.x, size.y, 1f);
            var renderer = go.AddComponent<SpriteRenderer>(); renderer.sprite = sprite; renderer.sharedMaterial = material;
            renderer.color = new Color(0.25f, 0.3f, 0.4f); renderer.sortingOrder = 0;
        }
        private static Vector3 SpritePoint(SpriteRenderer renderer, Vector2 uv)
        {
            Vector2 local = (Vector2.Scale(uv, renderer.sprite.rect.size) - renderer.sprite.pivot) / renderer.sprite.pixelsPerUnit;
            if (renderer.flipX) local.x *= -1f;
            return renderer.transform.TransformPoint(local);
        }
        private static string SavePreview(RenderTexture rt, string filename = "BossLaserAsset_Preview.png")
        {
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false); var previous = RenderTexture.active;
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                string path = Application.dataPath + "/CTJ/Tools~/" + filename;
                File.WriteAllBytes(path, texture.EncodeToPNG()); return path;
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }
        private static void Set(Object obj, string field, float value) { var s = new SerializedObject(obj); s.FindProperty(field).floatValue = value; s.ApplyModifiedPropertiesWithoutUndo(); }
        private static async Task Frames(int count) { int end = Time.frameCount + count; await Until(() => Time.frameCount >= end); }
        private static async Task Until(Func<bool> condition)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(12);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Verification timeout"); await Task.Yield(); }
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
    }
}
