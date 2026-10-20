using System;
using System.Linq;
using CTJ.Enemies;
using CTJ.Enemies.Bosses;
using CTJ.Testing;
using CTJ.UI;
using Lrw.Script.Agent.HealthSystem;
using Lrw.Script.Agent.SkillSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    // 새 CTJ 보스 에셋만 생성. 기존 씬/잡몹 프리팹/공용 SO를 저장하지 않습니다.
    public static class BuildRobotEyeBoss
    {
        public const string Root = "Assets/CTJ/Prototypes/Bosses";
        public const string PrefabPath = Root + "/RobotEyeBoss.prefab";
        public const string ScenePath = Root + "/BossPatternTest.unity";

        public static object Build()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode || EditorApplication.isCompiling)
                throw new InvalidOperationException("Use Edit Mode after compilation.");
            if (AssetDatabase.LoadAssetAtPath<GameObject>(PrefabPath) != null)
                throw new InvalidOperationException("Boss prefab already exists; do not overwrite tuned assets.");
            if (!AssetDatabase.IsValidFolder(Root)) AssetDatabase.CreateFolder("Assets/CTJ/Prototypes", "Bosses");
            Sprite circle = AssetDatabase.GetBuiltinExtraResource<Sprite>("UI/Skin/Knob.psd");
            if (circle == null) throw new InvalidOperationException("Unity circle sprite unavailable.");
            Sprite square = AssetDatabase.LoadAssetAtPath<Sprite>("Assets/CTJ/Prototypes/Sprites/TestWhite.png");
            Material material = AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/PrototypeSpriteUnlit.mat");
            StatData hp = LoadGuid<StatData>("43956039511c547428d1fa4553333473");
            StatData delay = LoadGuid<StatData>("9d471adb7a7344f89ab2d30e31d80402");
            StatData gold = LoadGuid<StatData>("829d06c7a4914e7c96e6d3b72587ca04");
            StatData damage = ScriptableObject.CreateInstance<StatData>();
            Set(damage, "<StatName>k__BackingField", "Boss Attack Damage");
            Set(damage, "<DefaultValue>k__BackingField", 30f);
            AssetDatabase.CreateAsset(damage, Root + "/BossAttackDamage.asset");
            StatGroup group = ScriptableObject.CreateInstance<StatGroup>();
            SerializedObject groupSO = new SerializedObject(group);
            SerializedProperty entries = groupSO.FindProperty("<Stats>k__BackingField");
            entries.arraySize = 4;
            StatData[] keys = { hp, delay, damage, gold };
            float[] values = { 2500f, 0.6f, 30f, 0f };
            for (int i = 0; i < keys.Length; i++)
            {
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("<StatData>k__BackingField").objectReferenceValue = keys[i];
                entries.GetArrayElementAtIndex(i).FindPropertyRelative("<BaseValue>k__BackingField").floatValue = values[i];
            }
            groupSO.ApplyModifiedPropertiesWithoutUndo();
            AssetDatabase.CreateAsset(group, Root + "/RobotEye_Stats.asset");
            SkillSO radialKey = ScriptableObject.CreateInstance<SkillSO>();
            SkillSO laserKey = ScriptableObject.CreateInstance<SkillSO>();
            AssetDatabase.CreateAsset(radialKey, Root + "/RobotEye_Radial.asset");
            AssetDatabase.CreateAsset(laserKey, Root + "/RobotEye_Laser.asset");

            Scene previous = SceneManager.GetActiveScene();
            Scene buildScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            try
            {
                SceneManager.SetActiveScene(buildScene);
                GameObject warning = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/CTJ/Prototypes/Prefabs/AttackTelegraphCanvas.prefab"));
                warning.GetComponentInChildren<Image>().sprite = circle;
                GameObject radialWarning = PrefabUtility.SaveAsPrefabAsset(warning, Root + "/RadialWarningCanvas.prefab");
                Object.DestroyImmediate(warning);

                GameObject projectile = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(
                    "Assets/CTJ/Prototypes/Prefabs/MadGhostProjectile.prefab"));
                projectile.name = "RobotEyeProjectile";
                Set(projectile.GetComponent<EnemyProjectile>(), "speed", 5f);
                Set(projectile.GetComponent<EnemyProjectile>(), "lifetime", 5f);
                SpriteRenderer projectileSprite = projectile.GetComponentInChildren<SpriteRenderer>();
                projectileSprite.sprite = circle;
                projectileSprite.color = new Color(1f, 0.45f, 0.25f);
                projectile.transform.localScale = Vector3.one;
                projectileSprite.transform.localScale = Vector3.one * (0.3f / circle.bounds.size.x);
                projectile.GetComponent<CircleCollider2D>().radius = circle.bounds.size.x * 0.5f;
                GameObject projectileAsset = PrefabUtility.SaveAsPrefabAsset(projectile, Root + "/RobotEyeProjectile.prefab");
                Object.DestroyImmediate(projectile);

                GameObject root = new GameObject("RobotEyeBoss");
                root.layer = LayerMask.NameToLayer("Enemy");
                Rigidbody2D body = root.AddComponent<Rigidbody2D>();
                body.bodyType = RigidbodyType2D.Kinematic;
                body.gravityScale = 0f;
                body.constraints = RigidbodyConstraints2D.FreezeRotation;
                CircleCollider2D hurtbox = root.AddComponent<CircleCollider2D>();
                hurtbox.radius = 1f;
                hurtbox.isTrigger = true;
                RobotEyeBoss boss = root.AddComponent<RobotEyeBoss>();
                Set(boss, "deathDespawnDelay", 0.3f);
                Transform visual = Child(root.transform, "Visual").transform;
                SpriteRenderer shell = Sprite(visual, "Shell", circle, material, new Color(0.35f, 0.45f, 0.6f), 2.2f, 3);
                SpriteRenderer eye = Sprite(visual, "Eye", circle, material, new Color(1f, 0.28f, 0.18f), 1.35f, 4);
                SpriteRenderer pupil = Sprite(visual, "Pupil", circle, material, new Color(0.08f, 0.09f, 0.12f), 0.55f, 5);
                Transform firePoint = Child(visual, "FirePoint").transform;
                Child(root.transform, "FlightModule").AddComponent<BossFlightModule>();
                Child(root.transform, "ArenaModule").AddComponent<BossArenaModule>();
                StatModule stats = Child(root.transform, "StatModule").AddComponent<StatModule>();
                Set(stats, "baseStats", group);
                HealthModule health = Child(root.transform, "HealthModule").AddComponent<HealthModule>();
                Set(health, "maxHpStatData", hp);
                EnemySkillModule skills = Child(root.transform, "SkillModule").AddComponent<EnemySkillModule>();
                BossRadialVolleySkill radial = Child(skills.transform, "RadialVolley").AddComponent<BossRadialVolleySkill>();
                BossLaserSkill laser = Child(skills.transform, "MovingLaser").AddComponent<BossLaserSkill>();
                ConfigureSkill(radial, radialKey, delay, damage, firePoint, radialWarning.GetComponent<AttackTelegraphUI>());
                ConfigureSkill(laser, laserKey, delay, damage, firePoint, AssetDatabase.LoadAssetAtPath<AttackTelegraphUI>(
                    "Assets/CTJ/Prototypes/Prefabs/AttackTelegraphCanvas.prefab"));
                Set(radial, "projectilePrefab", projectileAsset.GetComponent<EnemyProjectile>());
                SpriteRenderer beam = Sprite(laser.transform, "BeamVisual", square, material, new Color(1f, 0.12f, 0.08f, 0.85f), 1f, 2);
                beam.enabled = false;
                Set(laser, "beamVisual", beam);
                Set(boss, "radialVolley", radial);
                Set(boss, "laser", laser);
                ConfigureSkillEntries(skills, radial, laser);
                EnemyHitReactionModule hit = Child(root.transform, "HitReactionModule").AddComponent<EnemyHitReactionModule>();
                Set(hit, "enableKnockback", false);
                Set(hit, "cancelAttackOnHit", false);
                Set(hit, "hitMaterial", AssetDatabase.LoadAssetAtPath<Material>("Assets/CTJ/Prototypes/Sprites/EnemySolidHitFlash.mat"));
                SerializedObject hitSO = new SerializedObject(hit);
                SerializedProperty sprites = hitSO.FindProperty("sprites");
                sprites.arraySize = 3;
                sprites.GetArrayElementAtIndex(0).objectReferenceValue = shell;
                sprites.GetArrayElementAtIndex(1).objectReferenceValue = eye;
                sprites.GetArrayElementAtIndex(2).objectReferenceValue = pupil;
                hitSO.ApplyModifiedPropertiesWithoutUndo();
                EnemyCurrencyRewardModule reward = Child(root.transform, "CurrencyRewardModule").AddComponent<EnemyCurrencyRewardModule>();
                Set(reward, "rewardCurrency", LoadGuid<Object>("8640762c579b29e4ea16f10701bef0d1"));
                Set(reward, "rewardAmountStat", gold);
                GameObject bossAsset = PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
                Object.DestroyImmediate(root);
                BuildScene(bossAsset, square, material);
                EditorSceneManager.SaveScene(buildScene, ScenePath);
            }
            finally
            {
                if (previous.IsValid() && previous.isLoaded) SceneManager.SetActiveScene(previous);
                EditorSceneManager.CloseScene(buildScene, true);
            }
            return new { prefab = PrefabPath, scene = ScenePath };
        }

        private static void ConfigureSkill(Object skill, SkillSO key, StatData delay, StatData damage, Transform origin, AttackTelegraphUI warning)
        {
            Set(skill, "<SkillSo>k__BackingField", key);
            Set(skill, "<BaseCoolTime>k__BackingField", 0f); // 연속 패턴 간격은 보스 FSM, 공용 쿨타임 검사는 여전히 유효.
            Set(skill, "attackDelayStat", delay);
            Set(skill, "postBlinkDelay", 0.15f);
            Set(skill, "telegraphPrefab", warning);
            Set(skill, "damageStat", damage);
            Set(skill, "firePoint", origin);
        }

        private static void ConfigureSkillEntries(EnemySkillModule module, params Object[] attacks)
        {
            SerializedObject so = new SerializedObject(module);
            SerializedProperty entries = so.FindProperty("skills");
            entries.arraySize = attacks.Length;
            for (int i = 0; i < attacks.Length; i++)
            {
                SerializedProperty entry = entries.GetArrayElementAtIndex(i);
                entry.FindPropertyRelative("skill").objectReferenceValue = attacks[i];
                entry.FindPropertyRelative("useSkill").boolValue = true;
                entry.FindPropertyRelative("useForAutomaticSelection").boolValue = false;
                entry.FindPropertyRelative("maxHeightDifference").floatValue = -1f;
                entry.FindPropertyRelative("requireGrounded").boolValue = false;
                entry.FindPropertyRelative("requireClearPath").boolValue = false;
            }
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void BuildScene(GameObject bossAsset, Sprite square, Material material)
        {
            Camera camera = new GameObject("Boss Test Camera").AddComponent<Camera>();
            camera.tag = "MainCamera";
            camera.orthographic = true;
            camera.orthographicSize = 7f;
            camera.transform.position = new Vector3(0f, 3.5f, -10f);
            camera.clearFlags = CameraClearFlags.SolidColor;
            camera.backgroundColor = new Color(0.08f, 0.1f, 0.15f);
            camera.gameObject.AddComponent<AudioListener>();
            SpriteRenderer ground = Sprite(null, "Ground", square, material, new Color(0.25f, 0.3f, 0.4f), 1f, 0);
            ground.transform.localScale = new Vector3(36f, 1f, 1f);
            ground.transform.position = new Vector3(0f, -0.5f, 0f);
            ground.gameObject.layer = LayerMask.NameToLayer("Ground");
            ground.gameObject.AddComponent<BoxCollider2D>();
            SpriteRenderer target = Sprite(null, "TestTarget (not PlayerPack)", square, material, Color.cyan, 1f, 3);
            target.transform.position = new Vector3(2f, 0.55f, 0f);
            target.transform.localScale = new Vector3(0.6f, 1f, 1f);
            target.gameObject.AddComponent<BoxCollider2D>();
            Rigidbody2D targetBody = target.gameObject.AddComponent<Rigidbody2D>();
            targetBody.gravityScale = 3f;
            targetBody.constraints = RigidbodyConstraints2D.FreezeRotation;
            EnemyDamageProbe probe = target.gameObject.AddComponent<EnemyDamageProbe>();
            GameObject instance = (GameObject)PrefabUtility.InstantiatePrefab(bossAsset);
            instance.transform.position = new Vector3(0f, 4f, 0f);
            RobotEyeBoss boss = instance.GetComponent<RobotEyeBoss>();
            Set(boss, "target", target.transform);
            BossTestControls controls = new GameObject("Boss Test Controls").AddComponent<BossTestControls>();
            Set(controls, "boss", boss);
            Set(controls, "probe", probe);
            Set(controls, "targetBody", targetBody);
        }

        private static SpriteRenderer Sprite(Transform parent, string name, Sprite sprite, Material material, Color color, float size, int order)
        {
            GameObject obj = Child(parent, name);
            SpriteRenderer renderer = obj.AddComponent<SpriteRenderer>();
            renderer.sprite = sprite;
            renderer.sharedMaterial = material;
            renderer.color = color;
            renderer.sortingOrder = order;
            obj.transform.localScale = Vector3.one * (size / sprite.bounds.size.x);
            return renderer;
        }

        private static GameObject Child(Transform parent, string name)
        {
            GameObject obj = new GameObject(name);
            obj.transform.SetParent(parent, false);
            return obj;
        }
        private static T LoadGuid<T>(string guid) where T : Object => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid));
        private static void Set(Object target, string field, object value)
        {
            SerializedObject serialized = new SerializedObject(target);
            SerializedProperty p = serialized.FindProperty(field);
            if (p == null) throw new InvalidOperationException(target.name + ": missing " + field);
            if (value is Object obj) p.objectReferenceValue = obj;
            else if (value is float number) p.floatValue = number;
            else if (value is bool flag) p.boolValue = flag;
            else if (value is string text) p.stringValue = text;
            else throw new InvalidOperationException("Unsupported value: " + field);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
