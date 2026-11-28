using System;
using System.Linq;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectBoss23Recovery
{
    private const string Prefab = "Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab";
    private const string Anim = "Assets/_Graphics/Animations/Enemy/Enemy23/Boss23_ProductionControlDrone_";

    public static object Before() => Sheet("Before");
    public static object After() => Sheet("After");

    public static object Sparks()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        var node = new GameObject("Rear spark review camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(node, root.scene);
        var camera = node.AddComponent<Camera>();
        camera.enabled = false; camera.scene = root.scene;
        camera.orthographic = true; camera.orthographicSize = 1.7f;
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.075f, 0.08f);
        var target = RenderTexture.GetTemporary(512, 384, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(1024, 384, TextureFormat.RGB24, false);
        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            foreach (string name in new[] { "ProcessNumber", "AlarmLamp" })
                animator.transform.Find(name).GetComponent<SpriteRenderer>().sharedMaterial = body.sharedMaterial;
            var feedback = root.GetComponentInChildren<_Scripts.Agent.Enemies.Bosses.DroneLastStandFeedbackModule>(true);
            var ps = (ParticleSystem)new SerializedObject(feedback).FindProperty("malfunctionSparks").objectReferenceValue;
            var visual = root.transform.Find("Visual");
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + "GroggyHold.anim");
            for (int i = 0; i < 2; i++)
            {
                var scale = visual.localScale; scale.x = Mathf.Abs(scale.x) * (i == 0 ? 1f : -1f); visual.localScale = scale;
                clip.SampleAnimation(animator.gameObject, 0f);
                animator.transform.Find("ProcessNumber").GetComponent<SpriteRenderer>().flipX = i == 1;
                ps.useAutoRandomSeed = false; ps.randomSeed = 2375;
                ps.Simulate(0.55f, true, true, true);
                camera.transform.position = root.transform.position + new Vector3(0f, 0.1f, -10f);
                camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                image.ReadPixels(new Rect(0, 0, 512, 384), i * 512, 0);
            }
            image.Apply();
            const string path = "Tools~/CTJ/Boss23RearSparkPreview.png";
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            return path;
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image); Object.DestroyImmediate(node);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }

    public static object Anchors()
    {
        const string art = "Assets/_Graphics/Assets/Enemy/Enemy23/SHADOW Series - Voidrone Boss/";
        var reference = Load(art + "normal state/idle.png");
        var rows = new System.Collections.Generic.List<object>();
        try
        {
            foreach (string path in new[] { "Spetial Attacks/laser/robot only.png", "Spetial Attacks/shotting bomb/robot only.png", "Crouch/transition.png" })
            {
                var texture = Load(art + path);
                try
                {
                    int width = path.StartsWith("Crouch") ? 128 : 256;
                    var pixels = texture.GetPixels32();
                    var basis = reference.GetPixels32();
                    for (int frame = 0; frame < texture.width / width; frame++)
                    {
                        long best = long.MaxValue;
                        int bx = 0, by = 0;
                        for (int dy = -14; dy <= 24; dy++)
                            for (int dx = -8; dx <= 8; dx++)
                            {
                                long score = 0;
                                for (int y = 27; y < 44; y++)
                                    for (int x = 40; x < 61; x++)
                                    {
                                        var a = basis[(reference.height - 1 - y) * reference.width + x];
                                        var b = pixels[(texture.height - 1 - y - dy) * texture.width + frame * width + x + dx];
                                        score += (a.r - b.r) * (a.r - b.r) + (a.g - b.g) * (a.g - b.g) +
                                            (a.b - b.b) * (a.b - b.b) + (a.a - b.a) * (a.a - b.a);
                                    }
                                if (score < best) { best = score; bx = dx; by = dy; }
                            }
                        int roof = 90;
                        for (int y = 10; y < 70; y++)
                            for (int x = 40; x < 78; x++)
                            {
                                var c = pixels[(texture.height - 1 - y) * texture.width + frame * width + x];
                                if (c.a > 0 && c.r > 45 && c.r < 125 && c.g > c.r && c.g < 155 && c.b < c.g) roof = Mathf.Min(roof, y);
                            }
                        rows.Add(new { path, frame, dx = bx, dy = by, score = best, roof });
                    }
                }
                finally { Object.DestroyImmediate(texture); }
            }
            return rows;
        }
        finally { Object.DestroyImmediate(reference); }
    }

    private static Texture2D Load(string path)
    {
        var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.LoadImage(System.IO.File.ReadAllBytes(path));
        return texture;
    }

    private static object Sheet(string suffix)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode) throw new Exception("Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents(Prefab);
        var cameraNode = new GameObject("Pose review camera");
        UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraNode, root.scene);
        var camera = cameraNode.AddComponent<Camera>();
        camera.enabled = false; camera.scene = root.scene;
        camera.orthographic = true; camera.orthographicSize = 1.55f;
        camera.transform.position = root.transform.position + new Vector3(0.4f, 0.25f, -10f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.06f, 0.075f, 0.08f);
        var target = RenderTexture.GetTemporary(224, 224, 24);
        var previous = RenderTexture.active;
        var image = new Texture2D(224 * 12, 224 * 5, TextureFormat.RGB24, false);
        var details = new System.Collections.Generic.List<object>();

        try
        {
            var animator = root.GetComponentInChildren<Animator>(true);
            var body = animator.GetComponent<SpriteRenderer>();
            foreach (string name in new[] { "ProcessNumber", "AlarmLamp" })
                animator.transform.Find(name).GetComponent<SpriteRenderer>().sharedMaterial = body.sharedMaterial;
            string[] states = { "LaserCharge", "Laser", "BombCharge", "Bomb", "CrouchRecover" };
            for (int row = 0; row < states.Length; row++)
            {
                var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Anim + states[row] + ".anim");
                var frames = AnimationUtility.GetObjectReferenceCurve(clip,
                    EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"));
                for (int col = 0; col < 12; col++)
                {
                    int frame = Mathf.Min(col, frames.Length - 1);
                    clip.SampleAnimation(animator.gameObject, frames[frame].time);
                    camera.targetTexture = target; camera.Render(); RenderTexture.active = target;
                    image.ReadPixels(new Rect(0, 0, 224, 224), col * 224, (4 - row) * 224);
                    if (col < frames.Length)
                        details.Add(new { state = states[row], frame, sprite = body.sprite.name,
                            path = AssetDatabase.GetAssetPath(body.sprite),
                            number = (animator.transform.Find("ProcessNumber").localPosition * 28f).ToString("F1"),
                            lamp = (animator.transform.Find("AlarmLamp").localPosition * 28f).ToString("F1") });
                }
            }
            image.Apply();
            string path = "Tools~/CTJ/Boss23RecoveryAlignment" + suffix + ".png";
            System.IO.File.WriteAllBytes(path, image.EncodeToPNG());
            return new { path, rows = states, frames = details };
        }
        finally
        {
            RenderTexture.active = previous;
            Object.DestroyImmediate(image); Object.DestroyImmediate(cameraNode);
            RenderTexture.ReleaseTemporary(target);
            PrefabUtility.UnloadPrefabContents(root);
        }
    }
}
