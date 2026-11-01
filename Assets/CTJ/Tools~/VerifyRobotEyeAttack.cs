using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Enemies.Skills;
using CTJ.Testing;
using CTJ.UI;
using UnityEditor;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyRobotEyeAttack
    {
        private const string BossPath = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        private const string ProjectilePath = "Assets/CTJ/Prototypes/Bosses/RobotEyeProjectile.prefab";
        private const string WarningPath = "Assets/CTJ/Prototypes/Bosses/RadialWarningCanvas.prefab";
        private const string Sheet = "Assets/CTJ/Assets/Effect and Bullet 16x16/Fire Effect and Bullet 16x16.png";

        public static object Configure()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            SliceFrames();
            GameObject projectile = PrefabUtility.LoadPrefabContents(ProjectilePath);
            try
            {
                var original = projectile.GetComponent<SpriteRenderer>(); original.enabled = false;
                Transform child = projectile.transform.Find("Visual");
                if (child == null) { child = new GameObject("Visual").transform; child.SetParent(projectile.transform, false); }
                child.localScale = Vector3.one * 3f;
                var visual = child.GetComponent<SpriteRenderer>();
                if (visual == null) visual = child.gameObject.AddComponent<SpriteRenderer>();
                visual.sharedMaterial = original.sharedMaterial; visual.color = Color.white;
                visual.sortingLayerID = original.sortingLayerID; visual.sortingOrder = original.sortingOrder;
                visual.sprite = Frames("CTJ_FireFlight_")[0];
                var animation = projectile.GetComponent<EnemyProjectileVisual>();
                if (animation == null) animation = projectile.AddComponent<EnemyProjectileVisual>();
                var so = new SerializedObject(animation);
                so.FindProperty("visual").objectReferenceValue = visual;
                SetFrames(so.FindProperty("flightFrames"), Frames("CTJ_FireFlight_"));
                SetFrames(so.FindProperty("impactFrames"), Frames("CTJ_FireImpact_"));
                so.FindProperty("flightFramesPerSecond").floatValue = 12f;
                so.FindProperty("impactFramesPerSecond").floatValue = 18f;
                so.FindProperty("impactScale").floatValue = 1.4f;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(projectile, ProjectilePath);
            }
            finally { PrefabUtility.UnloadPrefabContents(projectile); }
            GameObject warning = PrefabUtility.LoadPrefabContents(WarningPath);
            try
            {
                var ui = warning.GetComponent<AttackTelegraphUI>(); var so = new SerializedObject(ui);
                var old = so.FindProperty("areaImage").objectReferenceValue as Graphic;
                GameObject area = old.gameObject;
                if (!(old is RadialLanesGraphic)) Object.DestroyImmediate(old);
                var lanes = area.GetComponent<RadialLanesGraphic>();
                if (lanes == null) lanes = area.AddComponent<RadialLanesGraphic>();
                lanes.raycastTarget = false; so.Update(); so.FindProperty("areaImage").objectReferenceValue = lanes;
                so.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(warning, WarningPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(warning); }
            return RunEdit();
        }

        private static void SliceFrames()
        {
            var importer = (TextureImporter)AssetImporter.GetAtPath(Sheet);
            var factory = new SpriteDataProviderFactories(); factory.Init();
            var provider = factory.GetSpriteEditorDataProviderFromObject(importer); provider.InitSpriteEditorDataProvider();
            var rects = provider.GetSpriteRects().ToList();
            // 기존 자동 슬라이스와 ID를 보존하고 필요한 일정 크기 프레임만 추가합니다.
            Add("CTJ_FireFlight_", 24, 0, 5);
            // 이번 작업의 프레임 검토용 임시 슬라이스만 정리합니다. 원래 에셋의 슬라이스는 유지합니다.
            rects.RemoveAll(r => r.name.StartsWith("CTJ_FireBurstA_") || r.name.StartsWith("CTJ_FireBurstB_"));
            int[] columns = { 30, 33, 30, 31, 32, 33 };
            int[] rows = { 0, 0, 1, 1, 1, 1 };
            for (int i = 0; i < columns.Length; i++) Add("CTJ_FireImpact_", columns[i], rows[i], 1, i);
            provider.SetSpriteRects(rects.ToArray());
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>().SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply(); importer.filterMode = FilterMode.Point; importer.textureCompression = TextureImporterCompression.Uncompressed;
            var settings = new TextureImporterSettings(); importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(settings); importer.SaveAndReimport();
            void Add(string prefix, int column, int row, int count, int firstIndex = 0)
            {
                for (int i = 0; i < count; i++)
                {
                    string name = prefix + (firstIndex + i);
                    var rect = rects.FirstOrDefault(r => r.name == name);
                    if (rect == null) { rect = new SpriteRect { name = name, spriteID = GUID.Generate() }; rects.Add(rect); }
                    rect.rect = new Rect((column + i) * 16, 208 - (row + 1) * 16, 16, 16);
                    rect.alignment = SpriteAlignment.Center; rect.pivot = Vector2.one * 0.5f;
                }
            }
        }

        public static object RunEdit()
        {
            foreach (string path in new[] { BossPath, ProjectilePath, WarningPath })
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>(path);
                foreach (Transform child in root.GetComponentsInChildren<Transform>(true))
                    Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script " + child.name);
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true))
                        if (property.propertyType == SerializedPropertyType.ObjectReference)
                            Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                                "Missing reference " + component.name + "/" + property.propertyPath);
                }
            }
            Check(AssetDatabase.LoadAssetAtPath<GameObject>(WarningPath).GetComponentInChildren<RadialLanesGraphic>() != null, "Radial lanes graphic");
            var shot = AssetDatabase.LoadAssetAtPath<EnemyProjectile>(ProjectilePath);
            Check(shot.GetComponent<EnemyProjectileVisual>() != null && Mathf.Approximately(shot.Diameter, 0.3f) && shot.Speed == 5f && shot.Lifetime == 5f,
                "Optional animation / same damage shape and flight settings");
            foreach (var sprite in Frames("CTJ_Fire")) Check(sprite.rect.size == Vector2.one * 16f && sprite.pivot == Vector2.one * 8f, "Uniform sprite frames");
            return new { passed = true, travelDistance = shot.TravelDistance, width = shot.Diameter, flightFrames = 5, impactFrames = 6 };
        }

        public static async Task<object> RenderFramePreview()
        {
            RequirePlay(); var stage = new GameObject("Fire sheet preview");
            RenderTexture rt = new RenderTexture(768, 384, 24, RenderTextureFormat.ARGB32);
            try
            {
                var material = AssetDatabase.LoadAssetAtPath<GameObject>(ProjectilePath).GetComponent<SpriteRenderer>().sharedMaterial;
                int row = 0;
                foreach (string prefix in new[] { "CTJ_FireFlight_", "CTJ_FireImpact_" })
                {
                    int column = 0;
                    foreach (Sprite sprite in Frames(prefix))
                    {
                        var go = new GameObject(sprite.name); go.transform.SetParent(stage.transform); go.layer = 31;
                        go.transform.position = new Vector3(8000 + column * 0.22f, -row * 0.22f);
                        var sr = go.AddComponent<SpriteRenderer>(); sr.sprite = sprite; sr.sharedMaterial = material; column++;
                    }
                    row++;
                }
                var camera = Camera(stage, new Vector3(8000.55f, -0.22f, -10f), 0.34f, rt);
                await TickFrames(3);
                return new { path = Save(rt, "FireFrames.png"), rows = "flight / impact, left to right" };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); rt.Release(); Object.Destroy(rt); }
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            try
            {
                object warning = await CheckVolley();
                var results = new List<object>();
                foreach (string scenario in new[] { "target", "wall", "floor", "insideWall", "lifetime" })
                    results.Add(await CheckProjectile(scenario));
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, warning, results, errors };
            }
            finally { Application.logMessageReceived -= capture; }
        }

        private static async Task<object> CheckVolley()
        {
            var stage = new GameObject("Radial warning verification"); stage.SetActive(false);
            RenderTexture rt = null;
            try
            {
                var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(BossPath), new Vector3(9000f, 0f), Quaternion.identity, stage.transform);
                var so = new SerializedObject(boss); so.FindProperty("firstVolleyDelay").floatValue = 0f; so.ApplyModifiedPropertiesWithoutUndo();
                boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                var target = new GameObject("Volley target"); target.transform.SetParent(stage.transform);
                target.transform.position = new Vector3(9010f, -2f); target.AddComponent<EnemyDamageProbe>();
                stage.SetActive(true); boss.SetTarget(target.transform);
                var skill = boss.GetComponentInChildren<BossRadialVolleySkill>();
                await Until(() => skill.IsPreparing);
                var line = (AttackTelegraphUI)typeof(EnemyAttackSkill).GetProperty("Telegraph", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(skill);
                await TickFrames(2);
                var graphic = line.GetComponentInChildren<RadialLanesGraphic>();
                Canvas.ForceUpdateCanvases();
                var mesh = Object.Instantiate(graphic.canvasRenderer.GetMesh());
                try
                {
                    Check(mesh.vertexCount == 48 && mesh.triangles.Length == 72, "Twelve rendered warning quads");
                    Vector3[] vertices = mesh.vertices;
                    for (int i = 0; i < 12; i++)
                    {
                        Vector3 start = (vertices[i * 4] + vertices[i * 4 + 1]) * 0.5f;
                        Vector3 end = (vertices[i * 4 + 2] + vertices[i * 4 + 3]) * 0.5f;
                        Vector2 expected = new Vector2(Mathf.Cos(i * Mathf.PI / 6f), Mathf.Sin(i * Mathf.PI / 6f));
                        Check(Vector2.Angle(end - start, expected) < 0.1f && Mathf.Abs(Vector3.Distance(start, end) - 25.3f) < 0.001f,
                            "Full 25-unit path plus diameter at direction " + i);
                    }
                }
                finally { Object.Destroy(mesh); }
                Vector2 origin = skill.LockedOrigin;
                boss.GetModule<BossFlightModule>().Place(origin + Vector2.right);
                target.transform.position += Vector3.left * 3f;
                foreach (Transform child in boss.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                rt = new RenderTexture(768, 432, 24, RenderTextureFormat.ARGB32);
                Camera(stage, (Vector3)origin + Vector3.back * 10f, 5.5f, rt);
                await TickFrames(3);
                Check(Vector2.Distance(line.transform.position, origin) < 0.001f && line.IsVisible, "Warning origin remains locked");
                string preview = Save(rt, "RobotEye_RadialLanes.png");
                await Until(() => skill.DidFire);
                var bullets = Object.FindObjectsByType<EnemyProjectile>(FindObjectsSortMode.None);
                Check(bullets.Length == 12 && !line.IsVisible, "Twelve actual bullets / warning hidden after firing");
                var directions = new HashSet<int>();
                foreach (var bullet in bullets)
                {
                    float angle = Mathf.Repeat(bullet.transform.eulerAngles.z, 360f);
                    int index = Mathf.RoundToInt(angle / 30f) % 12; directions.Add(index);
                    Check(Mathf.Abs(Mathf.DeltaAngle(angle, index * 30f)) < 0.1f && Vector2.Distance(bullet.transform.position, origin) < 0.5f,
                        "Projectile direction / locked spawn matches warning");
                }
                Check(directions.Count == 12, "No missing or duplicated shot direction");
                return new { lanes = 12, length = 25f, width = 0.3f, preview };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); if (rt != null) { rt.Release(); Object.Destroy(rt); } }
        }

        private static async Task<object> CheckProjectile(string scenario)
        {
            await TickFrames(2);
            var stage = new GameObject("Projectile " + scenario);
            EnemyProjectile projectile = null;
            try
            {
                Vector2 origin = new Vector2(9100f, 0f);
                var target = new GameObject("Target"); target.transform.SetParent(stage.transform); target.layer = 7;
                target.transform.position = origin + Vector2.right * 3f;
                var box = target.AddComponent<BoxCollider2D>(); box.isTrigger = true; box.size = Vector2.one * 0.5f;
                var extra = new GameObject("Extra target collider"); extra.transform.SetParent(target.transform, false);
                extra.AddComponent<CircleCollider2D>().isTrigger = true;
                var probe = target.AddComponent<EnemyDamageProbe>();
                Vector2 direction = scenario == "floor" ? Vector2.down : Vector2.right;
                if (scenario == "wall" || scenario == "floor" || scenario == "insideWall")
                {
                    var wall = new GameObject("Ground"); wall.transform.SetParent(stage.transform); wall.layer = 6;
                    wall.transform.position = scenario == "floor" ? origin + Vector2.down * 2f : origin + Vector2.right * (scenario == "insideWall" ? 0f : 2f);
                    wall.AddComponent<BoxCollider2D>().size = scenario == "floor" ? new Vector2(2f, 0.02f) : new Vector2(0.02f, 2f);
                }
                projectile = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyProjectile>(ProjectilePath), origin, Quaternion.identity);
                if (scenario == "wall" || scenario == "floor") projectile.Speed = 200f; // thin terrain swept collision
                if (scenario == "lifetime") projectile.Lifetime = 0.12f;
                projectile.Launch(null, target.transform, 30f, direction);
                var animation = projectile.GetComponent<EnemyProjectileVisual>();
                var visual = projectile.transform.Find("Visual").GetComponent<SpriteRenderer>();
                var flight = new HashSet<Sprite>();
                if (scenario == "lifetime")
                {
                    await Until(() => { if (projectile != null) Check(!animation.IsImpactPlaying, "Expiry must not fake a collision"); return projectile == null; });
                    return new { scenario, passed = true, impact = false };
                }
                await Until(() => { if (!animation.IsImpactPlaying) flight.Add(visual.sprite); return animation.IsImpactPlaying; });
                Check(projectile != null && !projectile.GetComponent<Collider2D>().enabled && !projectile.GetComponent<Rigidbody2D>().simulated,
                    "Collision stops simulation while effect remains");
                if (scenario == "target") Check(probe.ReceivedHitCount == 1 && probe.TotalDamage == 30f && flight.Count > 1, "Animated flight and one damage");
                else Check(probe.ReceivedHitCount == 0, "Terrain must not damage target behind it");
                Vector3 impactPosition = projectile.transform.position;
                if (scenario == "wall") Check(Mathf.Abs(impactPosition.x - (origin.x + 1.99f)) < 0.02f, "Effect at wall contact point");
                if (scenario == "floor") Check(Mathf.Abs(impactPosition.y + 1.99f) < 0.02f, "Effect at floor contact point");
                var impact = new HashSet<Sprite>();
                await Until(() =>
                {
                    if (projectile == null) return true;
                    Check(projectile.transform.position == impactPosition, "Explosion must not keep moving");
                    impact.Add(visual.sprite);
                    return false;
                });
                Check(impact.Count == 6, "All six impact frames must display before cleanup: " + impact.Count);
                Check(probe.ReceivedHitCount == (scenario == "target" ? 1 : 0), "No duplicate damage during explosion");
                return new { scenario, passed = true, impactFrames = impact.Count, hits = probe.ReceivedHitCount };
            }
            finally { stage.SetActive(false); Object.Destroy(stage); if (projectile != null) Object.Destroy(projectile.gameObject); }
        }

        private static Sprite[] Frames(string prefix) => AssetDatabase.LoadAllAssetsAtPath(Sheet).OfType<Sprite>()
            .Where(s => s.name.StartsWith(prefix, StringComparison.Ordinal)).OrderBy(s => s.name).ToArray();
        private static void SetFrames(SerializedProperty p, Sprite[] frames)
        {
            p.arraySize = frames.Length; for (int i = 0; i < frames.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = frames[i];
        }
        private static Camera Camera(GameObject stage, Vector3 position, float size, RenderTexture rt)
        {
            var go = new GameObject("Preview camera"); go.transform.SetParent(stage.transform); go.transform.position = position;
            var camera = go.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = size;
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f);
            camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false; camera.targetTexture = rt;
            go.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false; return camera;
        }
        private static string Save(RenderTexture rt, string name)
        {
            var previous = RenderTexture.active; var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                string path = Application.dataPath + "/CTJ/Tools~/" + name; File.WriteAllBytes(path, texture.EncodeToPNG()); return path;
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }
        private static async Task TickFrames(int count) { int end = Time.frameCount + count; await Until(() => Time.frameCount >= end, 5); }
        private static async Task Until(Func<bool> condition, int seconds = 10)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Verification timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play required");
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
