using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectBoss23VisualFix
{
    private const string Boss = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";

    public static object Inspect()
    {
        var root = PrefabUtility.LoadPrefabContents(Boss);

        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            var beam = root.transform.Find("LaserBeam").GetComponent<SpriteRenderer>();
            var states = new[] { "Idle", "Rise", "AirIdle", "AirShotFront", "AirShotRear", "Descend" }.Select(name =>
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + name + ".anim");
                var keys = AnimationUtility.GetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));

                return new { name, frames = keys.Select(k =>
                {
                    clip.SampleAnimation(animator.gameObject, k.time);
                    var s = body.sprite;
                    return new { sprite = s.name, time = k.time, pivot = (s.rect.position + s.pivot).ToString("F2"),
                        number = (animator.transform.Find("ProcessNumber").localPosition * 28f).ToString("F2"),
                        lamp = (animator.transform.Find("AlarmLamp").localPosition * 28f).ToString("F2") };
                }).ToArray() };
            }).ToArray();

            return new { states, beam = new { vertices = beam.sprite.vertices.Select(v => v.ToString("F3")).ToArray(),
                texture = beam.sprite.texture.name, rect = beam.sprite.rect.ToString() } };
        }
        finally { PrefabUtility.UnloadPrefabContents(root); }
    }

    public static object Before() => Sheet("Before");
    public static object After() => Sheet("After");

    private static object Sheet(string suffix)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");

        var root = PrefabUtility.LoadPrefabContents(Boss);
        var cameraNode = new GameObject("Alignment camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraNode, root.scene);
        var camera = cameraNode.AddComponent<Camera>();
        camera.enabled = false;
        camera.scene = root.scene;
        camera.orthographic = true;
        camera.orthographicSize = 1.9f;
        camera.transform.position = root.transform.position + new Vector3(0.3f, 0.4f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.075f, 0.08f);
        var target = RenderTexture.GetTemporary(256, 256, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(1792, 512, TextureFormat.RGB24, false);

        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            foreach (string name in new[] { "ProcessNumber", "AlarmLamp" })
                animator.transform.Find(name).GetComponent<SpriteRenderer>().sharedMaterial = body.sharedMaterial;

            for (int row = 0; row < 2; row++)
                for (int col = 0; col < 7; col++)
                {
                    string state = col == 6 ? (row == 0 ? "AirIdle" : "Idle") : row == 0 ? "Rise" : "Descend";
                    var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + state + ".anim");
                    clip.SampleAnimation(animator.gameObject, col == 6 ? 0f : col * 0.09f);
                    camera.targetTexture = target;
                    camera.Render();
                    RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 256, 256), col * 256, (1 - row) * 256);
                }

            image.Apply();
            string path = "Tools~/CTJ/Boss23AirAlignment" + suffix + ".png";
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());

            return path;
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image);
            Object.DestroyImmediate(cameraNode);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
