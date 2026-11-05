using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyFeedbackPolish
    {
        private const string Zombie = "Assets/CTJ/Prototypes/Prefabs/Enemy01_TwistedArm.prefab";
        private const string Death = "Assets/CTJ/Prototypes/Animations/Enemy01_Zombie_Death.anim";
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };
        private static readonly Color HitColor = new Color(1f, 0.32f, 0.36f, 1f);

        public static object Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Death);
            var spriteKeys = AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip).Single());
            int[] columns = { 0, 2, 3, 4, 5, 6, 7, 7 };
            Check(spriteKeys.Length == columns.Length, "Unexpected death frame layout");
            var xBinding = EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x");
            AnimationCurve xCurve = AnimationUtility.GetEditorCurve(clip, xBinding);
            Keyframe[] keys = xCurve.keys;
            for (int i = 0; i < keys.Length; i++)
            {
                Sprite sprite = (Sprite)spriteKeys[i].value;
                // 마지막 rect는 x=223부터 시작해 32px 셀의 왼쪽 경계 224보다 1px 벗어납니다.
                // rect.x / 32를 버림하면 이전 셀로 잘못 판단하므로 실제 열 번호를 지정합니다.
                float corrected = (sprite.rect.x + sprite.pivot.x - (columns[i] * 32f + 16f)) * 2f / sprite.pixelsPerUnit;
                if (Mathf.Abs(corrected - keys[i].value) > 0.0001f) keys[i].value = corrected;
            }
            xCurve.keys = keys; AnimationUtility.SetEditorCurve(clip, xBinding, xCurve);
            var settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.stopTime = spriteKeys.Last().time;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            EditorUtility.SetDirty(clip); AssetDatabase.SaveAssetIfDirty(clip);

            foreach (string relative in Enemies)
            {
                string path = "Assets/CTJ/Prototypes/" + relative + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    var reaction = new SerializedObject(root.GetComponentInChildren<EnemyHitReactionModule>(true));
                    reaction.FindProperty("hitColor").colorValue = HitColor;
                    reaction.FindProperty("hitColorBlend").floatValue = 0.65f;
                    reaction.ApplyModifiedPropertiesWithoutUndo();
                    var particles = new SerializedObject(root.GetComponentInChildren<EnemyParticleModule>(true));
                    var sprites = reaction.FindProperty("sprites");
                    var surfaces = particles.FindProperty("hitSprites"); surfaces.arraySize = sprites.arraySize;
                    for (int i = 0; i < sprites.arraySize; i++) surfaces.GetArrayElementAtIndex(i).objectReferenceValue = sprites.GetArrayElementAtIndex(i).objectReferenceValue;
                    particles.FindProperty("hitPointInset").floatValue = 0.1f;
                    particles.ApplyModifiedPropertiesWithoutUndo();
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            const string coinPath = "Assets/CTJ/Prototypes/Prefabs/EnemyRewardCoin.prefab";
            GameObject coin = PrefabUtility.LoadPrefabContents(coinPath);
            try
            {
                var data = new SerializedObject(coin.GetComponent<EnemyRewardCoin>());
                data.FindProperty("enableCollectScalePulse").boolValue = true;
                data.FindProperty("collectScalePeak").floatValue = 1.35f;
                data.FindProperty("collectScaleDip").floatValue = 0.7f;
                data.FindProperty("collectScaleDuration").floatValue = 0.24f;
                data.ApplyModifiedPropertiesWithoutUndo(); PrefabUtility.SaveAsPrefabAsset(coin, coinPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(coin); }
            return RunEdit();
        }

        public static object RunEdit()
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Death);
            var x = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"));
            Check(x.keys.All(k => Mathf.Abs(k.value) <= 0.051f), "Death horizontal offset must stay within 0.051 local units");
            Check(Mathf.Abs(x.Evaluate(0.6f)) < 0.001f, "Corpse stays at its original cell origin");
            Check(Mathf.Approximately(AnimationUtility.GetAnimationClipSettings(clip).stopTime, 0.7f), "Preserve death clip timing");
            foreach (string relative in Enemies)
            {
                GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/" + relative + ".prefab");
                foreach (Component component in enemy.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing script " + relative);
                    var property = new SerializedObject(component).GetIterator();
                    while (property.Next(true)) if (property.propertyType == SerializedPropertyType.ObjectReference)
                        Check(property.objectReferenceValue != null || property.objectReferenceInstanceIDValue == 0, "Missing reference " + relative + "/" + property.propertyPath);
                }
                var reaction = new SerializedObject(enemy.GetComponentInChildren<EnemyHitReactionModule>());
                Check(reaction.FindProperty("hitColor").colorValue == HitColor && Mathf.Approximately(reaction.FindProperty("hitColorBlend").floatValue, 0.65f), "Red-white blink");
                var particles = new SerializedObject(enemy.GetComponentInChildren<EnemyParticleModule>());
                Check(particles.FindProperty("hitSprites").arraySize > 0 && particles.FindProperty("hitPointInset").floatValue > 0f, "Body visuals wired");
            }
            return new { passed = true, enemies = 6, lastDeathOffset = x.Evaluate(0.6f), blink = ColorUtility.ToHtmlStringRGB(HitColor) };
        }

        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }

        public static async Task<object> RunPlay()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Isolated Play only");
            var errors = new List<string>();
            Application.LogCallback capture = (message, trace, type) => { if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) errors.Add(message); };
            Application.logMessageReceived += capture;
            float previous = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            try
            {
                var cases = new List<object>();
                foreach (int facing in new[] { 1, -1 }) cases.Add(await HitAndDeath(facing));
                Check(errors.Count == 0, string.Join(" | ", errors));
                return new { passed = true, cases, errors };
            }
            finally { Time.captureDeltaTime = previous; Application.logMessageReceived -= capture; }
        }

        private static async Task<object> HitAndDeath(int facing)
        {
            var stage = new GameObject("Feedback verification"); stage.SetActive(false);
            EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>(Zombie), new Vector3(9000f, 0f), Quaternion.identity, stage.transform);
            enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
            enemy.GetComponentInChildren<EnemyPatrolModule>().enabled = false;
            Rigidbody2D body = enemy.GetComponent<Rigidbody2D>(); body.gravityScale = 0f;
            Transform visual = enemy.transform.Find("Visual"); visual.localScale = new Vector3(facing, 1f, 1f);
            stage.SetActive(true); enemy.SetTarget(null);
            SpriteRenderer sprite = enemy.GetComponentInChildren<Animator>().GetComponent<SpriteRenderer>();
            var results = new List<object>();
            try
            {
                await Wait(0.05f);
                visual.localScale = new Vector3(facing, 1f, 1f);
                foreach (int side in new[] { -1, 1 })
                {
                    Bounds before = sprite.bounds;
                    Vector2 contact = enemy.GetComponent<Collider2D>().ClosestPoint((Vector2)before.center + Vector2.right * side * 3f);
                    Vector2 projected = before.ClosestPoint(new Vector3(contact.x, contact.y, before.center.z));
                    Vector2 expected = Vector2.MoveTowards(projected, before.center, 0.1f);
                    EnemyBloodParticle blood = HitAndGetSpray(enemy, 1f, contact, Vector2.left * side);
                    Check(Vector2.Distance(blood.transform.position, expected) < 0.002f, "Blood uses pre-pulse body bounds and inward contact");
                    var particles = new ParticleSystem.Particle[64]; int count = blood.GetComponent<ParticleSystem>().GetParticles(particles);
                    Check(count > 0 && particles.Take(count).All(p => Vector2.Distance(p.position, expected) <= 0.027f), "Burst begins at corrected contact");
                    Check(sprite.color == HitColor, "Red-white blink applied immediately");
                    var properties = new MaterialPropertyBlock(); sprite.GetPropertyBlock(properties);
                    Check(Mathf.Approximately(properties.GetFloat("_HitBlend"), 0.65f), "Artwork-preserving blend");
                    results.Add(new { side, contact = contact.ToString("F3"), spawn = blood.transform.position.ToString("F3") });
                    await Wait(0.4f);
                    Check(sprite.color == Color.white && sprite.transform.localScale == Vector3.one * 2f, "Hit color and scale restored");
                }
                Bounds lethalBounds = sprite.bounds;
                Vector2 rawPoint = (Vector2)lethalBounds.center + Vector2.right * facing * 2f;
                Vector2 expectedLethal = Vector2.MoveTowards(lethalBounds.ClosestPoint(rawPoint), lethalBounds.center, 0.1f);
                EnemyBloodParticle lethal = HitAndGetSpray(enemy, 100000f, rawPoint, Vector2.left * facing);
                Check(Vector2.Distance(lethal.transform.position, expectedLethal) < 0.002f && enemy.IsPlayingDeathReaction,
                    "Lethal hit works even after hurtbox disabled");
                await Wait(0.4f);
                Check(!enemy.IsPlayingDeathReaction && !body.simulated, "Death follows completed reaction");
                Vector3 rootPosition = enemy.transform.position;
                float lastX = sprite.bounds.center.x, maxStep = 0f;
                float until = Time.time + 0.75f;
                while (Time.time < until)
                {
                    Check(enemy != null && Vector3.Distance(enemy.transform.position, rootPosition) < 0.0001f, "Death root remains stationary");
                    float x = sprite.bounds.center.x;
                    maxStep = Mathf.Max(maxStep, Mathf.Abs(x - lastX)); lastX = x;
                    Check(Mathf.Abs(sprite.bounds.min.y - rootPosition.y) < 0.002f, "Death feet stay on baseline");
                    await Task.Yield();
                }
                Check(maxStep <= 0.16f && Mathf.Abs(sprite.transform.localPosition.x) < 0.001f, "No last-frame corpse teleport: " + maxStep);
                Check(Mathf.Sign(visual.localScale.x) == facing, "Facing preserved through death");
                return new { facing, hits = results, maxDeathFrameStep = maxStep, lastLocalX = sprite.transform.localPosition.x };
            }
            finally
            {
                stage.SetActive(false); Object.Destroy(stage);
                foreach (EnemyBloodParticle blood in Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None)) Object.Destroy(blood.gameObject);
            }
        }

        private static EnemyBloodParticle HitAndGetSpray(EnemyBase enemy, float amount, Vector2 point, Vector2 direction)
        {
            var before = Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None);
            enemy.ApplyDamage(new DamageData { DamageAmount = amount }, point, direction, -direction);
            return Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Except(before).Single();
        }

        private static async Task Wait(float seconds)
        {
            float until = Time.time + seconds; DateTime deadline = DateTime.UtcNow.AddSeconds(10);
            while (Time.time < until) { Check(DateTime.UtcNow < deadline, "Wait timeout"); await Task.Yield(); }
        }

        public static async Task<object> RenderPreview()
        {
            Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) &&
                SceneManager.GetActiveScene().path == "", "Isolated Play only");
            var root = new GameObject("Death frame and blink preview");
            var cameraObject = new GameObject("Preview camera"); cameraObject.transform.SetParent(root.transform);
            Camera camera = cameraObject.AddComponent<Camera>(); camera.orthographic = true; camera.orthographicSize = 1.4f;
            camera.transform.position = new Vector3(0f, 0f, -10f); camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.055f, 0.065f, 0.08f);
            var target = new RenderTexture(1400, 300, 24); camera.targetTexture = target;
            Sprite square = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, Texture2D.whiteTexture.width, Texture2D.whiteTexture.height),
                Vector2.one * 0.5f, Texture2D.whiteTexture.width, 0, SpriteMeshType.FullRect);
            Material unlit = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
            var views = new List<SpriteRenderer>();
            try
            {
                var ground = new GameObject("Baseline"); ground.transform.SetParent(root.transform); ground.transform.position = new Vector3(0f, -0.615f);
                ground.transform.localScale = new Vector3(14f, 0.025f, 1f);
                var line = ground.AddComponent<SpriteRenderer>(); line.sprite = square; line.sharedMaterial = unlit; line.color = Color.gray;
                AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Death);
                for (int i = 0; i < 8; i++)
                {
                    var anchor = new GameObject("Frame " + i); anchor.transform.SetParent(root.transform);
                    anchor.transform.position = new Vector3(-5.25f + i * 1.5f, -0.6f); anchor.transform.localScale = Vector3.one * 2.5f;
                    var mark = new GameObject("Root mark"); mark.transform.SetParent(anchor.transform, false);
                    mark.transform.localScale = new Vector3(0.012f, 0.08f, 1f);
                    var marker = mark.AddComponent<SpriteRenderer>(); marker.sprite = square; marker.sharedMaterial = unlit; marker.color = Color.cyan;
                    var visual = new GameObject("HitVisual"); visual.transform.SetParent(anchor.transform, false); visual.transform.localScale = Vector3.one * 2f;
                    var sprite = visual.AddComponent<SpriteRenderer>(); sprite.sharedMaterial = unlit; sprite.sortingOrder = 10;
                    SampleView(sprite, clip, i * 0.1f + 0.0001f); views.Add(sprite);
                }
                await Task.Yield(); await Task.Yield();
                SavePreview(camera, target, "Enemy01_DeathPolish.png");
                AnimationClip idle = AssetDatabase.LoadAssetAtPath<AnimationClip>("Assets/CTJ/Prototypes/Animations/Enemy01_Zombie_Idle.anim");
                Material flash = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/EnemySolidHitFlash.mat");
                for (int i = 0; i < views.Count; i++)
                {
                    SampleView(views[i], idle, 0f);
                    if (i % 2 == 0) continue;
                    views[i].sharedMaterial = flash; views[i].color = HitColor;
                    var properties = new MaterialPropertyBlock(); properties.SetFloat("_HitBlend", 0.65f); properties.SetColor("_OriginalTint", Color.white);
                    views[i].SetPropertyBlock(properties);
                }
                await Task.Yield(); await Task.Yield();
                SavePreview(camera, target, "EnemyBlinkPolish.png");
                return new { previews = new[] { "Enemy01_DeathPolish.png", "EnemyBlinkPolish.png" } };
            }
            finally { camera.targetTexture = null; Object.Destroy(target); Object.Destroy(root); Object.Destroy(square); }
        }

        private static void SampleView(SpriteRenderer view, AnimationClip clip, float time)
        {
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, AnimationUtility.GetObjectReferenceCurveBindings(clip).Single());
            view.sprite = (Sprite)keys.Last(k => k.time <= time).value;
            float x = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x")).Evaluate(time);
            float y = AnimationUtility.GetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y")).Evaluate(time);
            view.transform.localPosition = new Vector3(x, y, 0f);
            Check(view.sprite != null, "Preview sprite missing");
        }

        private static void SavePreview(Camera camera, RenderTexture target, string file)
        {
            camera.Render(); RenderTexture previous = RenderTexture.active;
            var pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
            try
            {
                RenderTexture.active = target; pixels.ReadPixels(new Rect(0, 0, target.width, target.height), 0, 0); pixels.Apply();
                System.IO.File.WriteAllBytes("Assets/CTJ/Tools~/" + file, pixels.EncodeToPNG());
            }
            finally { RenderTexture.active = previous; Object.Destroy(pixels); }
        }
        public static object Inspect()
        {
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(Death);
            var spriteBinding = AnimationUtility.GetObjectReferenceCurveBindings(clip).Single();
            var keys = AnimationUtility.GetObjectReferenceCurve(clip, spriteBinding);
            GameObject enemy = AssetDatabase.LoadAssetAtPath<GameObject>(Zombie);
            return new {
                frames = keys.Select(k => { var s = (Sprite)k.value; return new { k.time, s.name, rect = s.rect.ToString(),
                    pivot = s.pivot.ToString(), ppu = s.pixelsPerUnit }; }).ToArray(),
                positions = AnimationUtility.GetCurveBindings(clip).Select(b => new { b.propertyName,
                    keys = AnimationUtility.GetEditorCurve(clip,b).keys.Select(k => new { k.time, k.value }).ToArray() }).ToArray(),
                colliders = enemy.GetComponentsInChildren<Collider2D>(true).Select(c => new { c.name, c.enabled,
                    c.isTrigger, c.gameObject.layer, position = c.transform.localPosition.ToString(), offset = c.offset.ToString(),
                    size = c is BoxCollider2D box ? box.size.ToString() : "" }).ToArray()
            };
        }
    }
}
