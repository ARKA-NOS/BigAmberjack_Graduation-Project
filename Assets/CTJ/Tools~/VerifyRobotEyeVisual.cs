using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyRobotEyeVisual
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Bosses/RobotEyeBoss.prefab";
        private const string Art = "Assets/CTJ/Assets/Eyes_V2/";

        public static object ConfigurePrefab()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            foreach (string name in new[] { "EyeGlobe_sprite_1", "Iris6", "Iris1" })
            {
                var importer = (TextureImporter)AssetImporter.GetAtPath(Art + name + ".png");
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                var textureSettings = new TextureImporterSettings(); importer.ReadTextureSettings(textureSettings);
                textureSettings.spriteMeshType = SpriteMeshType.FullRect; importer.SetTextureSettings(textureSettings);
                importer.SaveAndReimport();
            }
            GameObject root = PrefabUtility.LoadPrefabContents(Prefab);
            try
            {
                var boss = root.GetComponent<RobotEyeBoss>();
                Transform visual = root.transform.Find("Visual");
                var globe = (visual.Find("EyeGlobe") ?? visual.Find("Shell")).GetComponent<SpriteRenderer>();
                var iris = (visual.Find("EyeGlobe/Iris") ?? visual.Find("Eye")).GetComponent<SpriteRenderer>();
                float height = globe.bounds.size.y;
                globe.name = "EyeGlobe"; globe.sprite = Sprite("EyeGlobe_sprite_1"); globe.color = Color.white;
                globe.transform.localScale = Vector3.one * (height / globe.sprite.bounds.size.y);
                globe.transform.localPosition = Vector3.zero;
                globe.sortingOrder = -3;
                iris.name = "Iris"; iris.transform.SetParent(globe.transform, false);
                iris.transform.localPosition = Vector3.zero; iris.transform.localRotation = Quaternion.identity;
                iris.transform.localScale = Vector3.one; iris.color = Color.white; iris.sprite = Sprite("Iris6");
                iris.sortingLayerID = globe.sortingLayerID; iris.sortingOrder = -2;
                iris.sharedMaterial = globe.sharedMaterial;
                Transform overlay = iris.transform.Find("RedIris");
                if (overlay == null) { overlay = new GameObject("RedIris").transform; overlay.SetParent(iris.transform, false); }
                var red = overlay.GetComponent<SpriteRenderer>();
                if (red == null) red = overlay.gameObject.AddComponent<SpriteRenderer>();
                red.sprite = Sprite("Iris1"); red.sharedMaterial = iris.sharedMaterial;
                red.sortingLayerID = iris.sortingLayerID; red.sortingOrder = -1;
                red.color = new Color(1f, 1f, 1f, 0f);
                SetReferences(root.GetComponentInChildren<EnemyHitReactionModule>(), "sprites", globe, iris, red);
                SetReferences(root.GetComponentInChildren<EnemyOutlineModule>(), "sprites", globe);
                Transform oldPupil = visual.Find("Pupil");
                if (oldPupil != null) Object.DestroyImmediate(oldPupil.gameObject);
                var module = root.GetComponentInChildren<RobotEyeVisualModule>();
                if (module == null)
                {
                    var go = new GameObject("EyeVisualModule"); go.transform.SetParent(root.transform, false);
                    module = go.AddComponent<RobotEyeVisualModule>();
                }
                var settings = new SerializedObject(module);
                settings.FindProperty("globe").objectReferenceValue = globe;
                settings.FindProperty("iris").objectReferenceValue = iris;
                settings.FindProperty("redIris").objectReferenceValue = red;
                settings.FindProperty("normalIris").objectReferenceValue = Sprite("Iris6");
                settings.FindProperty("afterLaserIris").objectReferenceValue = Sprite("Iris1");
                settings.FindProperty("paddingPixels").floatValue = 5f;
                settings.FindProperty("followSmoothTime").floatValue = 0.08f;
                settings.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, Prefab);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
            return RunEdit();
        }

        public static object RunEdit()
        {
            var source = AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Prefab);
            var globe = source.transform.Find("Visual/EyeGlobe").GetComponent<SpriteRenderer>();
            var iris = source.transform.Find("Visual/EyeGlobe/Iris").GetComponent<SpriteRenderer>();
            var red = iris.transform.Find("RedIris").GetComponent<SpriteRenderer>();
            Check(globe.sprite == Sprite("EyeGlobe_sprite_1") && iris.sprite == Sprite("Iris6"), "Sprite references");
            Check(Mathf.Abs(globe.bounds.size.y - 2.2f) < 0.001f, "Preserve original boss diameter");
            Check(globe.color == Color.white && iris.color == Color.white, "Original pixel colors");
            Check(red.sprite == Sprite("Iris1") && red.color.a == 0f, "Red overlay starts transparent");
            Check(source.transform.Find("Visual/Pupil") == null && source.GetComponentInChildren<RobotEyeVisualModule>() != null,
                "Replace placeholder with two sprites and module");
            Check(new SerializedObject(source).FindProperty("bodyVisual").objectReferenceValue == globe, "Arena still uses globe radius");
            Check(globe.sortingOrder == -3 && iris.sortingOrder == -2, "Wall > eye > warning order");
            foreach (Transform child in source.GetComponentsInChildren<Transform>(true))
                Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) == 0, "Missing script: " + child.name);
            foreach (Component component in source.GetComponentsInChildren<Component>(true))
            {
                var property = new SerializedObject(component).GetIterator();
                while (property.Next(true))
                    if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0,
                            "Missing reference: " + component.GetType().Name + "/" + property.propertyPath);
            }
            return new { passed = true, diameter = globe.bounds.size.y, eyePixels = globe.sprite.rect.size.ToString(),
                irisPixels = iris.sprite.rect.size.ToString(), scope = "sprites / module / dimensions / references / sorting" };
        }

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Isolated Play scene required");
            var errors = new List<string>();
            Application.LogCallback capture = (m, s, t) => { if (t == LogType.Error || t == LogType.Assert || t == LogType.Exception) errors.Add(m); };
            Application.logMessageReceived += capture;
            var stage = new GameObject("Eye visual verification"); stage.SetActive(false);
            RenderTexture rt = null;
            try
            {
                var boss = Object.Instantiate(AssetDatabase.LoadAssetAtPath<RobotEyeBoss>(Prefab), new Vector3(6000f, 4f), Quaternion.identity, stage.transform);
                Set(boss, "firstVolleyDelay", 999f);
                boss.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                boss.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                var target = new GameObject("Eye target"); target.transform.SetParent(stage.transform); target.layer = 7;
                target.transform.position = new Vector3(6004f, 4f);
                target.AddComponent<BoxCollider2D>().isTrigger = true;
                target.AddComponent<EnemyDamageProbe>();
                stage.SetActive(true); boss.SetTarget(target.transform);
                var module = boss.GetModule<RobotEyeVisualModule>();
                var globe = boss.transform.Find("Visual/EyeGlobe").GetComponent<SpriteRenderer>();
                var iris = globe.transform.Find("Iris").GetComponent<SpriteRenderer>();
                var red = iris.transform.Find("RedIris").GetComponent<SpriteRenderer>();
                var firePoint = boss.transform.Find("Visual/FirePoint");
                Vector3 fireLocal = firePoint.localPosition;
                await Until(() => boss.CurrentState == RobotEyeBossState.Waiting, 10);
                for (int i = 0; i < 8; i++)
                {
                    Vector2 direction = new Vector2(Mathf.Cos(i * Mathf.PI / 4f), Mathf.Sin(i * Mathf.PI / 4f));
                    target.transform.position = boss.transform.position + (Vector3)(direction * 10f);
                    await Wait(0.4f, () => CheckPadding(globe, iris, 5f));
                    Vector2 eyeDirection = iris.bounds.center - globe.bounds.center;
                    Check(Vector2.Dot(eyeDirection.normalized, direction) > 0.99f, "8-direction gaze " + i);
                }
                Check(iris.sprite == Sprite("Iris6") && firePoint.localPosition == fireLocal, "Normal iris / unchanged muzzle");
                boss.transform.localScale = new Vector3(1.3f, 0.8f, 1f);
                target.transform.position = boss.transform.position + new Vector3(-8f, 8f);
                await Wait(0.35f, () => CheckPadding(globe, iris, 5f));
                boss.transform.localScale = Vector3.one;
                Set(module, "paddingPixels", 8f); await Wait(0.15f, () => CheckPadding(globe, iris, 8f));
                Set(module, "paddingPixels", 40f); await Wait(0.1f);
                Check(IrisOffset(globe, iris).magnitude < 0.0001f, "Oversized padding holds center");
                Set(module, "paddingPixels", 5f);
                boss.SetTarget(null); await Wait(0.4f);
                Check(IrisOffset(globe, iris).magnitude < 0.0001f, "Missing target recenters safely");
                boss.SetTarget(target.transform);
                target.SetActive(false); await Wait(0.25f); target.SetActive(true);
                target.transform.position = boss.transform.position + new Vector3(8f, -4f);
                await Wait(0.4f);

                foreach (Transform child in boss.GetComponentsInChildren<Transform>(true)) child.gameObject.layer = 31;
                var cameraObject = new GameObject("Eye preview camera"); cameraObject.transform.SetParent(stage.transform);
                var camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.45f;
                camera.backgroundColor = new Color(0.06f, 0.07f, 0.08f, 1f); camera.clearFlags = CameraClearFlags.SolidColor;
                camera.cullingMask = 1 << 31; camera.allowHDR = false; camera.allowMSAA = false;
                cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                rt = new RenderTexture(480, 480, 24, RenderTextureFormat.ARGB32); camera.targetTexture = rt;
                camera.transform.position = boss.transform.position + Vector3.back * 10f;
                await Frames(3); string normalPreview = SavePreview(rt, "RobotEye_Normal.png");

                boss.ApplyDamage(new DamageData { DamageAmount = 1500f }, boss.transform.position, Vector2.left, Vector2.right);
                Check(boss.HasUsedLaserPhase && !boss.HasFinishedLaserPhase && iris.sprite == Sprite("Iris6"), "Laser start must not change iris yet");
                var seenStates = new HashSet<RobotEyeBossState>();
                var laser = boss.GetComponentInChildren<BossLaserSkill>();
                int firingStartFrame = -1;
                bool sawPartialRed = false, checkedLaserAim = false;
                DateTime deadline = DateTime.UtcNow.AddSeconds(35);
                while (!boss.HasFinishedLaserPhase)
                {
                    Check(DateTime.UtcNow < deadline && EditorApplication.isPlaying, "Laser phase timeout: " + boss.CurrentState);
                    seenStates.Add(boss.CurrentState);
                    Check(iris.sprite == Sprite("Iris6"), "Changed iris before phase completed");
                    if (laser.IsFiring)
                    {
                        if (firingStartFrame < 0) firingStartFrame = Time.frameCount;
                        target.transform.position = boss.transform.position + Vector3.up * 8f;
                        if (Time.frameCount > firingStartFrame + 2)
                        {
                            Check(Vector2.Angle(iris.bounds.center - globe.bounds.center, laser.BeamDirection) < 0.5f, "Laser gaze must stay on beam, not target");
                            checkedLaserAim = true;
                        }
                    }
                    else firingStartFrame = -1;
                    if (boss.CurrentState == RobotEyeBossState.ReturnCenter && module.RedBlend > 0.01f && module.RedBlend < 0.99f)
                        sawPartialRed = true;
                    Check(Mathf.Abs(red.color.a - module.RedBlend) < 0.001f, "Overlay alpha matches blend");
                    CheckPadding(globe, iris, 5f);
                    await Task.Yield();
                }
                await Frames(3);
                Check(boss.CompletedTraversals == 3 && !boss.IsInvulnerable && seenStates.Contains(RobotEyeBossState.LaserSweep) &&
                    seenStates.Contains(RobotEyeBossState.ReturnCenter), "Real three traversals and center return");
                Check(red.sprite == Sprite("Iris1") && red.color.a == 1f && sawPartialRed && checkedLaserAim, "Gradual red overlay / laser gaze");
                camera.transform.position = boss.transform.position + Vector3.back * 10f;
                await Wait(0.4f, () => CheckPadding(globe, iris, 5f));
                string afterPreview = SavePreview(rt, "RobotEye_AfterLaser.png");
                boss.SetTarget(null); await Wait(0.4f);
                Check(red.color.a == 1f && IrisOffset(globe, iris).magnitude < 0.0001f, "Phase appearance persists without target");
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, paddingPixels = 5, directions = 8, completedTraversals = boss.CompletedTraversals,
                    normalPreview, afterPreview, sawPartialRed, checkedLaserAim, errors };
            }
            finally
            {
                Application.logMessageReceived -= capture;
                stage.SetActive(false); Object.Destroy(stage);
                if (rt != null) { rt.Release(); Object.Destroy(rt); }
            }
        }

        private static Vector2 IrisOffset(SpriteRenderer globe, SpriteRenderer iris) =>
            (Vector2)iris.transform.localPosition + (Vector2)Vector3.Scale(iris.sprite.bounds.center, iris.transform.localScale) - (Vector2)globe.sprite.bounds.center;
        private static void CheckPadding(SpriteRenderer globe, SpriteRenderer iris, float pixels)
        {
            float eyeRadius = Mathf.Min(globe.sprite.rect.width, globe.sprite.rect.height) * 0.5f / globe.sprite.pixelsPerUnit;
            Vector3 irisSize = Vector3.Scale(iris.sprite.bounds.extents, iris.transform.localScale);
            float total = IrisOffset(globe, iris).magnitude + Mathf.Max(Mathf.Abs(irisSize.x), Mathf.Abs(irisSize.y));
            Check(total <= eyeRadius - pixels / globe.sprite.pixelsPerUnit + 0.0001f, "Iris crossed inner padding");
        }
        private static Sprite Sprite(string name) => AssetDatabase.LoadAllAssetsAtPath(Art + name + ".png").OfType<Sprite>().Single();
        private static void SetReferences(Object obj, string field, params Object[] values)
        {
            var so = new SerializedObject(obj); var p = so.FindProperty(field); p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Set(Object obj, string field, float value)
        {
            var so = new SerializedObject(obj); so.FindProperty(field).floatValue = value; so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static string SavePreview(RenderTexture rt, string filename)
        {
            RenderTexture previous = RenderTexture.active;
            var texture = new Texture2D(rt.width, rt.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = rt; texture.ReadPixels(new Rect(0, 0, rt.width, rt.height), 0, 0); texture.Apply();
                string path = Application.dataPath + "/CTJ/Tools~/" + filename; File.WriteAllBytes(path, texture.EncodeToPNG()); return path;
            }
            finally { RenderTexture.active = previous; Object.Destroy(texture); }
        }
        private static async Task Frames(int count) { int end = Time.frameCount + count; await Until(() => Time.frameCount >= end, 5); }
        private static async Task Wait(float seconds, Action check = null)
        {
            float end = Time.time + seconds; await Frames(1);
            await Until(() => { check?.Invoke(); return Time.time >= end; }, 5);
        }
        private static async Task Until(Func<bool> condition, int seconds)
        {
            DateTime deadline = DateTime.UtcNow.AddSeconds(seconds);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < deadline, "Verification timeout"); await Task.Yield(); }
        }
        private static void Check(bool valid, string message) { if (!valid) throw new InvalidOperationException(message); }
    }
}
