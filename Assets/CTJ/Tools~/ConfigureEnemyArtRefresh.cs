using System;
using System.Linq;
using CTJ.Enemies;
using CTJ.Enemies.Skills;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;
using Object = UnityEngine.Object;

namespace CTJ.Tools
{
    public static class ConfigureEnemyArtRefresh
    {
        private const string Animations = "Assets/CTJ/Prototypes/Animations/";
        private const string Zombie = "Assets/CTJ/Assets/Zombie.png";
        private const string Archer = "Assets/CTJ/Assets/ArcherHero/Final/";

        public static object Apply()
        {
            Check(!EditorApplication.isPlayingOrWillChangePlaymode, "Edit only");
            foreach (string path in new[] { Zombie, Archer + "Idle and running.png", Archer + "Normal Attack.png", Archer + "death.png" })
            {
                TextureImporter importer = (TextureImporter)AssetImporter.GetAtPath(path);
                importer.filterMode = FilterMode.Point;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.mipmapEnabled = false;
                importer.SaveAndReimport(); // 기존 Sprite ID/슬라이스/피벗은 보존합니다.
            }
            Configure("Enemy01_TwistedArm", true);
            Configure("Enemy04_MechanicArcher", false);
            string turretPath = "Assets/CTJ/Prototypes/Prefabs/Enemy02_ProjectileRobot.prefab";
            GameObject turret = PrefabUtility.LoadPrefabContents(turretPath);
            try
            {
                Set(turret.GetComponentInChildren<EnemyOutlineModule>(), "outerSilhouetteOnly", true);
                // 지면에 고정된 받침은 수납 대상이 아니므로 아래쪽 외곽선까지 표시합니다.
                turret.transform.Find("Visual/GroundRim").GetComponent<SpriteRenderer>().maskInteraction = SpriteMaskInteraction.None;
                PrefabUtility.SaveAsPrefabAsset(turret, turretPath);
            }
            finally { PrefabUtility.UnloadPrefabContents(turret); }
            return new { updated = new[] { "Enemy01 Zombie", "Enemy04 ArcherHero", "Enemy02 outer silhouette" } };
        }

        private static void Configure(string name, bool zombie)
        {
            string path = "Assets/CTJ/Prototypes/Prefabs/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                Animator animator = root.GetComponentInChildren<Animator>();
                SpriteRenderer renderer = animator.GetComponent<SpriteRenderer>();
                Sprite[] idleSheet = Sprites(zombie ? Zombie : Archer + "Idle and running.png");
                Sprite[] attackSheet = Sprites(zombie ? Zombie : Archer + "Normal Attack.png");
                Sprite[] deathSheet = Sprites(zombie ? Zombie : Archer + "death.png");
                // 물리/스킬 범위는 유지하고 비주얼만 몸체 높이에 맞춥니다.
                float scale = root.GetComponent<BoxCollider2D>().size.y / idleSheet[0].bounds.size.y;
                Func<Sprite, Vector2> anchor = zombie ? ZombieAnchor : ArcherAnchor;
                string prefix = zombie ? "Enemy01_Zombie" : "Enemy04_ArcherHero";
                AnimationClip idle = Clip(prefix + "_Idle", zombie ? idleSheet.Take(8).ToArray() : idleSheet.Take(2).ToArray(),
                    zombie ? 1f : 2f / 3f, true, scale, anchor);
                AnimationClip move = Clip(prefix + "_Move", zombie ? idleSheet.Skip(15).Take(8).ToArray() : idleSheet.Skip(2).Take(8).ToArray(),
                    2f / 3f, true, scale, anchor);
                AnimationClip preparation = Clip(prefix + "_AttackPreparation", zombie ? attackSheet.Skip(8).Take(3).ToArray() : attackSheet.Take(5).ToArray(),
                    0.8f, false, scale, anchor, zombie ? new[] { 0f, 0.15f, 0.35f } : new[] { 0f, 0.1f, 0.2f, 0.35f, 0.5f });
                AnimationClip attack = Clip(prefix + "_Attack", zombie ? attackSheet.Skip(11).Take(4).ToArray() : attackSheet.Skip(5).Take(6).ToArray(),
                    zombie ? 0.4166667f : 0.6f, false, scale, anchor, zombie ? new[] { 0f, 0.06f, 0.1f, 0.16f } : null);
                if (zombie)
                    AnimationUtility.SetAnimationEvents(attack, new[] {
                        new AnimationEvent { time = 0.05f, functionName = "OnAttackHit" },
                        new AnimationEvent { time = 0.4f, functionName = "OnAttackFinished" }
                    });
                else AnimationUtility.SetAnimationEvents(attack, Array.Empty<AnimationEvent>()); // 후딜 종료는 ProjectileSkill이 결정
                AssetDatabase.SaveAssetIfDirty(attack);
                Sprite[] deathFrames = zombie ? new[] { deathSheet[45] }.Concat(deathSheet.Skip(47).Take(6)).ToArray() : deathSheet.Skip(5).Take(10).ToArray();
                AnimationClip death = Clip(prefix + "_Death", deathFrames, zombie ? 0.7f : 10f / 12f, false, scale, anchor);
                animator.runtimeAnimatorController = Controller(prefix, idle, move, preparation, attack, death);
                renderer.sprite = idleSheet[0]; renderer.color = Color.white;
                renderer.transform.localScale = Vector3.one * scale;
                renderer.transform.localPosition = Position(idleSheet[0], scale, anchor);
                if (zombie)
                {
                    EnemyMeleeSkill skill = root.GetComponentInChildren<EnemyMeleeSkill>();
                    Set(skill, "blinkBeforeAttack", false); Set(skill, "preparationWarning", null);
                    EnemyAttackWarningModule warning = root.GetComponentInChildren<EnemyAttackWarningModule>();
                    if (warning != null) warning.enabled = false;
                }
                else
                {
                    EnemyProjectileSkill skill = root.GetComponentInChildren<EnemyProjectileSkill>();
                    Set(skill, "attackAnimator", animator);
                    Set(skill, "preparationStateName", "Base Layer.AttackPreparation");
                    Set(skill, "idleStateName", "Base Layer.Idle");
                    Transform firePoint = (Transform)new SerializedObject(skill).FindProperty("firePoint").objectReferenceValue;
                    firePoint.localPosition = new Vector3(0.7f, 0.95f, 0f);
                }
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally { PrefabUtility.UnloadPrefabContents(root); }
        }

        private static Sprite[] Sprites(string path) => AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
            .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1))).ToArray();

        // 자동 슬라이스의 크기가 제각각이므로 원본 시트 셀의 발 위치를 공통 기준으로 씁니다.
        private static Vector2 ZombieAnchor(Sprite sprite) => new Vector2(
            Mathf.Floor(sprite.rect.x / 32f) * 32f + 16f, Mathf.Max(0f, Mathf.Floor(sprite.rect.center.y / 32f) * 32f - 1f));
        private static Vector2 ArcherAnchor(Sprite sprite) => new Vector2(
            Mathf.Floor(sprite.rect.x / 64f) * 64f + 32f, Mathf.Floor(sprite.rect.center.y / 64f) * 64f + 10f);
        private static Vector3 Position(Sprite sprite, float scale, Func<Sprite, Vector2> anchor) =>
            (sprite.rect.position + sprite.pivot - anchor(sprite)) * (scale / sprite.pixelsPerUnit);

        private static AnimationClip Clip(string name, Sprite[] frames, float duration, bool loop, float scale,
            Func<Sprite, Vector2> anchor, float[] times = null)
        {
            string path = Animations + name + ".anim";
            AnimationClip clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null) { clip = new AnimationClip { name = name }; AssetDatabase.CreateAsset(clip, path); }
            clip.ClearCurves(); clip.frameRate = 60f;
            var sprites = new ObjectReferenceKeyframe[frames.Length + 1];
            var x = new Keyframe[frames.Length + 1]; var y = new Keyframe[frames.Length + 1];
            for (int i = 0; i <= frames.Length; i++)
            {
                int index = i == frames.Length ? (loop ? 0 : frames.Length - 1) : i;
                float time = i == frames.Length ? duration : times != null ? times[i] : duration * i / frames.Length;
                Vector3 position = Position(frames[index], scale, anchor);
                sprites[i] = new ObjectReferenceKeyframe { time = time, value = frames[index] };
                x[i] = new Keyframe(time, position.x, float.PositiveInfinity, float.PositiveInfinity);
                y[i] = new Keyframe(time, position.y, float.PositiveInfinity, float.PositiveInfinity);
            }
            AnimationUtility.SetObjectReferenceCurve(clip, EditorCurveBinding.PPtrCurve("", typeof(SpriteRenderer), "m_Sprite"), sprites);
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.x"), new AnimationCurve(x));
            AnimationUtility.SetEditorCurve(clip, EditorCurveBinding.FloatCurve("", typeof(Transform), "m_LocalPosition.y"), new AnimationCurve(y));
            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop; settings.stopTime = duration;
            AnimationUtility.SetAnimationClipSettings(clip, settings);
            AssetDatabase.SaveAssetIfDirty(clip);
            return clip;
        }

        private static AnimatorController Controller(string prefix, params AnimationClip[] clips)
        {
            string path = Animations + prefix + ".controller";
            AnimatorController controller = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            bool create = controller == null;
            if (create) controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            string[] names = { "Idle", "Move", "AttackPreparation", "Attack", "Death" };
            AnimatorState[] states = new AnimatorState[names.Length];
            for (int i = 0; i < names.Length; i++)
            {
                states[i] = machine.states.Select(s => s.state).FirstOrDefault(s => s.name == names[i]) ?? machine.AddState(names[i], new Vector3(200 + i % 2 * 230, i / 2 * 120));
                states[i].motion = clips[i]; states[i].writeDefaultValues = false;
            }
            if (create)
            {
                controller.AddParameter("IsMoving", AnimatorControllerParameterType.Bool);
                AnimatorStateTransition move = states[0].AddTransition(states[1]);
                move.hasExitTime = false; move.duration = 0; move.AddCondition(AnimatorConditionMode.If, 0, "IsMoving");
                AnimatorStateTransition idle = states[1].AddTransition(states[0]);
                idle.hasExitTime = false; idle.duration = 0; idle.AddCondition(AnimatorConditionMode.IfNot, 0, "IsMoving");
            }
            // 준비는 마지막 자세를 유지합니다. 공격/사망 종료를 Animator가 임의로 앞당기지 않습니다.
            machine.defaultState = states[0];
            EditorUtility.SetDirty(controller); AssetDatabase.SaveAssetIfDirty(controller);
            return controller;
        }

        private static void Set(Object target, string field, object value)
        {
            SerializedObject so = new SerializedObject(target); SerializedProperty p = so.FindProperty(field);
            Check(p != null, "Missing field: " + field);
            if (value is bool flag) p.boolValue = flag;
            else if (value is string text) p.stringValue = text;
            else p.objectReferenceValue = value as Object;
            so.ApplyModifiedPropertiesWithoutUndo();
        }
        private static void Check(bool value, string message) { if (!value) throw new InvalidOperationException(message); }
        public static object Inspect()
        {
            string[] sheets = { "Assets/CTJ/Assets/Zombie.png",
                "Assets/CTJ/Assets/ArcherHero/Final/Idle and running.png",
                "Assets/CTJ/Assets/ArcherHero/Final/Normal Attack.png",
                "Assets/CTJ/Assets/ArcherHero/Final/death.png" };
            return new {
                sheets = sheets.Select(path => new { path, sprites = AssetDatabase.LoadAllAssetsAtPath(path).OfType<Sprite>()
                    .OrderBy(s => int.Parse(s.name.Substring(s.name.LastIndexOf('_') + 1)))
                    .Select(s => new { s.name, rect = s.rect.ToString(), pivot = s.pivot.ToString(), ppu = s.pixelsPerUnit }).ToArray() }).ToArray(),
                enemies = new[] { "Enemy01_TwistedArm", "Enemy04_MechanicArcher", "Enemy02_ProjectileRobot" }.Select(name => {
                    GameObject root = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/CTJ/Prototypes/Prefabs/" + name + ".prefab");
                    return new { name, body = root.GetComponent<BoxCollider2D>().size.ToString(), scale = root.transform.localScale.ToString(),
                        renderers = root.GetComponentsInChildren<SpriteRenderer>(true).Select(s => new {
                            path = AnimationUtility.CalculateTransformPath(s.transform, root.transform), sprite = s.sprite.name,
                            position = s.transform.localPosition.ToString(), scale = s.transform.localScale.ToString(), order = s.sortingOrder,
                            localBounds = s.sprite.bounds.ToString() }).ToArray(),
                        animators = root.GetComponentsInChildren<Animator>().Select(a => new {
                            path = AnimationUtility.CalculateTransformPath(a.transform, root.transform), controller = AssetDatabase.GetAssetPath(a.runtimeAnimatorController) }).ToArray() };
                }).ToArray()
            };
        }
    }
}
