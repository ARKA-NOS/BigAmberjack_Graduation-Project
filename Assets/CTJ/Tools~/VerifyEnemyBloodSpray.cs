using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using CTJ.Enemies;
using DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Rendering.Universal;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class VerifyEnemyBloodSpray
    {
        private const string Prefab = "Assets/CTJ/Prototypes/Particles/HitBloodSpray.prefab";
        private static readonly string[] Enemies = { "Prefabs/Catto_Melee", "Prefabs/MadGhost_Ranged", "Prefabs/Enemy01_TwistedArm",
            "Prefabs/Enemy02_ProjectileRobot", "Prefabs/Enemy04_MechanicArcher", "Bosses/RobotEyeBoss" };
        private const BindingFlags Fields = BindingFlags.Instance | BindingFlags.NonPublic;

        public static object RunEdit()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            ParticleSystem prefab = AssetDatabase.LoadAssetAtPath<ParticleSystem>(Prefab);
            Check(prefab.GetComponent<EnemyBloodParticle>() != null && prefab.GetComponent<EnemyHitParticle>() != null, "Blood controllers");
            var renderer = prefab.GetComponent<ParticleSystemRenderer>();
            Check(renderer.renderMode == ParticleSystemRenderMode.Mesh && renderer.meshCount == 4 &&
                !renderer.sharedMaterial.HasProperty("_MainTex") && renderer.sharedMaterial.shader.isSupported,
                "Four original polygon meshes, no blood texture: mode=" + renderer.renderMode + " meshes=" + renderer.meshCount +
                " shader=" + renderer.sharedMaterial.shader.isSupported);
            Check(!ShaderUtil.GetShaderMessages(renderer.sharedMaterial.shader).Any(message => message.severity.ToString() == "Error"), "Blood shader compilation");
            Mesh[] meshes = new Mesh[renderer.meshCount]; renderer.GetMeshes(meshes);
            Check(meshes.All(mesh => mesh.GetVertexAttributeFormat(UnityEngine.Rendering.VertexAttribute.Color) == UnityEngine.Rendering.VertexAttributeFormat.UNorm8),
                "Particle mesh vertex colors use Color32");
            foreach (string path in Enemies)
            {
                EnemyBase enemy = AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + path + ".prefab");
                foreach (Component component in enemy.GetComponentsInChildren<Component>(true))
                {
                    Check(component != null, "Missing script " + path);
                    SerializedProperty field = new SerializedObject(component).GetIterator();
                    while (field.Next(true)) if (field.propertyType == SerializedPropertyType.ObjectReference)
                        Check(field.objectReferenceValue != null || field.objectReferenceInstanceIDValue == 0, "Broken reference " + path + "/" + field.propertyPath);
                }
                SerializedObject data = new SerializedObject(enemy.GetComponentInChildren<EnemyParticleModule>());
                SerializedProperty entries = data.FindProperty("hitEffects");
                Check(entries.arraySize == 1 && entries.GetArrayElementAtIndex(0).FindPropertyRelative("prefab").objectReferenceValue == prefab &&
                    entries.GetArrayElementAtIndex(0).FindPropertyRelative("randomPrefabs").arraySize == 0, "One shared blood effect " + path);
                Check(data.FindProperty("maxConcurrentEffects").intValue == 24, "Bounded blood burst count");
            }
            return new { passed = true, enemies = 6, meshes = 4 };
        }

        public static async Task<object> RunPlay()
        {
            RequirePlay();
            float previous = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            try
            {
                object[] surfaces = await Task.WhenAll(Floor(), Wall(), Platform(), ThinWall(), Corner(), NoTerrain(), LateLanding());
                object[] enemies = await Task.WhenAll(Enemies.Select((path, i) => Enemy(path, 4000f + i * 100f)));
                object direction = await DealerDirection();
                object scene = await SceneCleanup();
                return new { passed = true, surfaces, enemies, direction, scene };
            }
            finally { Time.captureDeltaTime = previous; }
        }

        private static async Task<object> Floor()
        {
            using (var stage = new Stage(1000f))
            {
                stage.Box("Floor", new Vector2(0f, -0.1f), new Vector2(30f, 0.2f));
                EnemyBloodParticle blood = stage.Burst(new Vector2(0f, 1.2f));
                int count = Drops(blood).Length;
                Check(count >= 14 && count <= 20, "Random count");
                await Wait(0.14f);
                var airborne = Drops(blood);
                Check(airborne.Average(d => d.Position.x) > 1000.25f && airborne.Max(d => d.Position.y) - airborne.Min(d => d.Position.y) > 0.4f,
                    "Wide forward fan");
                float initialVy = airborne.Average(d => d.Velocity.y);
                await Wait(0.08f);
                Check(Drops(blood).Where(d => d.State == "Air").Average(d => d.Velocity.y) < initialVy - 0.5f, "Gravity curves trajectory");
                await Until(() => Drops(blood).All(d => d.State == "Floor"), "All floor contacts");
                var landed = Drops(blood); float age = Read<float>(blood, "_age");
                float minFade = landed.Min(d => d.FadeAt), maxFade = landed.Max(d => d.FadeAt);
                Check(landed.Max(d => d.Position.x) - 1000f < 5.5f && landed.Max(d => d.Position.x) - 1000f > 1f, "Medium spray reach");
                await Wait(0.35f);
                Check(Drops(blood).Zip(landed, (a, b) => Vector2.Distance(a.Position, b.Position) < 0.001f).All(same => same), "Floor stains stay still");
                await Wait(Mathf.Max(0f, minFade - Read<float>(blood, "_age") - 0.08f));
                Check(Rendered(blood).All(p => p.startColor.a == 255), "No fade before contact hold");
                await Wait(0.28f);
                Check(Rendered(blood).Any(p => p.startColor.a > 0 && p.startColor.a < 240), "Gradual fade");
                await Wait(maxFade + 0.7f - Read<float>(blood, "_age"));
                Check(blood == null, "Root cleaned after all stains end");
                return new { passed = true, scenario = "floor / gravity / spread / hold / fade", count, firstFade = minFade, lastFade = maxFade, allLandedAt = age };
            }
        }

        private static async Task<object> Wall()
        {
            using (var stage = new Stage(1200f))
            {
                stage.Box("Wall", new Vector2(1f, 0f), new Vector2(0.2f, 40f));
                EnemyBloodParticle blood = stage.Burst(new Vector2(0f, 2f));
                await Until(() => Drops(blood).All(d => d.State == "Wall"), "Wall contacts");
                var before = Drops(blood);
                await Wait(0.5f); var after = Drops(blood);
                float slide = before.Average(d => d.Position.y) - after.Average(d => d.Position.y);
                Check(slide >= 0.09f && slide <= 0.14f && after.All(d => d.State == "Wall"), "Slow wall slide " + slide);
                Check(after.Zip(before, (a, b) => Mathf.Abs(a.Position.x - b.Position.x) < 0.002f && a.FadeAt == b.FadeAt).All(v => v), "Wall support / hold timer not reset");
                float latest = after.Max(d => d.FadeAt);
                await Wait(latest + 0.7f - Read<float>(blood, "_age"));
                Check(blood == null, "Wall traces fade and clean");
                return new { passed = true, scenario = "wall slide / fade", slide };
            }
        }

        private static async Task<object> Platform()
        {
            using (var stage = new Stage(1400f))
            {
                BoxCollider2D floor = stage.Box("One way platform", new Vector2(0f, 1f), new Vector2(10f, 0.1f));
                floor.gameObject.layer = 10;
                PlatformEffector2D effector = floor.gameObject.AddComponent<PlatformEffector2D>(); effector.useOneWay = true;
                floor.usedByEffector = true;
                EnemyBloodParticle blood = stage.Burst(Vector2.zero, 0f, b => { Set(b, "speedRange", Vector2.zero); Set(b, "upwardSpeed", 8f); });
                await Wait(0.22f);
                Check(Drops(blood).All(d => d.State == "Air" && d.Position.y > 1.1f), "Pass platform from below");
                await Until(() => Drops(blood).All(d => d.State == "Floor"), "Land on platform");
                Check(Drops(blood).All(d => d.Position.y > 1.05f && d.Position.y < 1.12f), "Platform top contact");
                return new { passed = true, scenario = "one-way platform" };
            }
        }

        private static async Task<object> ThinWall()
        {
            using (var stage = new Stage(1600f))
            {
                stage.Box("Thin wall", new Vector2(1f, 0f), new Vector2(0.02f, 20f));
                stage.Box("Ignored trigger", new Vector2(0.3f, 0f), new Vector2(0.1f, 20f)).isTrigger = true;
                EnemyBloodParticle blood = stage.Burst(Vector2.zero, 0f, b => { Set(b, "speedRange", Vector2.one * 100f); Set(b, "spreadAngle", 0f); });
                await Wait(0.06f);
                Check(Drops(blood).All(d => d.State == "Wall" && d.Position.x > 1600.8f && d.Position.x < 1601f), "Swept collision, ignore trigger");
                return new { passed = true, scenario = "thin wall / trigger" };
            }
        }

        private static async Task<object> Corner()
        {
            using (var stage = new Stage(1800f))
            {
                stage.Box("Wall", new Vector2(0.8f, 2f), new Vector2(0.1f, 4f));
                stage.Box("Floor", new Vector2(0f, -0.1f), new Vector2(10f, 0.2f));
                EnemyBloodParticle blood = stage.Burst(new Vector2(0f, 1f), 0f, b => { Set(b, "wallSlideSpeed", 2f); Set(b, "spreadAngle", 0f); Set(b, "upwardSpeed", 0f); });
                await Until(() => Drops(blood).Any(d => d.State == "Wall"), "Corner wall first");
                await Until(() => Drops(blood).All(d => d.State == "Floor"), "Slide to floor");
                Check(Drops(blood).All(d => d.Position.y > 0f), "Corner does not sink through floor");
                return new { passed = true, scenario = "wall to floor" };
            }
        }

        private static async Task<object> NoTerrain()
        {
            using (var stage = new Stage(2000f))
            {
                EnemyBloodParticle blood = stage.Burst(Vector2.zero, 180f);
                await Wait(0.2f);
                Check(Drops(blood).Average(d => d.Position.x) < 1999.6f, "Left-facing fan");
                await Wait(2.5f); Check(blood == null, "No terrain fallback cleanup");
                return new { passed = true, scenario = "left / no terrain cleanup" };
            }
        }

        private static async Task<object> LateLanding()
        {
            using (var stage = new Stage(2200f))
            {
                EnemyBloodParticle blood = stage.Burst(new Vector2(0f, 0.1f), 0f, b =>
                {
                    Set(b, "speedRange", Vector2.zero); Set(b, "upwardSpeed", 0f); Set(b, "gravityScale", 0f);
                    Set(b, "maxAirTime", 0.2f); Set(b, "traceDuration", Vector2.one * 3f);
                });
                await Wait(0.66f);
                Check(Rendered(blood).All(p => p.startColor.a < 80), "Air fallback already fading");
                stage.Box("Late floor", new Vector2(0f, -0.1f), new Vector2(10f, 0.2f));
                Physics2D.SyncTransforms(); Set(blood, "gravityScale", 10f);
                await Until(() => Drops(blood).Length > 0 && Drops(blood).All(d => d.State == "Floor"), "Late landing");
                float lastFade = Drops(blood).Max(d => d.FadeAt);
                await Wait(lastFade + 0.3f - Read<float>(blood, "_age"));
                Check(blood != null && Rendered(blood).Any(p => p.startColor.a > 0), "Lifetime guard must preserve late landing fade");
                await Wait(0.45f); Check(blood == null, "Late landing cleanup");
                return new { passed = true, scenario = "late landing extends lifetime through full fade" };
            }
        }

        private static async Task<object> Enemy(string path, float x)
        {
            using (var stage = new Stage(x))
            {
                stage.Box("Floor", new Vector2(0f, -0.1f), new Vector2(30f, 0.2f));
                EnemyBase enemy = stage.Enemy(path, new Vector2(0f, 1.5f));
                Hit(enemy, 100000f);
                EnemyBloodParticle[] blood = Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None)
                    .Where(b => Mathf.Abs(b.transform.position.x - x) < 20f).ToArray();
                Check(blood.Length == 1 && enemy.IsDead && enemy.IsPlayingDeathReaction && blood[0].transform.parent == null, "One detached lethal spray " + path);
                Check(blood[0].gameObject.scene == enemy.gameObject.scene, "Same scene");
                await Wait(0.75f);
                Check(enemy == null && blood[0] != null && Rendered(blood[0]).Length > 0, "Blood survives owner " + path);
                await Wait(5.6f); Check(blood[0] == null, "After-death stains clean " + path);
                return new { passed = true, path };
            }
        }

        private static async Task<object> DealerDirection()
        {
            using (var stage = new Stage(4800f))
            {
                EnemyBase dealer = stage.Enemy(Enemies[2], new Vector2(-2f, 0f));
                EnemyBase victim = stage.Enemy(Enemies[2], Vector2.zero);
                victim.ApplyDamage(new DamageData { DamageAmount = 0.01f, Dealer = dealer }, victim.transform.position, Vector2.left, Vector2.right);
                var blood = Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Single(b => Mathf.Abs(b.transform.position.x - 4800f) < 1f);
                Vector2 expected = (victim.GetModule<EnemyParticleModule>().transform.position - dealer.transform.position).normalized;
                Check(Vector2.Dot(blood.transform.right, expected) > 0.999f, "Away from dealer even when supplied direction is reversed");
                var particleModule = victim.GetModule<EnemyParticleModule>();
                particleModule.enabled = false; Hit(victim, 0.01f);
                Check(Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Count(b => Mathf.Abs(b.transform.position.x - 4800f) < 1f) == 1, "Disabled module");
                particleModule.enabled = true;
                for (int i = 0; i < 35; i++) Hit(victim, 0.01f);
                Check(Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Count(b => b.isActiveAndEnabled && Mathf.Abs(b.transform.position.x - 4800f) < 1f) == 24, "Burst cap");
                await Wait(0.05f);
                return new { passed = true, scenario = "direction / disable / burst cap" };
            }
        }

        private static async Task<object> SceneCleanup()
        {
            Scene scene = SceneManager.CreateScene("CTJ blood scene cleanup");
            using (var stage = new Stage(5000f))
            {
                EnemyBloodParticle blood = stage.Burst(Vector2.zero);
                SceneManager.MoveGameObjectToScene(stage.Root, scene);
                AsyncOperation unload = SceneManager.UnloadSceneAsync(scene);
                await Until(() => unload.isDone, "Scene unload");
                Check(blood == null, "No stains survive scene unload");
                return new { passed = true, scenario = "scene cleanup" };
            }
        }

        public static async Task<object> RenderPreview()
        {
            RequirePlay();
            float previousStep = Time.captureDeltaTime; Time.captureDeltaTime = 1f / 60f;
            RenderTexture target = new RenderTexture(1440, 400, 24);
            Texture2D pixels = null;
            GameObject cameraObject = new GameObject("Blood spray proof camera");
            Material groundMaterial = new Material(Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default"));
            groundMaterial.SetColor("_Color", new Color(0.15f, 0.18f, 0.21f));
            try
            {
                using (var air = new Stage(0f))
                using (var floor = new Stage(4.8f))
                using (var wall = new Stage(9.6f))
                {
                    foreach (Stage stage in new[] { air, floor, wall })
                    {
                        stage.Box("Floor", new Vector2(0f, -0.15f), new Vector2(4.5f, 0.3f));
                        Quad(stage, new Vector2(0f, -0.15f), new Vector2(4.5f, 0.3f), groundMaterial);
                    }
                    wall.Box("Wall", new Vector2(0.9f, 1.4f), new Vector2(0.25f, 3.1f));
                    Quad(wall, new Vector2(0.9f, 1.4f), new Vector2(0.25f, 3.1f), groundMaterial);
                    floor.Burst(new Vector2(-1.5f, 1f));
                    wall.Burst(new Vector2(-0.4f, 1.7f));
                    await Wait(1.35f);
                    air.Burst(new Vector2(-1.2f, 1.3f));
                    await Wait(0.18f);
                    Camera camera = cameraObject.AddComponent<Camera>();
                    camera.transform.position = new Vector3(4.8f, 1.2f, -10f);
                    camera.orthographic = true; camera.orthographicSize = 2f;
                    camera.targetTexture = target; camera.clearFlags = CameraClearFlags.SolidColor;
                    camera.backgroundColor = new Color(0.025f, 0.035f, 0.045f);
                    camera.allowHDR = false; camera.allowMSAA = false;
                    cameraObject.AddComponent<UniversalAdditionalCameraData>().renderPostProcessing = false;
                    await Wait(0.05f);
                    RenderTexture previousTarget = RenderTexture.active;
                    try
                    {
                        RenderTexture.active = target; pixels = new Texture2D(target.width, target.height, TextureFormat.RGBA32, false);
                        pixels.ReadPixels(new Rect(0f, 0f, target.width, target.height), 0, 0); pixels.Apply();
                    }
                    finally { RenderTexture.active = previousTarget; }
                    Color32[] colors = pixels.GetPixels32(); int[] red = new int[3];
                    for (int y = 0; y < 400; y++) for (int x = 0; x < 1440; x++)
                    {
                        Color32 color = colors[y * 1440 + x];
                        if (color.r > 70 && color.r > color.g * 2 && color.r > color.b * 2) red[x / 480]++;
                    }
                    string output = Application.dataPath + "/CTJ/Tools~/EnemyBloodSprayPreview.png";
                    System.IO.File.WriteAllBytes(output, pixels.EncodeToPNG());
                    Check(red.All(count => count > 20), "Visible air / floor / wall particles " + string.Join(",", red));
                    return new { passed = true, image = output, redPixels = red };
                }
            }
            finally
            {
                Time.captureDeltaTime = previousStep;
                Object.Destroy(cameraObject); Object.Destroy(target); Object.Destroy(groundMaterial);
                if (pixels != null) Object.Destroy(pixels);
            }
        }

        private static void Quad(Stage stage, Vector2 point, Vector2 size, Material material)
        {
            GameObject quad = GameObject.CreatePrimitive(PrimitiveType.Quad); quad.transform.SetParent(stage.Root.transform);
            Object.Destroy(quad.GetComponent<Collider>());
            quad.transform.position = new Vector3(stage.X + point.x, point.y, 0.1f);
            quad.transform.localScale = new Vector3(size.x, size.y, 1f);
            quad.GetComponent<Renderer>().sharedMaterial = material;
        }

        private sealed class Stage : IDisposable
        {
            public GameObject Root = new GameObject("Blood verification");
            public readonly float X;
            public Stage(float x) { X = x; }
            public BoxCollider2D Box(string name, Vector2 point, Vector2 size)
            {
                GameObject go = new GameObject(name); go.transform.SetParent(Root.transform); go.layer = 6;
                go.transform.position = new Vector3(X + point.x, point.y);
                BoxCollider2D box = go.AddComponent<BoxCollider2D>(); box.size = size; return box;
            }
            public EnemyBloodParticle Burst(Vector2 point, float angle = 0f, Action<EnemyBloodParticle> configure = null)
            {
                ParticleSystem particle = Object.Instantiate(AssetDatabase.LoadAssetAtPath<ParticleSystem>(Prefab),
                    new Vector3(X + point.x, point.y), Quaternion.Euler(0f, 0f, angle), Root.transform);
                EnemyBloodParticle blood = particle.GetComponent<EnemyBloodParticle>(); configure?.Invoke(blood);
                Physics2D.SyncTransforms();
                typeof(EnemyHitParticle).GetMethod("Play", Fields).Invoke(particle.GetComponent<EnemyHitParticle>(), new object[] { particle, 0.1f });
                return blood;
            }
            public EnemyBase Enemy(string path, Vector2 position)
            {
                GameObject holder = new GameObject("Enemy holder"); holder.transform.SetParent(Root.transform); holder.SetActive(false);
                EnemyBase enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<EnemyBase>("Assets/CTJ/Prototypes/" + path + ".prefab"),
                    new Vector3(X + position.x, position.y), Quaternion.identity, holder.transform);
                enemy.GetComponentInChildren<EnemyCurrencyRewardModule>().enabled = false;
                enemy.GetComponentInChildren<EnemyDeathModule>().enabled = false;
                EnemyPatrolModule patrol = enemy.GetComponentInChildren<EnemyPatrolModule>(); if (patrol != null) patrol.enabled = false;
                Set(enemy, "deathDespawnDelay", 0f); Set(enemy, "moveSpeed", 0f); Set(enemy, "detectionRange", 0f);
                enemy.GetComponent<Rigidbody2D>().gravityScale = 0f;
                holder.SetActive(true); Physics2D.SyncTransforms(); return enemy;
            }
            public void Dispose()
            {
                if (Root != null) { Root.SetActive(false); Object.Destroy(Root); }
                foreach (EnemyBloodParticle b in Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None))
                    if (Mathf.Abs(b.transform.position.x - X) < 30f) { b.gameObject.SetActive(false); Object.Destroy(b.gameObject); }
            }
        }

        private sealed class Snapshot
        {
            public Vector2 Position, Velocity;
            public float FadeAt;
            public string State;
        }
        private static Snapshot[] Drops(EnemyBloodParticle blood)
        {
            if (blood == null) return Array.Empty<Snapshot>();
            Array drops = Read<Array>(blood, "_drops"); int count = Read<int>(blood, "_count");
            return Enumerable.Range(0, count).Select(i => {
                object drop = drops.GetValue(i); Type type = drop.GetType();
                return new Snapshot { Position = (Vector2)type.GetField("Position").GetValue(drop), Velocity = (Vector2)type.GetField("Velocity").GetValue(drop),
                    FadeAt = (float)type.GetField("FadeAt").GetValue(drop), State = type.GetField("State").GetValue(drop).ToString() };
            }).ToArray();
        }
        private static ParticleSystem.Particle[] Rendered(EnemyBloodParticle blood)
        {
            ParticleSystem.Particle[] particles = new ParticleSystem.Particle[64];
            int count = blood.GetComponent<ParticleSystem>().GetParticles(particles); return particles.Take(count).ToArray();
        }
        private static T Read<T>(object target, string field) => (T)target.GetType().GetField(field, Fields).GetValue(target);
        private static void Hit(EnemyBase enemy, float amount) => enemy.ApplyDamage(new DamageData { DamageAmount = amount }, enemy.transform.position, Vector2.right, Vector2.left);
        private static void Set(Object target, string field, object value)
        {
            var data = new SerializedObject(target); var p = data.FindProperty(field);
            if (value is float f) p.floatValue = f; else if (value is Vector2 v) p.vector2Value = v; else p.boolValue = (bool)value;
            data.ApplyModifiedPropertiesWithoutUndo();
        }
        private static async Task Wait(float seconds) { float end = Time.time + Mathf.Max(0f, seconds); await Until(() => Time.time >= end, "Wait"); }
        private static async Task Until(Func<bool> condition, string label)
        {
            DateTime timeout = DateTime.UtcNow.AddSeconds(20);
            while (!condition()) { Check(EditorApplication.isPlaying && DateTime.UtcNow < timeout, "Timeout " + label); await Task.Yield(); }
        }
        private static void RequirePlay() => Check(EditorApplication.isPlaying && SessionState.GetBool("CTJ.Enemy01.Verifying", false) && SceneManager.GetActiveScene().path == "", "Isolated Play only");
        private static void Check(bool condition, string message) { if (!condition) throw new InvalidOperationException(message); }
    }
}
