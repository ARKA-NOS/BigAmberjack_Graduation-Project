using System;
using System.Linq;
using System.Threading.Tasks;
using _Scripts.UI.HitFeedback;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyEnemyVisualFix
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Popup = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicator.prefab";

    public static object CheckAssets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(Boss);
        int clips = 0;

        foreach (var clip in root.GetComponentInChildren<Animator>(true).runtimeAnimatorController.animationClips.Distinct())
        {
            var number = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("ProcessNumber", typeof(Transform), "m_LocalPosition.y"));
            if (number == null) continue;
            var lamp = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("AlarmLamp", typeof(Transform), "m_LocalPosition.y"));

            Check(lamp != null && number.keys.All(k => Mathf.Abs(lamp.Evaluate(k.time) - k.value - 10f / 28f) < 0.00001f),
                "Lamp offset lost during " + clip.name);
            clips++;
        }

        var label = AssetDatabase.LoadAssetAtPath<GameObject>(Popup).GetComponentInChildren<TMP_Text>(true);

        Check(label.font.name == "AmberjackPixel Raster" && label.font.HasCharacters("0123456789"), "Wrong pixel font");
        Check(label.fontSharedMaterial.shader.name == "Enemy/DamageIndicatorBitmap" &&
            !ShaderUtil.ShaderHasError(label.fontSharedMaterial.shader), "Bitmap stroke shader invalid");
        Check(label.fontSize == 56f && Mathf.Abs(label.fontSharedMaterial.GetFloat("_StrokePixels") - 0.25f) < 0.0001f,
            "Larger/bolder indicator settings missing");

        return new { clips, lampRaisePixels = 2, font = label.font.name, fontSize = label.fontSize };
    }

    public static object Laser()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents(Boss);
        var node = new GameObject("Beam width camera");
        SceneManager.MoveGameObjectToScene(node, root.scene);
        var camera = node.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = root.scene;
        camera.orthographic = true;
        camera.orthographicSize = 3f;
        camera.aspect = 1f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = Color.clear;
        var target = RenderTexture.GetTemporary(768, 768, 24);
        var previous = RenderTexture.active;
        var pixels = new Texture2D(768, 768, TextureFormat.RGBA32, false);
        var sheet = new Texture2D(1536, 768, TextureFormat.RGBA32, false);

        try
        {
            foreach (var renderer in root.GetComponentsInChildren<Renderer>(true)) renderer.enabled = false;
            foreach (var canvas in root.GetComponentsInChildren<Canvas>(true)) canvas.enabled = false;
            var beam = root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>();

            Check(!ShaderUtil.ShaderHasError(beam.sharedMaterial.shader), "Laser shader failed compilation");
            beam.enabled = true;
            beam.color = Color.white;
            var results = new System.Collections.Generic.List<object>();

            for (int i = 0; i < 2; i++)
            {
                float angle = i == 0 ? -35f : 35f;
                float radians = angle * Mathf.Deg2Rad;
                Vector2 direction = new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
                Vector2 side = new Vector2(-direction.y, direction.x);
                Vector2 origin = new Vector2(0f, 4.5f);
                float length = 4.5f / -direction.y;
                const float width = 0.8f;

                beam.transform.position = origin + direction * (length * 0.5f);
                beam.transform.rotation = Quaternion.Euler(0f, 0f, angle);
                beam.transform.localScale = new Vector3(width / beam.sprite.bounds.size.x / root.transform.lossyScale.x,
                    length / beam.sprite.bounds.size.y / root.transform.lossyScale.y, 1f);
                var block = new MaterialPropertyBlock();
                block.SetVector("_BeamOrigin", new Vector4(0f, 4.5f, 0f, 0f));
                block.SetVector("_BeamDirection", new Vector4(direction.x, direction.y, 0f, 0f));
                block.SetFloat("_BeamLength", length);
                block.SetFloat("_EndFadeDistance", 1f);
                block.SetFloat("_FloorY", 0f);
                beam.SetPropertyBlock(block);
                camera.transform.position = new Vector3(i == 0 ? -1.5f : 1.5f, 2.25f, -10f);
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                pixels.ReadPixels(new Rect(0, 0, 768, 768), 0, 0);
                pixels.Apply();
                sheet.SetPixels(i * 768, 0, 768, 768, pixels.GetPixels());

                var samples = new[] { 0.2f, 0.4f, 0.7f, 1.2f }.Select(remaining =>
                {
                    Vector2 center = origin + direction * (length - remaining);
                    float a = Alpha(center - side * width * 0.35f);
                    float b = Alpha(center + side * width * 0.35f);

                    Check(Mathf.Abs(a - b) < 0.035f, "Diagonal end fade narrows one side: " + a + "/" + b);
                    return new { remaining, left = a, right = b };
                }).ToArray();
                Check(samples[0].left < samples[2].left && samples[2].left < samples[3].left, "End gradient disappeared");
                float actualWidth = beam.transform.TransformVector(Vector3.right * beam.sprite.bounds.size.x).magnitude;
                Check(Mathf.Abs(actualWidth - width) < 0.0001f, "Beam world width changed");
                results.Add(new { angle, actualWidth, samples });
            }

            sheet.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/Boss23StraightLaserPreview.png", sheet.EncodeToPNG());

            return new { results };

            float Alpha(Vector2 world)
            {
                Vector3 uv = camera.WorldToViewportPoint(world);
                return pixels.GetPixel(Mathf.Clamp((int)(uv.x * 768), 0, 767), Mathf.Clamp((int)(uv.y * 768), 0, 767)).a;
            }
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(pixels);
            Object.DestroyImmediate(sheet);
            Object.DestroyImmediate(node);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static object FontPreview()
    {
        Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents(Popup);
        var node = new GameObject("Pixel font preview camera");
        SceneManager.MoveGameObjectToScene(node, root.scene);
        var camera = node.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = root.scene;
        camera.orthographic = true;
        camera.orthographicSize = 0.9f;
        camera.aspect = 4f;
        camera.transform.position = root.transform.position + Vector3.back * 10f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.05f, 0.06f, 0.07f);
        var target = RenderTexture.GetTemporary(1024, 256, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(1024, 256, TextureFormat.RGB24, false);

        try
        {
            var canvas = root.GetComponent<Canvas>();
            canvas.enabled = true;
            canvas.worldCamera = camera;
            var label = root.GetComponentInChildren<TMP_Text>(true);
            label.rectTransform.sizeDelta = new Vector2(1000f, 100f);
            label.text = "0123456789";
            label.color = Color.white;
            label.ForceMeshUpdate(true, true);
            Canvas.ForceUpdateCanvases();
            camera.targetTexture = target;
            camera.Render();
            RenderTexture.active = target;
            image.ReadPixels(new Rect(0, 0, 1024, 256), 0, 0);
            image.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/DamageIndicatorPixelFontPreview.png", image.EncodeToPNG());

            return new { font = label.font.name, digits = label.text, preview = "Tools~/CTJ/DamageIndicatorPixelFontPreview.png" };
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(node);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static async Task<object> Popups()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "Assets/_Scenes/Test/EnemyTestScene.unity", "Use EnemyTestScene Play Mode");
        var originals = SceneManager.GetActiveScene().GetRootGameObjects().Where(g => g.activeSelf && g.GetComponent<Lean.Pool.LeanGameObjectPool>() == null).ToArray();
        foreach (var g in originals) g.SetActive(false);
        var normal = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyDamageIndicatorPopup>(Popup));
        var critical = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyDamageIndicatorPopup>(Popup));

        try
        {
            normal.Show(new DamageIndicatorData(1, 9.8f, false, Vector3.left));
            critical.Show(new DamageIndicatorData(1, 25.5f, true, Vector3.right));
            var a = normal.GetComponentInChildren<TMP_Text>();
            var b = critical.GetComponentInChildren<TMP_Text>();
            a.ForceMeshUpdate(); b.ForceMeshUpdate();

            Check(a.text == "10" && b.text == "26", "Damage rounding changed");
            Check(a.font.name == "AmberjackPixel Raster" && b.font == a.font && a.fontSharedMaterial == b.fontSharedMaterial, "Font not shared");
            Check(a.fontSize == 56f && b.fontSize == 56f && a.fontSharedMaterial.GetFloat("_StrokePixels") > 0f,
                "Popup size or stroke reverted at runtime");
            Check(a.color == Color.white && b.color.r > 0.9f && b.color.g > 0.7f && b.color.b < 0.3f, "Normal/critical colors changed");
            Check(a.textInfo.characterInfo.Take(a.textInfo.characterCount).All(c => c.fontAsset == a.font), "Pixel font used a fallback");
            float deadline = Time.realtimeSinceStartup + 2f;
            while ((normal != null || critical != null) && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
            Check(normal == null && critical == null, "Pixel popup did not expire");

            return new { normalText = "10", criticalText = "26", font = "AmberjackPixel Raster", expired = true };
        }
        finally
        {
            if (normal != null) Object.Destroy(normal.gameObject);
            if (critical != null) Object.Destroy(critical.gameObject);
            foreach (var g in originals) if (g != null) g.SetActive(true);
        }
    }

    private static void Check(bool success, string message) { if (!success) throw new Exception(message); }
}
