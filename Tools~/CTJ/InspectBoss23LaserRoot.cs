using System;
using System.Linq;
using System.Reflection;
using _Scripts.Agent.Enemies.Skills;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public static class InspectBoss23LaserRoot
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.NonPublic;
    public static object Run()
    {
        if (EditorApplication.isPlaying || EditorApplication.isCompiling) throw new Exception("Use Edit Mode");
        var root = PrefabUtility.LoadPrefabContents("Assets/_Prefabs/Enemies/Bosses/Enemy23/Boss23_ProductionControlDrone.prefab");
        var cameraRoot = new GameObject("Laser root preview camera");
        var sheet = new Texture2D(1536, 1536, TextureFormat.RGBA32, false);
        try
        {
            root.SetActive(true);
            UnityEngine.SceneManagement.SceneManager.MoveGameObjectToScene(cameraRoot, root.scene);
            var camera = cameraRoot.AddComponent<Camera>();
            camera.enabled = false; camera.scene = root.scene;
            camera.orthographic = true; camera.orthographicSize = 1.6f;
            camera.transform.position = new Vector3(0f, -0.15f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor; camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f, 0f);
            var animator = root.GetComponentInChildren<Animator>(true);
            var clip = animator.runtimeAnimatorController.animationClips.Single(c => c.name.EndsWith("_Laser"));
            var laser = root.GetComponentInChildren<DroneDownwardLaserSkill>(true);
            var data = new SerializedObject(laser);
            var beam = (SpriteRenderer)data.FindProperty("beam").objectReferenceValue;
            var muzzle = (Transform)data.FindProperty("muzzle").objectReferenceValue;
            Set("_origin", (Vector2)muzzle.position); Set("_floorY", -6.5f);
            Set("_radius", 7.5f); Set("_beamWidth", data.FindProperty("width").floatValue);
            Set("_bodyOverlap", data.FindProperty("bodyOverlapDistance").floatValue);
            Set("_beamProperties", new MaterialPropertyBlock()); Set("_sweepDuration", 1f);
            Set("_totalSweepDuration", 1f);
            beam.enabled = true;
            var update = typeof(DroneDownwardLaserSkill).GetMethod("UpdateBeam", Private);
            float[] angles = { -57.85f, 0f, 57.85f };
            float minConnectionAlpha = 1f;
            for (int row = 0; row < 3; row++)
            {
                clip.SampleAnimation(animator.gameObject, clip.length * row * 0.5f);
                for (int i = 0; i < angles.Length; i++)
                {
                    update.Invoke(laser, new object[] { angles[i], 0.4f });
                    var rt = RenderTexture.GetTemporary(512, 512, 24, RenderTextureFormat.ARGB32);
                    var previous = RenderTexture.active;
                    try
                    {
                        camera.targetTexture = rt; camera.Render(); RenderTexture.active = rt;
                        sheet.ReadPixels(new Rect(0, 0, 512, 512), i * 512, (2 - row) * 512); sheet.Apply();
                        float radians = angles[i] * Mathf.Deg2Rad;
                        Vector2 direction = new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
                        for (int sample = 0; sample <= 40; sample++)
                        {
                            Vector3 world = muzzle.position + (Vector3)(direction * (sample * 0.025f));
                            Vector3 uv = camera.WorldToViewportPoint(world);
                            float alpha = sheet.GetPixel(i * 512 + Mathf.Clamp((int)(uv.x * 512), 0, 511),
                                (2 - row) * 512 + Mathf.Clamp((int)(uv.y * 512), 0, 511)).a;
                            minConnectionAlpha = Mathf.Min(minConnectionAlpha, alpha);
                        }
                    }
                    finally { camera.targetTexture = null; RenderTexture.active = previous; RenderTexture.ReleaseTemporary(rt); }
                }
            }
            if (minConnectionAlpha < 0.8f) throw new Exception("Transparent gap in body/laser connection: " + minConnectionAlpha);
            string path = "Tools~/CTJ/Boss23LaserRootPreview.png";
            System.IO.File.WriteAllBytes(path, sheet.EncodeToPNG());
            return new { preview = path, muzzle = muzzle.position.ToString(), frames = 3, angles = 3, minConnectionAlpha };
            void Set(string field, object value) => typeof(DroneDownwardLaserSkill).GetField(field, Private).SetValue(laser, value);
        }
        finally { Object.DestroyImmediate(cameraRoot); Object.DestroyImmediate(sheet); PrefabUtility.UnloadPrefabContents(root); }
    }
}
