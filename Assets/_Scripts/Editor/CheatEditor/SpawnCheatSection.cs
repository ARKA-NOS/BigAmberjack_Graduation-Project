using System.Linq;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Players;
using _Scripts.Agent.Players.InteractSystem;
using _Scripts.Interactable.NPC;
using _Scripts.Maps;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class SpawnCheatSection : ICheatSection
    {
        private const string PrefabFolder = "Assets/_Prefabs";

        private static readonly string[] CategoryNames = { "적", "NPC", "오브젝트" };

        // CategoryNames와 같은 순서다. 프리팹 루트의 컴포넌트로 분류한다.
        private readonly GameObject[][] _prefabs;
        private readonly string[][] _prefabNames;
        private readonly int[] _selectedIndices = new int[CategoryNames.Length];

        private int _category;
        private Vector2 _offset;
        private int _count = 1;

        public SpawnCheatSection()
        {
            GameObject[] all = AssetDatabase.FindAssets("t:Prefab", new[] { PrefabFolder })
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<GameObject>)
                .Where(prefab => prefab != null)
                .OrderBy(prefab => prefab.name)
                .ToArray();

            _prefabs = new[]
            {
                all.Where(IsEnemy).ToArray(),
                all.Where(IsNpc).ToArray(),
                all.Where(IsInteractObject).ToArray()
            };
            _prefabNames = _prefabs
                .Select(prefabs => prefabs.Select(prefab => prefab.name).ToArray())
                .ToArray();
        }

        public string Title => "플레이어 위치에 생성";

        public void Draw()
        {
            PlayerController player = Object.FindFirstObjectByType<PlayerController>();
            if (player == null)
            {
                EditorGUILayout.LabelField("씬에 플레이어가 없습니다.");
                return;
            }

            _category = GUILayout.Toolbar(_category, CategoryNames);

            GameObject[] prefabs = _prefabs[_category];
            if (prefabs.Length == 0)
            {
                EditorGUILayout.LabelField($"{PrefabFolder}에 해당 프리팹이 없습니다.");
                return;
            }

            _selectedIndices[_category] =
                EditorGUILayout.Popup("프리팹", _selectedIndices[_category], _prefabNames[_category]);
            _offset = EditorGUILayout.Vector2Field("위치 보정", _offset);
            _count = EditorGUILayout.IntSlider("개수", _count, 1, 10);

            if (!GUILayout.Button("생성")) return;

            Vector3 position = player.transform.position + (Vector3)_offset;
            for (int i = 0; i < _count; i++)
                Spawn(prefabs[_selectedIndices[_category]], position);
        }

        public void Tick() { }

        public void Release() { }

        // 방이 있으면 방의 자식으로 만든다. 상호작용 잠금이 방을 찾고, 방을 벗어날 때 함께 정리된다.
        private static void Spawn(GameObject prefab, Vector3 position)
        {
            RoomController room = FindNearestRoom(position);

            // 방에서 생성한 적은 맵 생성과 같은 경로를 써서 방에 등록한다.
            if (room != null && prefab.TryGetComponent(out EnemyBase enemy))
            {
                EnemySpawnUtility.SpawnEnemy(room, enemy, position);
                return;
            }

            Object.Instantiate(prefab, position, Quaternion.identity, room != null ? room.transform : null);
        }

        private static RoomController FindNearestRoom(Vector3 position)
            => Object.FindObjectsByType<RoomController>(FindObjectsSortMode.None)
                .OrderBy(room => (room.transform.position - position).sqrMagnitude)
                .FirstOrDefault();

        private static bool IsEnemy(GameObject prefab)
            => prefab.TryGetComponent(out EnemyBase _);

        private static bool IsNpc(GameObject prefab)
            => prefab.TryGetComponent(out Npc _) || prefab.TryGetComponent(out TitleNpc _);

        private static bool IsInteractObject(GameObject prefab)
            => !IsNpc(prefab) && prefab.TryGetComponent(out AbstractInteractObject _);
    }
}
