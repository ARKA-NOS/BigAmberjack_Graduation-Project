using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyOutlinePixels
    {
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };
        private static readonly Color Hit = new Color(0.75f, 0.18f, 0.22f, 1f);

        public static object Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            foreach (string relative in Enemies)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var settings = new SerializedObject(root.GetComponentInChildren<EnemyOutlineModule>(true));
                    settings.FindProperty("normalColor").colorValue = Color.black;
                    settings.FindProperty("hitColor").colorValue = Hit;
                    settings.FindProperty("showOnlyWhenHit").boolValue = true;
                    settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            const string coinPath = "Assets/CTJ/Prototypes/Prefabs/EnemyRewardCoin.prefab";
            GameObject coin = PrefabUtility.LoadPrefabContents(coinPath);
            try
            {
                var settings = new SerializedObject(coin.GetComponent<EnemyRewardCoin>());
                settings.FindProperty("collectScalePeak").floatValue = 1.8f;
                settings.FindProperty("collectScaleDip").floatValue = 0.35f;
                settings.FindProperty("collectScaleDuration").floatValue = 0.26f;
                settings.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(coin, coinPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(coin); }
            return RunEdit();
        }

        public static object RunEdit()
        {
            foreach (string relative in Enemies)
            {
                GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + relative + ".prefab");
                var module = root.GetComponentInChildren<EnemyOutlineModule>();
                var data = new SerializedObject(module);
                Check(module.ShowOnlyWhenHit && data.FindProperty("normalColor").colorValue == Color.black &&
                    data.FindProperty("hitColor").colorValue == Hit && data.FindProperty("width").floatValue == 2f, "Outline defaults " + relative);
                foreach (Component component in root.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing script " + relative);
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Missing reference " + relative + "/" + property.propertyPath);
                }
            }
            string[] errors = ShaderUtil.GetShaderMessages(Shader.Find("CTJ/Enemies/TemporaryOutline"))
                .Where(m => m.severity.ToString() == "Error").Select(m => m.message).ToArray();
            Check(errors.Length == 0, string.Join(" | ", errors));
            return new { passed = true, enemies = 6, shaderErrors = errors };
        }

        public static async Task<object> Measure()
        {
            RequirePlay();
            AssetDatabase.ImportAsset("Assets/CTJ/Shaders/EnemyOutline.shader", ImportAssetOptions.ForceUpdate);
            var results = new List<object>();
            foreach (bool outer in new[] { false, true })
            {
                foreach (int size in new[] { 16, 256 })
                    foreach (float zoom in new[] { 1f, 2f, 4f })
                        results.Add(await MeasureCase(size, zoom, new Vector3(1.5f, 0.8f, 1f), 0f, 2f, outer));
                results.Add(await MeasureCase(256, 1f, Vector3.one, 45f, 6f, outer));
                foreach (Vector3 scale in new[] { new Vector3(0.5f, 2f, 1f), new Vector3(-2f, 0.5f, 1f) })
                    results.Add(await MeasureCase(256, 2f, scale, 0f, 2f, outer));
                results.Add(await MeasureCase(256, 2f, Vector3.one, 0f, 2f, outer, 512));
            }
            return new { cases = results };
        }

        private static async Task<object> MeasureCase(int textureSize, float zoom, Vector3 scale, float angle, float width, bool outer, int resolution = 256)
        {
            var root = new GameObject("Screen pixel thickness probe");
            var texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
            var pixels = new Color32[textureSize * textureSize];
            for (int y = textureSize / 4; y < textureSize * 3 / 4; y++)
                for (int x = textureSize / 4; x < textureSize * 3 / 4; x++) pixels[y * textureSize + x] = new Color32(255, 255, 255, 255);
            texture.SetPixels32(pixels); texture.Apply(); texture.filterMode = FilterMode.Point; texture.wrapMode = TextureWrapMode.Clamp;
            Sprite sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), Vector2.one * 0.5f, textureSize, 0, SpriteMeshType.FullRect);
            var renderTarget = new RenderTexture(resolution, resolution, 24, RenderTextureFormat.ARGB32);
            try
            {
                var body = new GameObject("Body"); body.transform.SetParent(root.transform); body.transform.localScale = scale;
                body.transform.rotation = Quaternion.Euler(0, 0, angle);
                var source = body.AddComponent<SpriteRenderer>(); source.sprite = sprite;
                source.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
                var border = new GameObject("Outline"); border.transform.SetParent(body.transform, false);
                var outline = border.AddComponent<SpriteRenderer>(); outline.sprite = sprite;
                outline.sharedMaterial = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/EnemyOutline.mat");
                outline.sortingOrder = outer ? -1 : 1; outline.color = Color.red;
                var settings = new MaterialPropertyBlock(); settings.SetFloat("_OutlineWidth", width);
                settings.SetVector("_SpriteUVRect", new Vector4(0, 0, 1, 1)); settings.SetVector("_SpriteLocalRect", new Vector4(0, 0, 1, 1));
                settings.SetFloat("_OuterOutline", outer ? 1 : 0); outline.SetPropertyBlock(settings);
                var cameraObject = new GameObject("Pixel camera"); cameraObject.transform.SetParent(root.transform);
                Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = zoom;
                camera.transform.position = new Vector3(0, 0, -10); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.backgroundColor = Color.clear; camera.allowMSAA = false; camera.allowHDR = false; camera.targetTexture = renderTarget;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                await Frames(2); camera.Render();
                Color32[] image = Read(renderTarget, angle > 0f ? "Assets/CTJ/Tools~/OutlineProbe_" + outer + ".png" : null);
                int horizontal = 0, vertical = 0;
                int scan = resolution / 2 + (angle > 0f ? 16 : 0); // 대각선은 모서리 끝이 아닌 평평한 변을 가로질러 측정합니다.
                for (int x = resolution / 2; x < resolution; x++) if (Red(image[scan * resolution + x])) horizontal++;
                for (int y = resolution / 2; y < resolution; y++) if (Red(image[y * resolution + scan])) vertical++;
                if (angle == 0f) Check(horizontal == (int)width && vertical == (int)width, "Screen pixel width changed with texture size / zoom / scale");
                else Check(horizontal >= 8 && horizontal <= 9 && vertical >= 8 && vertical <= 9, "Diagonal must use the same six-pixel normal thickness: " + horizontal + "/" + vertical + ", outer=" + outer);
                return new { textureSize, resolution, zoom, scale = scale.ToString(), angle, width, outer, horizontal, vertical };
            }
            finally { root.SetActive(false); Object.Destroy(root); Object.Destroy(sprite); Object.Destroy(texture); Object.Destroy(renderTarget); }
        }

        public static async Task<object> LethalFeedback()
        {
            RequirePlay();
            var root = new GameObject("Lethal outline verification"); root.SetActive(false);
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab"), root.transform);
            enemy.GetComponent<Rigidbody2D>().constraints = RigidbodyConstraints2D.FreezeAll;
            enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyParticleModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            root.SetActive(true);
            var outline = enemy.GetComponentInChildren<EnemyOutlineModule>();
            try
            {
                await Frames(2);
                Check(Outlines(enemy).All(r => !r.enabled), "Hidden before hit");
                enemy.ApplyDamage(new DamageData { DamageAmount = 99999f }, enemy.transform.position, Vector2.right, Vector2.left);
                await Frames(2);
                Check(enemy.IsPlayingDeathReaction && outline.IsShowingHit && Outlines(enemy).Any(r => r.enabled && r.color == Hit), "Lethal hit outline remains during final reaction");
                float until = Time.time + 0.4f; DateTime deadline = DateTime.UtcNow.AddSeconds(10);
                while (Time.time < until) { Check(DateTime.UtcNow < deadline, "Death wait timeout"); await Task.Yield(); }
                Check(!enemy.IsPlayingDeathReaction && Outlines(enemy).All(r => !r.enabled), "Death animation has no stale outline");
                return new { passed = true, scope = "lethal hit feedback then removal" };
            }
            finally { root.SetActive(false); Object.Destroy(root); }
        }

        private static IEnumerable<SpriteRenderer> Outlines(EnemyBase enemy) => enemy.GetComponentsInChildren<SpriteRenderer>(true).Where(r => r.name == "TemporaryOutline");

        private static bool Red(Color32 color) => color.r > 128 && color.g < 32 && color.b < 32 && color.a > 64;
        private static Color32[] Read(RenderTexture target, string path = null)
        {
            var previous = RenderTexture.active; var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try { RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply(); if (path != null) System.IO.File.WriteAllBytes(path, pixels.EncodeToPNG()); return pixels.GetPixels32(); }
            finally { RenderTexture.active = previous; Object.Destroy(pixels); }
        }
        private static async Task Frames(int count)
        {
            int until = Time.frameCount + count; DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (Time.frameCount < until) { Check(DateTime.UtcNow < deadline, "Frame timeout"); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
