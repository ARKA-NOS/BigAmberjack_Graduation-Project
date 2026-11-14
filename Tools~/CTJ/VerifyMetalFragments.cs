using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using _Scripts.Agent.Enemies;
using _Scripts.DevLib.BattleSystem;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

public static class VerifyMetalFragments
{
    private const string MetalPath = "Assets/_Prefabs/Enemies/Effects/HitMetalFragments.prefab";
    private const string BurstPath = "Assets/_Prefabs/Enemies/Effects/DeathMetalBurst.prefab";
    private const string RobotPath = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy02/Enemy02_ProjectileRobot.prefab";
    private const string ArcherPath = "Assets/_Prefabs/Enemies/Legacy/Enemy/Enemy04/Enemy04_MechanicArcher.prefab";

    private static void Check(bool value, string message)
    {
        if (!value) throw new Exception(message);
    }

    public static object Assets()
    {
        var root = AssetDatabase.LoadAssetAtPath<GameObject>(MetalPath);
        Check(root != null && root.GetComponent<EnemyMetalFragmentParticle>() != null &&
            root.GetComponent<EnemyHitParticle>() != null && root.GetComponent<EnemyBloodParticle>() == null,
            "Metal effect prefab components are wrong");
        Check(GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(root) == 0, "Missing metal prefab script");
        var emitter = new SerializedObject(root.GetComponent<EnemyMetalFragmentParticle>());
        Check(Mathf.Approximately(emitter.FindProperty("attachmentChance").floatValue, 0.25f), "Attachment chance is not 25%");
        var colors = emitter.FindProperty("attachmentColors");
        Check(colors.arraySize == 2 && IsLightGray(colors.GetArrayElementAtIndex(0).colorValue) &&
            IsLightGray(colors.GetArrayElementAtIndex(1).colorValue) &&
            colors.GetArrayElementAtIndex(0).colorValue.r > colors.GetArrayElementAtIndex(1).colorValue.r + 0.1f,
            "Attachment palette must contain off-white and light gray");
        Vector2 sizeRange = emitter.FindProperty("sizeRange").vector2Value;
        Check(sizeRange.x > 0f && sizeRange.y >= sizeRange.x, "Invalid metal particle size range");
        var palette = emitter.FindProperty("bodyColorWeights");
        Color[] bodyColors = ReadPalette(palette);
        Check(bodyColors.Distinct().Count() == 5 && bodyColors.Length == 5 &&
            bodyColors.Contains(new Color(0.26f, 0.28f, 0.31f)) && bodyColors.Contains(new Color(0.58f, 0.60f, 0.63f)) &&
            bodyColors.Contains(new Color(0.51f, 0.51f, 0.51f)) &&
            !bodyColors.Contains(new Color(0.34f, 0.43f, 0.52f)) &&
            bodyColors.Any(color => color.r > color.g && color.g > color.b),
            "Expected five body colors with neutral gray in place of blue-gray");
        var renderer = root.GetComponent<ParticleSystemRenderer>();
        var meshes = new Mesh[4];
        Check(root.GetComponentsInChildren<ParticleSystem>(true).Length == 2 && renderer.sharedMaterial != null &&
            renderer.GetMeshes(meshes) == 4 && meshes.Distinct().Count() == meshes.Length &&
            meshes.All(mesh => mesh != null && mesh.vertexCount >= 8 && Aspect(mesh.bounds.size) < 1.15f),
            "Expected four irregular polygons with similar width and height in one body renderer");
        var attachmentSystem = emitter.FindProperty("attachmentParticles").objectReferenceValue as ParticleSystem;
        var attachmentRenderer = attachmentSystem?.GetComponent<ParticleSystemRenderer>();
        var parts = new Mesh[3];
        Check(attachmentRenderer != null && attachmentRenderer.meshCount == parts.Length &&
            attachmentRenderer.GetMeshes(parts) == parts.Length && parts.All(mesh => mesh != null) &&
            parts.Select(mesh => mesh.name).SequenceEqual(new[] { "MetalPartBracket", "MetalPartPin", "MetalPartWasher" }),
            "Expected three separate attachment shapes: bracket, pin, washer");
        var burst = AssetDatabase.LoadAssetAtPath<GameObject>(BurstPath);
        Check(burst != null && PrefabUtility.GetPrefabAssetType(burst) == PrefabAssetType.Variant &&
            PrefabUtility.GetCorrespondingObjectFromSource(burst) == root, "Death burst must inherit HitMetalFragments");
        var burstEmitter = new SerializedObject(burst.GetComponent<EnemyMetalFragmentParticle>());
        Vector2Int hitCount = emitter.FindProperty("fragmentCount").vector2IntValue;
        Vector2Int burstCount = burstEmitter.FindProperty("fragmentCount").vector2IntValue;
        Check(burstCount.x > 0 && burstCount.y >= burstCount.x &&
            burstEmitter.FindProperty("speedRange").vector2Value.x > emitter.FindProperty("speedRange").vector2Value.y &&
            Mathf.Approximately(burstEmitter.FindProperty("spreadAngle").floatValue, 360f),
            "Expected a valid, faster, full-circle burst");
        foreach (string path in new[] { RobotPath, ArcherPath })
        {
            var enemy = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var module = new SerializedObject(enemy.GetComponentInChildren<EnemyParticleModule>(true));
            var entry = module.FindProperty("hitEffects").GetArrayElementAtIndex(0);
            Check(entry.FindPropertyRelative("prefab").objectReferenceValue == root.GetComponent<ParticleSystem>(),
                "Metal hit effect is not connected: " + path);
            Check(module.FindProperty("deathBurstPrefab").objectReferenceValue == burst.GetComponent<ParticleSystem>(),
                "Metal death burst is not connected: " + path);
        }
        return new { passed = true, meshes = meshes.Select(mesh => mesh.name).ToArray(),
            bodyColors = bodyColors.Select(ColorUtility.ToHtmlStringRGB).ToArray(), attachmentChance = 0.25f,
            attachmentShapes = parts.Select(mesh => mesh.name).ToArray(), connectedEnemies = 2,
            hitCount = new[] { hitCount.x, hitCount.y }, burstCount = new[] { burstCount.x, burstCount.y } };
    }

    private static bool IsLightGray(Color color) =>
        color.r >= 0.68f && Mathf.Max(color.r, color.g, color.b) - Mathf.Min(color.r, color.g, color.b) < 0.025f;

    private static float Aspect(Vector3 size) => Mathf.Max(size.x, size.y) / Mathf.Min(size.x, size.y);

    private static Color[] ReadPalette(SerializedProperty palette) => Enumerable.Range(0, palette.arraySize)
        .Select(index => palette.GetArrayElementAtIndex(index).FindPropertyRelative("Color").colorValue).ToArray();

    private static GameObject Box(string name, Vector2 position, Vector2 size)
    {
        var box = new GameObject(name);
        box.layer = LayerMask.NameToLayer("Ground");
        box.transform.position = position;
        box.AddComponent<BoxCollider2D>().size = size;
        return box;
    }

    private static EnemyMetalFragmentParticle[] FindFragments() =>
        Object.FindObjectsByType<EnemyMetalFragmentParticle>(FindObjectsSortMode.None);

    private static async Task Until(Func<bool> condition, float timeout, string message)
    {
        float deadline = Time.realtimeSinceStartup + timeout;
        while (!condition() && Time.realtimeSinceStartup < deadline) await Awaitable.NextFrameAsync();
        Check(condition(), message);
    }

    public static async Task<object> Run()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        bool previousBackground = Application.runInBackground;
        float previousMaximumDelta = Time.maximumDeltaTime;
        GameObject floor = null, wall = null, robot = null, archer = null, effect = null;
        try
        {
            Application.runInBackground = true;
            Time.maximumDeltaTime = 0.033f;
            floor = Box("Metal Test Floor", new Vector2(0f, -0.5f), new Vector2(16f, 1f));
            wall = Box("Metal Test Wall", new Vector2(0.95f, 1.4f), new Vector2(0.2f, 2.8f));
            effect = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(MetalPath),
                new Vector3(0f, 0.72f), Quaternion.identity);
            var emitter = new SerializedObject(effect.GetComponent<EnemyMetalFragmentParticle>());
            var particles = effect.GetComponent<ParticleSystem>();
            var (shapesEmitted, colorsEmitted, favoredCount) = VerifyAppearanceSelection(effect, emitter, particles);
            emitter.FindProperty("fragmentCount").vector2IntValue = Vector2Int.one;
            emitter.FindProperty("speedRange").vector2Value = Vector2.one * 5f;
            emitter.FindProperty("spreadAngle").floatValue = 0f;
            emitter.FindProperty("upwardSpeed").floatValue = 0f;
            emitter.FindProperty("attachmentChance").floatValue = 1f;
            emitter.FindProperty("lifetimeRange").vector2Value = Vector2.one * 1.1f;
            emitter.ApplyModifiedPropertiesWithoutUndo();
            Physics2D.SyncTransforms();
            var attachmentSystem = (ParticleSystem)emitter.FindProperty("attachmentParticles").objectReferenceValue;
            Play(effect, particles);
            var records = new ParticleSystem.Particle[2];
            var attachments = new ParticleSystem.Particle[2];
            Check(particles.GetParticles(records) == 1 && attachmentSystem.GetParticles(attachments) == 1,
                "Expected a body fragment and an attached part in separate renderers");
            Check(Aspect(records[0].startSize3D) < 1.18f, "Body particle stretches its near-square mesh");
            Check(IsLightGray(attachments[0].startColor), "Attached part has an unexpected tint");
            Vector2 initialLocalOffset = LocalOffset(records[0], attachments[0]);
            Check(initialLocalOffset.magnitude > 0.01f, "Attached part is centered");
            Vector2 previous = records[0].position;
            bool wentRight = false, bouncedLeft = false, reachedFloor = false, bouncedUp = false;
            float start = Time.time;
            while (Time.time - start < 0.82f)
            {
                await Awaitable.NextFrameAsync();
                Check(particles.GetParticles(records) == 1 && attachmentSystem.GetParticles(attachments) == 1,
                    "Attachment disappeared before its parent");
                Check(Vector2.Distance(initialLocalOffset, LocalOffset(records[0], attachments[0])) < 0.002f,
                    "Attached part slipped while spinning");
                Vector2 current = records[0].position;
                Vector2 delta = current - previous;
                wentRight |= delta.x > 0.015f;
                bouncedLeft |= wentRight && delta.x < -0.015f;
                reachedFloor |= current.y < 0.17f;
                bouncedUp |= reachedFloor && delta.y > 0.015f;
                Check(current.x < 0.851f && current.y >= -0.005f, "Fragment passed through a wall or floor");
                previous = current;
            }
            Check(bouncedLeft && bouncedUp, "Fragment did not bounce from both wall and floor");
            await Until(() => effect == null, 2f, "Metal particle lifetime did not clean up");

            robot = SpawnEnemy(RobotPath, new Vector3(-1f, 0.04f));
            Hit(robot);
            var sprays = FindFragments();
            Check(sprays.Length == 1, "Enemy02 did not emit one metal spray on damage");
            archer = SpawnEnemy(ArcherPath, new Vector3(-3f, 0.04f));
            Hit(archer);
            sprays = FindFragments();
            Check(sprays.Length == 2 && sprays.All(spray => spray.GetComponent<ParticleSystem>().particleCount > 0),
                "Enemy02 and Enemy04 must each emit a metal spray");
            Check(Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Length == 0,
                "A regular machine hit also emitted blood");
            return new { passed = true, shapesEmitted, colorsEmitted, favoredCount,
                wallBounce = bouncedLeft, floorBounce = bouncedUp,
                separateGrayAttachmentRotatesWithFragment = true, robotHit = true, archerHit = true };
        }
        finally
        {
            foreach (var fragment in FindFragments()) Object.Destroy(fragment.gameObject);
            if (effect != null) Object.Destroy(effect);
            if (robot != null) Object.Destroy(robot);
            if (archer != null) Object.Destroy(archer);
            if (wall != null) Object.Destroy(wall);
            if (floor != null) Object.Destroy(floor);
            Application.runInBackground = previousBackground;
            Time.maximumDeltaTime = previousMaximumDelta;
        }
    }

    public static async Task<object> RunDeath()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        bool previousBackground = Application.runInBackground;
        GameObject robot = null, archer = null;
        try
        {
            Application.runInBackground = true;
            robot = SpawnEnemy(RobotPath, new Vector3(-3f, 3f));
            archer = SpawnEnemy(ArcherPath, new Vector3(3f, 3f));

            int deathEvents = 0;
            foreach (GameObject enemy in new[] { robot, archer })
            {
                var agent = enemy.GetComponent<EnemyBase>();
                agent.DeathStarted += () => deathEvents++;
                Hit(enemy, 99999f);
                Check(agent.IsDead, "Lethal hit did not kill " + enemy.name);
                Hit(enemy, 99999f);
            }

            var bursts = FindFragments().Where(effect => effect.name.StartsWith("DeathMetalBurst")).ToArray();
            Check(deathEvents == 2 && bursts.Length == 2 && bursts.All(effect => effect.transform.parent == null),
                "Each enemy should emit one independent metal burst during its death event");
            Check(Object.FindObjectsByType<EnemyBloodParticle>(FindObjectsSortMode.None).Length == 0,
                "A metal enemy emitted blood on death");

            var systems = bursts.Select(effect => effect.GetComponent<ParticleSystem>()).ToArray();
            var counts = systems.Select(system => system.particleCount).ToArray();
            var burstPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BurstPath).GetComponent<EnemyMetalFragmentParticle>();
            var range = new SerializedObject(burstPrefab).FindProperty("fragmentCount").vector2IntValue;
            Check(counts.All(count => count >= range.x && count <= range.y), "Unexpected death fragment count");
            float scatterAt = Time.fixedTime + 0.08f;
            await Until(() => Time.fixedTime >= scatterAt, 2f, "Death burst did not advance");
            var records = new ParticleSystem.Particle[64];
            foreach (ParticleSystem system in systems)
            {
                int count = system.GetParticles(records);
                Vector3 origin = system.transform.position;
                var offsets = records.Take(count).Select(particle => particle.position - origin).ToArray();
                Check(offsets.Any(offset => offset.x < -0.15f) && offsets.Any(offset => offset.x > 0.15f) &&
                    offsets.Any(offset => offset.y < -0.15f) && offsets.Any(offset => offset.y > 0.15f),
                    "Death burst did not scatter in all four directions");
            }
            await Until(() => bursts.All(burst => burst == null), 4f, "Metal death bursts did not clean up");
            return new { passed = true, deathEvents, burstCount = counts, allDirections = true, emittedBlood = false };
        }
        finally
        {
            if (robot != null) Object.Destroy(robot);
            if (archer != null) Object.Destroy(archer);
            foreach (var fragment in FindFragments()) Object.Destroy(fragment.gameObject);
            Application.runInBackground = previousBackground;
        }
    }

    private static (int shapes, int colors, int favoredCount) VerifyAppearanceSelection(
        GameObject effect, SerializedObject emitter, ParticleSystem particles)
    {
        const int sampleCount = 64;
        emitter.FindProperty("fragmentCount").vector2IntValue = Vector2Int.one * sampleCount;
        emitter.FindProperty("attachmentChance").floatValue = 0f;
        emitter.ApplyModifiedPropertiesWithoutUndo();
        var palette = emitter.FindProperty("bodyColorWeights");
        var expectedColors = ReadPalette(palette).Select(color => (Color32)color).ToArray();
        var records = new ParticleSystem.Particle[sampleCount];
        var randomState = UnityEngine.Random.state;
        try
        {
            UnityEngine.Random.InitState(824);
            Play(effect, particles);
            Check(particles.GetParticles(records) == sampleCount, "Expected a full burst of body particles");
            int shapes = records.Select(particle => particle.GetMeshIndex(particles)).Distinct().Count();
            var colors = records.Select(particle => particle.startColor).Distinct().ToArray();
            Check(shapes == 4, "Some body shapes never appeared in a real particle burst");
            Check(colors.Length == expectedColors.Length && colors.All(color => expectedColors.Contains(color)),
                "A particle used a color outside the palette or a palette entry never appeared");

            for (int i = 0; i < palette.arraySize; i++)
                palette.GetArrayElementAtIndex(i).FindPropertyRelative("Weight").floatValue = i == 0 ? 4f : i == 1 ? 1f : 0f;
            emitter.ApplyModifiedPropertiesWithoutUndo();
            Play(effect, particles);
            Check(particles.GetParticles(records) == sampleCount, "Expected a full weighted burst");
            int favoredCount = records.Count(particle => particle.startColor.Equals(expectedColors[0]));
            int otherCount = records.Count(particle => particle.startColor.Equals(expectedColors[1]));
            Check(favoredCount + otherCount == sampleCount && favoredCount > otherCount && otherCount > 0,
                "The higher-weight color should appear more often, and zero-weight colors should be skipped");

            return (shapes, colors.Length, favoredCount);
        }
        finally { UnityEngine.Random.state = randomState; }
    }

    private static void Play(GameObject effect, ParticleSystem root) =>
        typeof(EnemyHitParticle).GetMethod("Play", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(effect.GetComponent<EnemyHitParticle>(), new object[] { root, 3f });

    private static Vector2 LocalOffset(ParticleSystem.Particle body, ParticleSystem.Particle attachment)
    {
        Vector3 offset = attachment.position - body.position;
        return Quaternion.Inverse(Quaternion.Euler(0f, 0f, body.rotation3D.z)) * offset;
    }

    private static GameObject SpawnEnemy(string path, Vector3 position)
    {
        var enemy = Object.Instantiate(AssetDatabase.LoadAssetAtPath<GameObject>(path), position, Quaternion.identity);
        foreach (var module in enemy.GetComponentsInChildren<MonoBehaviour>())
            if (module is EnemyHitCameraShakeModule or EnemyDamageIndicatorModule or EnemyDeathModule or EnemyCurrencyRewardModule)
                module.enabled = false;
        return enemy;
    }

    private static void Hit(GameObject root, float damage = 3f)
    {
        var enemy = root.GetComponent<EnemyBase>();
        enemy.ApplyDamage(new DamageData { DamageAmount = damage }, (Vector2)root.transform.position + Vector2.up * 0.75f,
            Vector2.right, Vector2.left);
    }

    public static async Task<object> Preview()
    {
        Check(Application.isPlaying && SceneManager.GetActiveScene().path == "", "Isolated Play Mode required");
        EditorApplication.isPaused = false;
        await Awaitable.NextFrameAsync();
        var camera = new GameObject("Metal Preview Camera").AddComponent<Camera>();
        camera.transform.position = new Vector3(0.45f, 0.95f, -10f);
        camera.orthographic = true;
        camera.orthographicSize = 1.6f;
        camera.backgroundColor = new Color(0.075f, 0.09f, 0.11f);
        camera.clearFlags = CameraClearFlags.SolidColor;
        var light = new GameObject("Preview Light").AddComponent<UnityEngine.Rendering.Universal.Light2D>();
        light.lightType = UnityEngine.Rendering.Universal.Light2D.LightType.Global;
        var floor = Box("Metal Preview Floor", new Vector2(0f, -0.25f), new Vector2(16f, 0.5f));
        var sprite = floor.AddComponent<SpriteRenderer>();
        sprite.sprite = Sprite.Create(Texture2D.whiteTexture, new Rect(0, 0, 1, 1), Vector2.one * 0.5f, 1f);
        sprite.color = new Color(0.18f, 0.22f, 0.25f);
        sprite.transform.localScale = new Vector3(16f, 0.5f, 1f);
        floor.GetComponent<BoxCollider2D>().size = Vector2.one;
        var archer = SpawnEnemy(ArcherPath, new Vector3(-1.25f, 0.03f));
        var patrol = archer.GetComponentInChildren<EnemyPatrolModule>();
        if (patrol != null) patrol.enabled = false;
        await Awaitable.NextFrameAsync();
        var randomState = UnityEngine.Random.state;
        UnityEngine.Random.InitState(1202);
        Hit(archer);
        UnityEngine.Random.state = randomState;
        float captureAt = Time.time + 0.22f;
        await Until(() => Time.time >= captureAt, 2f, "Preview did not advance");
        EditorApplication.isPaused = true;
        return new { displayed = FindFragments().Length };
    }
}
