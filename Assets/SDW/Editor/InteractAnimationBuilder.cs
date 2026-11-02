using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine;

namespace SDW.Editor
{
    // 상자/NPC 애니메이션 클립과 컨트롤러를 만들고 프리팹에 연결하는 일회성 도구.
    // 메뉴: Tools/SDW/Build Interact Animations
    public static class InteractAnimationBuilder
    {
        private const string OutputDir = "Assets/SDW/Animation";
        private const string ChestSheet = "Assets/SDW/Asset/Animated Chests/Chests.png";
        private const string NpcSheet = "Assets/SDW/Asset/SHADOW Series - The Free Assets/SHADOW Series - The Free Assets/SHADOW Series - The Blind Huntress/Sprite Sheet/1. Idle 48 x 48.png";
        private const string NpcSpritePrefix = "1. Idle 48 x 48_";

        private const string ChestBasePrefab = "Assets/SDW/Prefabs/Chest/Chest_Base.prefab";
        private const string ChestItemPrefab = "Assets/SDW/Prefabs/Chest/Chest_Item.prefab";
        private const string ChestCoinPrefab = "Assets/SDW/Prefabs/Chest/Chest_Coin.prefab";
        private const string NpcBasePrefab = "Assets/SDW/Prefabs/NPC/Npc_Base.prefab";

        private const int NpcIdleFrameCount = 12;

        [MenuItem("Tools/SDW/Build Interact Animations")]
        public static void Build()
        {
            Directory.CreateDirectory(OutputDir);

            AnimatorController itemController = BuildChestController("Chest_Item", 0, 9);
            AnimatorController goldController = BuildChestController("Chest_Gold", 20, 29);

            AssignController(ChestBasePrefab, itemController);
            AssignController(ChestItemPrefab, itemController);
            AssignController(ChestCoinPrefab, goldController);

            BuildNpc();
        }

        [MenuItem("Tools/SDW/Build Npc Animation")]
        public static void BuildNpc()
        {
            Directory.CreateDirectory(OutputDir);
            AssignController(NpcBasePrefab, BuildNpcController());

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[InteractAnimationBuilder] 완료");
        }

        // 닫힘(첫 프레임 1장, 루프) → Open 트리거 → 열림(나머지 프레임, 1회 재생)
        private static AnimatorController BuildChestController(string name, int firstFrame, int lastFrame)
        {
            Dictionary<string, Sprite> sprites = LoadSprites(ChestSheet);

            AnimationClip closed = CreateClip($"{name}_Closed", new[] { sprites[$"Chests_{firstFrame}"] }, 1f, true);
            AnimationClip open = CreateClip($"{name}_Open", Range(sprites, "Chests_", firstFrame + 1, lastFrame), 12f, false);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{OutputDir}/{name}.controller");
            controller.AddParameter("Open", AnimatorControllerParameterType.Trigger);

            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState closedState = machine.AddState("Closed");
            closedState.motion = closed;
            AnimatorState openState = machine.AddState("Open");
            openState.motion = open;
            machine.defaultState = closedState;

            AnimatorStateTransition transition = closedState.AddTransition(openState);
            transition.hasExitTime = false;
            transition.duration = 0f;
            transition.AddCondition(AnimatorConditionMode.If, 0, "Open");

            return controller;
        }

        private static AnimatorController BuildNpcController()
        {
            Dictionary<string, Sprite> sprites = LoadSprites(NpcSheet);
            AnimationClip idle = CreateClip("Npc_BlindHuntress_Idle", Range(sprites, NpcSpritePrefix, 0, NpcIdleFrameCount - 1), 8f, true);

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath($"{OutputDir}/Npc_BlindHuntress.controller");
            AnimatorState state = controller.layers[0].stateMachine.AddState("Idle");
            state.motion = idle;
            controller.layers[0].stateMachine.defaultState = state;
            return controller;
        }

        private static AnimationClip CreateClip(string name, IReadOnlyList<Sprite> frames, float fps, bool loop)
        {
            var clip = new AnimationClip { frameRate = fps };

            var keys = new ObjectReferenceKeyframe[frames.Count];
            for (int i = 0; i < frames.Count; i++)
                keys[i] = new ObjectReferenceKeyframe { time = i / fps, value = frames[i] };

            var binding = new EditorCurveBinding
            {
                path = "",
                type = typeof(SpriteRenderer),
                propertyName = "m_Sprite"
            };
            AnimationUtility.SetObjectReferenceCurve(clip, binding, keys);

            AnimationClipSettings settings = AnimationUtility.GetAnimationClipSettings(clip);
            settings.loopTime = loop;
            AnimationUtility.SetAnimationClipSettings(clip, settings);

            AssetDatabase.CreateAsset(clip, $"{OutputDir}/{name}.anim");
            return clip;
        }

        private static List<Sprite> Range(Dictionary<string, Sprite> sprites, string prefix, int from, int to)
        {
            var list = new List<Sprite>();
            for (int i = from; i <= to; i++)
                list.Add(sprites[prefix + i]);
            return list;
        }

        private static Dictionary<string, Sprite> LoadSprites(string path)
        {
            var map = new Dictionary<string, Sprite>();
            foreach (Object asset in AssetDatabase.LoadAllAssetRepresentationsAtPath(path))
            {
                if (asset is Sprite sprite)
                    map[sprite.name] = sprite;
            }

            return map;
        }

        // 프리팹 루트에 Animator가 없으면 추가하고 컨트롤러를 연결한다.
        private static void AssignController(string prefabPath, RuntimeAnimatorController controller)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(prefabPath);
            try
            {
                if (!root.TryGetComponent(out Animator animator))
                    animator = root.AddComponent<Animator>();

                animator.runtimeAnimatorController = controller;
                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }
    }
}
