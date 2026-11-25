using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class TuneDamageIndicatorReadability
{
    private const string Popup = "Assets/_Prefabs/Enemies/Effects/EnemyDamageIndicator.prefab";
    private const string Material = "Assets/_Graphics/Materials/Enemy/EnemyDamageIndicatorText.mat";

    public static object Run()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var shader = Shader.Find("Enemy/DamageIndicatorBitmap");
        if (shader == null || ShaderUtil.ShaderHasError(shader)) throw new Exception("Bitmap stroke shader invalid");
        var material = AssetDatabase.LoadAssetAtPath<Material>(Material);

        material.shader = shader;
        material.SetFloat("_StrokePixels", 0.25f);
        EditorUtility.SetDirty(material);
        AssetDatabase.SaveAssetIfDirty(material);
        var root = PrefabUtility.LoadPrefabContents(Popup);

        try
        {
            var label = root.GetComponentInChildren<TMP_Text>(true);

            label.fontSize = 56f;
            label.fontSharedMaterial = material;
            PrefabUtility.SaveAsPrefabAsset(root, Popup);
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }

        return new { fontSize = 56f, strokePixels = 0.25f, shader = shader.name };
    }

    public static object Compare()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var root = PrefabUtility.LoadPrefabContents(Popup);
        var node = new GameObject("Text readability camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(node, root.scene);
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
        var image = new Texture2D(1024, 768, TextureFormat.RGB24, false);
        var sample = new Texture2D(1024, 256, TextureFormat.RGB24, false);
        var previewMaterial = new Material(AssetDatabase.LoadAssetAtPath<Material>(Material));

        try
        {
            root.GetComponent<Canvas>().worldCamera = camera;
            var label = root.GetComponentInChildren<TMP_Text>(true);
            label.rectTransform.sizeDelta = new Vector2(1000f, 100f);
            label.fontSharedMaterial = previewMaterial;
            label.text = "0123456789";
            label.color = Color.white;
            int[] ink = new int[3];

            for (int i = 0; i < 3; i++)
            {
                label.fontSize = i == 0 ? 42f : 56f;
                previewMaterial.SetFloat("_StrokePixels", i == 2 ? 0.25f : 0f);
                label.ForceMeshUpdate(true, true);
                Canvas.ForceUpdateCanvases();
                camera.targetTexture = target;
                camera.Render();
                RenderTexture.active = target;
                sample.ReadPixels(new Rect(0, 0, 1024, 256), 0, 0);
                sample.Apply();
                Color[] colors = sample.GetPixels();
                foreach (var color in colors) if (color.r > 0.65f && color.g > 0.65f) ink[i]++;
                image.SetPixels(0, (2 - i) * 256, 1024, 256, colors);
            }

            if (ink[1] <= ink[0] || ink[2] <= ink[1] * 1.2f) throw new Exception("Larger/bolder text did not render: " + string.Join(",", ink));
            image.Apply();
            System.IO.File.WriteAllBytes("Tools~/CTJ/DamageIndicatorReadabilityPreview.png", image.EncodeToPNG());

            return new { oldInk = ink[0], enlargedInk = ink[1], boldInk = ink[2], strokeGain = (float)ink[2] / ink[1] };
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(sample);
            Object.DestroyImmediate(previewMaterial);
            Object.DestroyImmediate(node);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
