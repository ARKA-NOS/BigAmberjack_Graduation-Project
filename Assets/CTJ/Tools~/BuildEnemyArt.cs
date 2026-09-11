// Unity Pipeline run_script로 실행하는 1회 구성 도구. Tools~는 Unity 자동 컴파일 대상이 아닙니다.
using System;
using System.IO;
using System.Linq;
using CTJ.Enemies;
using CTJ.Testing;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.SceneManagement;
using UnityEditor.U2D.Sprites;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class BuildEnemyArt
    {
        public const string Root = "Assets/CTJ/Prototypes";
        private const string Source = "Assets/CTJ/Assets/Horror Enemy Pack";

        public static string Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before building assets.");
            if (AssetDatabase.IsValidFolder(Root) || Directory.Exists(Root))
                throw new InvalidOperationException("Prototypes already exists. This builder does not overwrite tuned assets.");

            AssetDatabase.CreateFolder("Assets/CTJ", "Prototypes");
            foreach (string folder in new[] { "Sprites", "Animations", "Prefabs", "Scenes" })
                AssetDatabase.CreateFolder(Root, folder);

            Sprite[] cat = Slice(Source + "/Catto/Cat monster-Sheet Single Row.png", "Catto", 48, 32, 35);
            Sprite[] ghost = Slice(Source + "/Mad Ghost/Mad Ghost-Sheet Single Row.png", "MadGhost", 64, 64, 31);
            AnimationClip catIdle = Clip("Catto_Idle", cat, 14, 4, 6f, true);
            AnimationClip catMove = Clip("Catto_Move", cat, 3, 4, 10f, true);
            AnimationClip catAttack = Clip("Catto_Attack", cat, 18, 6, 10f, false);
            AnimationUtility.SetAnimationEvents(catAttack, new[]
            {
                new AnimationEvent { time = 0.2f, functionName = "OnAttackHit" },
                new AnimationEvent { time = 0.55f, functionName = "OnAttackFinished" }
            });
            EditorUtility.SetDirty(catAttack);
            AssetDatabase.SaveAssetIfDirty(catAttack);

            AnimatorController catController = Controller("Catto", catIdle, catMove, catAttack);
            AnimatorController ghostController = Controller("MadGhost",
                Clip("MadGhost_Idle", ghost, 0, 4, 6f, true),
                Clip("MadGhost_Move", ghost, 4, 4, 10f, true),
                Clip("MadGhost_Attack", ghost, 13, 3, 10f, false));

            Shader shader = Shader.Find("Universal Render Pipeline/2D/Sprite-Unlit-Default");
            if (shader == null) shader = Shader.Find("Sprites/Default");
            if (shader == null) throw new InvalidOperationException("No sprite shader is available.");
            Material material = new Material(shader) { name = "PrototypeSpriteUnlit" };
            AssetDatabase.CreateAsset(material, Root + "/Sprites/PrototypeSpriteUnlit.mat");
            Sprite square = CreateTestSquare();
            PhysicsMaterial2D frictionless = new PhysicsMaterial2D("EnemyFrictionless") { friction = 0f, bounciness = 0f };
            AssetDatabase.CreateAsset(frictionless, Root + "/Prefabs/EnemyFrictionless.physicsMaterial2D");

            Scene previous = SceneManager.GetActiveScene();
            Scene buildScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(buildScene);
                GameObject projectilePrefab = BuildProjectile(material);
                GameObject catRoot = CreateEnemy("Catto_Melee", cat[14], catController, material,
                    new Vector2(0.5f, 0.56f), frictionless, true);
                MeleeEnemy melee = catRoot.GetComponent<MeleeEnemy>();
                Transform visual = catRoot.transform.Find("Visual");
                BoxCollider2D hitbox = Child(visual, "AttackHitbox").AddComponent<BoxCollider2D>();
                hitbox.isTrigger = true;
                hitbox.offset = new Vector2(0.65f, 0.34f);
                hitbox.size = new Vector2(0.9f, 0.65f);
                Set(melee, "attackHitbox", hitbox);
                Set(melee, "attackAnimator", visual.GetComponent<Animator>());
                Set(melee, "detectionRange", 6f);
                Set(melee, "attackRange", 1.05f);
                Set(melee, "moveSpeed", 2f);
                Set(melee, "attackInterval", 1f);
                Set(melee, "attackTimeout", 1.5f);
                EnemyAnimationEventRelay relay = visual.gameObject.AddComponent<EnemyAnimationEventRelay>();
                Set(relay, "enemy", melee);
                GameObject catPrefab = PrefabUtility.SaveAsPrefabAsset(catRoot, Root + "/Prefabs/Catto_Melee.prefab");
                Object.DestroyImmediate(catRoot);

                GameObject ghostRoot = CreateEnemy("MadGhost_Ranged", ghost[0], ghostController, material,
                    new Vector2(0.6f, 1.15f), frictionless, false);
                RangedEnemy ranged = ghostRoot.GetComponent<RangedEnemy>();
                Transform ghostVisual = ghostRoot.transform.Find("Visual");
                Transform muzzle = Child(ghostVisual, "FirePoint").transform;
                muzzle.localPosition = new Vector3(0.5f, 0.95f, 0f);
                Set(ranged, "firePoint", muzzle);
                Set(ranged, "attackAnimator", ghostVisual.GetComponent<Animator>());
                Set(ranged, "projectilePrefab", projectilePrefab.GetComponent<EnemyProjectile>());
                Set(ranged, "detectionRange", 10f);
                Set(ranged, "attackRange", 7f);
                Set(ranged, "moveSpeed", 1.5f);
                Set(ranged, "attackInterval", 1.2f);
                GameObject ghostPrefab = PrefabUtility.SaveAsPrefabAsset(ghostRoot, Root + "/Prefabs/MadGhost_Ranged.prefab");
                Object.DestroyImmediate(ghostRoot);

                BuildDemo(catPrefab, ghostPrefab, square, material);
                EditorSceneManager.SaveScene(buildScene, Root + "/Scenes/EnemyArtTest.unity");
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(buildScene, true);
            }
            return Validate();
        }

        private static Sprite[] Slice(string source, string name, int width, int height, int count)
        {
            string path = Root + "/Sprites/" + name + ".png";
            if (!AssetDatabase.CopyAsset(source, path)) throw new InvalidOperationException("Cannot copy " + source);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 32f;
            importer.filterMode = FilterMode.Point;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = 2048;
            importer.alphaIsTransparency = true;
            TextureImporterSettings settings = new TextureImporterSettings();
            importer.ReadTextureSettings(settings);
            settings.spriteMeshType = SpriteMeshType.FullRect;
            importer.SetTextureSettings(settings);
            SpriteDataProviderFactories factories = new SpriteDataProviderFactories();
            factories.Init();
            ISpriteEditorDataProvider provider = factories.GetSpriteEditorDataProviderFromObject(importer);
            provider.InitSpriteEditorDataProvider();
            SpriteRect[] rects = new SpriteRect[count];
            for (int i = 0; i < count; i++)
                rects[i] = new SpriteRect
                {
                    name = name + "_" + i.ToString("D3"),
                    rect = new Rect(i * width, 0, width, height),
                    alignment = SpriteAlignment.Custom,
                    pivot = new Vector2(0.5f, 0f),
                    spriteID = GUID.Generate()
                };
            provider.SetSpriteRects(rects);
            provider.GetDataProvider<ISpriteNameFileIdDataProvider>()
                .SetNameFileIdPairs(rects.Select(r => new SpriteNameFileIdPair(r.name, r.spriteID)));
            provider.Apply();
            importer.SaveAndReimport();
            Sprite[] sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>().OrderBy(s => s.name).ToArray();
            if (sprites.Length != count) throw new InvalidOperationException("Incorrect sprite count: " + path);
            return sprites;
        }

        private static AnimationClip Clip(string name, Sprite[] sprites, int start, int count, float fps, bool loop)
        {
            AnimationClip clip = new AnimationClip { name = name, frameRate = fps };
            ObjectReferenceKeyframe[] keys = new ObjectReferenceKeyframe[count + 1];
            for (int i = 0; i < count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = sprites[start + i] };
            keys[count] = new ObjectReferenceKeyframe { time = count / fps,
                value = sprites[loop ? start : start + count - 1] };
            AnimationUtility.SetObjectReferenceCurve(clip,
                EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), keys);
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            settings.stopTime = count / fps;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.CreateAsset(clip, Root + "/Animations/" + name + ".anim");
            return clip;
        }

        private static AnimatorController Controller(string name, AnimationClip idleClip, AnimationClip moveClip, AnimationClip attackClip)
        {
            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(Root + "/Animations/" + name + ".controller");
            controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle", new Vector3(250, 0));
            AnimatorState move = machine.AddState("Move", new Vector3(500, 0));
            AnimatorState attack = machine.AddState("Attack", new Vector3(250, 150));
            idle.motion = idleClip;
            move.motion = moveClip;
            attack.motion = attackClip;
            idle.writeDefaultValues = move.writeDefaultValues = attack.writeDefaultValues = false;
            machine.defaultState = idle;
            AnimatorStateTransition toMove = idle.AddTransition(move);
            toMove.hasExitTime = false;
            toMove.duration = 0f;
            toMove.AddCondition(AnimatorConditionMode.If, 0f, "IsMoving");
            AnimatorStateTransition toIdle = move.AddTransition(idle);
            toIdle.hasExitTime = false;
            toIdle.duration = 0f;
            toIdle.AddCondition(AnimatorConditionMode.IfNot, 0f, "IsMoving");
            AnimatorStateTransition recovery = attack.AddTransition(idle);
            recovery.hasExitTime = true;
            recovery.exitTime = 1f;
            recovery.duration = 0f;
            EditorUtility.SetDirty(controller);
            AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static GameObject CreateEnemy(string name, Sprite sprite, AnimatorController controller,
            Material material, Vector2 bodySize, PhysicsMaterial2D frictionless, bool melee)
        {
            GameObject root = new GameObject(name);
            Rigidbody2D body = root.AddComponent<Rigidbody2D>();
            body.constraints = RigidbodyConstraints2D.FreezeRotation;
            body.gravityScale = 3f;
            body.interpolation = RigidbodyInterpolation2D.Interpolate;
            BoxCollider2D collider = root.AddComponent<BoxCollider2D>();
            collider.size = bodySize;
            collider.offset = new Vector2(0f, bodySize.y * 0.5f);
            collider.sharedMaterial = frictionless;
            RequiredModules(root);
            EnemyBase enemy = melee ? (EnemyBase)root.AddComponent<MeleeEnemy>() : root.AddComponent<RangedEnemy>();
            GameObject visual = Child(root.transform, "Visual");
            SpriteRenderer renderer = visual.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.sortingOrder = 10;
            Animator animator = visual.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.cullingMode = AnimatorCullingMode.AlwaysAnimate;
            EnemyLocomotionAnimator locomotion = visual.AddComponent<EnemyLocomotionAnimator>();
            Set(locomotion, "enemy", enemy);
            Set(enemy, "facingRoot", visual.transform);
            return root;
        }

        // 공용 Agent가 필수로 요구하는 기존 모듈만 연결합니다. 적 체력/사망 로직은 확장하지 않습니다.
        private static void RequiredModules(GameObject root)
        {
            StatData maxHealth = AssetDatabase.LoadAssetAtPath<StatData>("Assets/Lrw/GameModule/Stats/MaxHealth.asset");
            if (maxHealth == null) throw new InvalidOperationException("Missing existing MaxHealth StatData.");
            if (root.GetComponent<StatModule>() == null) root.AddComponent<StatModule>();
            HealthModule health = root.GetComponent<HealthModule>();
            if (health == null) health = root.AddComponent<HealthModule>();
            Set(health, "maxHpStatData", maxHealth);
        }

        public static string ConnectRequiredModules()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Stop Play Mode before editing CTJ prefabs.");
            foreach (string name in new[] { "Catto_Melee", "MadGhost_Ranged" })
            {
                string path = Root + "/Prefabs/" + name + ".prefab";
                GameObject root = PrefabUtility.LoadPrefabContents(path);
                try
                {
                    RequiredModules(root);
                    PrefabUtility.SaveAsPrefabAsset(root, path);
                }
                finally { PrefabUtility.UnloadPrefabContents(root); }
            }
            return Validate();
        }

        private static GameObject BuildProjectile(Material material)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prefabs/Projectile.prefab");
            if (source == null) throw new InvalidOperationException("Missing existing CTJ Projectile prefab.");
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(source);
            PrefabUtility.UnpackPrefabInstance(instance, PrefabUnpackMode.Completely, InteractionMode.AutomatedAction);
            instance.name = "MadGhostProjectile";
            instance.GetComponent<SpriteRenderer>().sharedMaterial = material;
            instance.GetComponent<SpriteRenderer>().color = new Color(1f, 0.08f, 0.35f, 1f);
            instance.GetComponent<SpriteRenderer>().sortingOrder = 11;
            instance.GetComponent<Rigidbody2D>().bodyType = RigidbodyType2D.Kinematic;
            instance.GetComponent<Rigidbody2D>().gravityScale = 0f;
            instance.GetComponent<CircleCollider2D>().isTrigger = true;
            Set(instance.GetComponent<EnemyProjectile>(), "speed", 8f);
            Set(instance.GetComponent<EnemyProjectile>(), "lifetime", 4f);
            GameObject prefab = PrefabUtility.SaveAsPrefabAsset(instance, Root + "/Prefabs/MadGhostProjectile.prefab");
            Object.DestroyImmediate(instance);
            return prefab;
        }

        private static Sprite CreateTestSquare()
        {
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            texture.SetPixels(new[] { Color.white, Color.white, Color.white, Color.white });
            texture.Apply();
            string path = Root + "/Sprites/TestWhite.png";
            File.WriteAllBytes(path, texture.EncodeToPNG());
            Object.DestroyImmediate(texture);
            AssetDatabase.ImportAsset(path);
            TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 2f;
            importer.filterMode = FilterMode.Point;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.mipmapEnabled = false;
            importer.SaveAndReimport();
            return AssetDatabase.LoadAssetAtPath<Sprite>(path);
        }

        private static void BuildDemo(GameObject cat, GameObject ghost, Sprite square, Material material)
        {
            Camera camera = new GameObject("CTJ Prototype Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 3.4f;
            camera.transform.position = new Vector3(1.5f, 1.2f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.12f, 0.15f, 0.2f);
            camera.gameObject.AddComponent<AudioListener>();
            Block("Catto Ground", new Vector2(-2.5f, -0.15f), new Vector2(6f, 0.3f), new Color(0.35f, 0.4f, 0.45f), square, material, true);
            Block("Ghost Ground", new Vector2(5f, -0.15f), new Vector2(6f, 0.3f), new Color(0.35f, 0.4f, 0.45f), square, material, true);
            GameObject catTarget = Block("Catto Target - move this to dodge", new Vector2(-1.5f, 0.48f), new Vector2(0.5f, 0.95f), Color.cyan, square, material, false);
            GameObject ghostTarget = Block("Ghost Target - move this to dodge", new Vector2(6.7f, 0.48f), new Vector2(0.5f, 0.95f), new Color(1f, 0.75f, 0.2f), square, material, false);
            catTarget.AddComponent<EnemyDamageProbe>();
            ghostTarget.AddComponent<EnemyDamageProbe>();
            GameObject catInstance = (GameObject)PrefabUtility.InstantiatePrefab(cat);
            catInstance.transform.position = new Vector3(-3.5f, 0f, 0f);
            Set(catInstance.GetComponent<MeleeEnemy>(), "target", catTarget.transform);
            GameObject ghostInstance = (GameObject)PrefabUtility.InstantiatePrefab(ghost);
            ghostInstance.transform.position = new Vector3(3.5f, 0f, 0f);
            Set(ghostInstance.GetComponent<RangedEnemy>(), "target", ghostTarget.transform);
        }

        private static GameObject Block(string name, Vector2 position, Vector2 size, Color color, Sprite sprite, Material material, bool ground)
        {
            GameObject block = new GameObject(name);
            block.transform.position = position;
            block.transform.localScale = new Vector3(size.x, size.y, 1f);
            SpriteRenderer renderer = block.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            block.AddComponent<BoxCollider2D>();
            if (ground) block.layer = LayerMask.NameToLayer("Ground");
            return block;
        }

        private static GameObject Child(Transform parent, string name)
        {
            GameObject child = new GameObject(name);
            child.transform.SetParent(parent, false);
            return child;
        }

        private static void Set(Object target, string field, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty property = serialized.FindProperty(field);
            if (property == null) throw new InvalidOperationException(target.name + ": Missing field " + field);
            if (value is Object reference) property.objectReferenceValue = reference;
            else if (value is float number) property.floatValue = number;
            else throw new InvalidOperationException("Unsupported value for " + field);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        public static string Validate()
        {
            foreach (string name in new[] { "Catto_Melee", "MadGhost_Ranged", "MadGhostProjectile" })
            {
                GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(Root + "/Prefabs/" + name + ".prefab");
                if (prefab == null) throw new InvalidOperationException("Missing prefab " + name);
                foreach (Transform child in prefab.GetComponentsInChildren<Transform>(true))
                    if (GameObjectUtility.GetMonoBehavioursWithMissingScriptCount(child.gameObject) != 0)
                        throw new InvalidOperationException("Missing script on " + child.name);
                foreach (SpriteRenderer renderer in prefab.GetComponentsInChildren<SpriteRenderer>(true))
                    if (renderer.sprite == null || renderer.sharedMaterial == null)
                        throw new InvalidOperationException("Missing sprite/material on " + renderer.name);
                if (name != "MadGhostProjectile")
                {
                    HealthModule health = prefab.GetComponent<HealthModule>();
                    if (health == null || prefab.GetComponent<StatModule>() == null ||
                        new SerializedObject(health).FindProperty("maxHpStatData").objectReferenceValue == null)
                        throw new InvalidOperationException("Missing Agent module dependency on " + name);
                }
            }
            AnimationClip attack = AssetDatabase.LoadAssetAtPath<AnimationClip>(Root + "/Animations/Catto_Attack.anim");
            AnimationEvent[] events = AnimationUtility.GetAnimationEvents(attack);
            if (events.Length != 2 || events[0].functionName != "OnAttackHit" || events[1].functionName != "OnAttackFinished")
                throw new InvalidOperationException("Catto attack events are incorrect.");
            foreach (string name in new[] { "Catto", "MadGhost" })
            {
                AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(Root + "/Animations/" + name + ".controller");
                string[] states = controller.layers[0].stateMachine.states.Select(s => s.state.name).ToArray();
                if (!states.Contains("Idle") || !states.Contains("Move") || !states.Contains("Attack"))
                    throw new InvalidOperationException("Missing Animator states in " + name);
                foreach (Sprite sprite in AssetDatabase.LoadAllAssetsAtPath(Root + "/Sprites/" + name + ".png").OfType<Sprite>())
                    if (sprite.pixelsPerUnit != 32f || sprite.pivot.y != 0f)
                        throw new InvalidOperationException("Inconsistent sprite scale/pivot.");
            }
            return "Validated: 3 prefabs, 2 controllers, 6 clips; Catto Hit=0.2s/Finished=0.55s; PPU=32, bottom-center pivots. Test scene: " + Root + "/Scenes/EnemyArtTest.unity";
        }
    }
}
