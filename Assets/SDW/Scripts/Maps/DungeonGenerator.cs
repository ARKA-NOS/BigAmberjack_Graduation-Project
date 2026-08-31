using System.Collections.Generic;
using Agents.Players;
using SDW.Scripts.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDW.Scripts.Maps
{
    public class DungeonGenerator : MonoBehaviour
    {
        [Header("Dungeon Size")]
        [SerializeField] private int minRoomCount = 10;
        [SerializeField] private int maxRoomCount = 30;
        [SerializeField, Range(0f, 1f)] private float expandChance = 0.5f;

        [Header("Room Prefabs")]
        [SerializeField] private List<RoomDefinition> roomPrefabs = new();
        [SerializeField] private bool allowPrefabReuseWhenExhausted = false;
        [SerializeField] private Portal portalPrefab;

        [Header("Player")]
        [SerializeField] private PlayerController playerPrefab;

        [Header("UI")]
        [SerializeField] private ScreenFader screenFader;

        [Header("Layout")]
        [SerializeField] private float roomSpacing = 20f;

        [Header("Gizmos")]
        [SerializeField] private float gizmoRoomSize = 1f;

        private readonly Dictionary<RoomNode, GameObject> _roomInstances = new();

        private IDungeonLayoutGenerator _layoutGenerator;
        private DungeonLayout _layout;
        private PlayerController _player;

        private void Start()
        {
            GenerateDungeon();
        }

        [ContextMenu("Test")]
        private void GenerateDungeon()
        {
            ClearInstantiatedRooms();

            _layoutGenerator ??= new RandomWalkDungeonLayoutGenerator();
            _layout = _layoutGenerator.Generate(minRoomCount, maxRoomCount, expandChance);

            InstantiateRooms();
            SpawnPlayer();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                SceneView.RepaintAll();
#endif
        }

        #region RoomsCreate

                private void InstantiateRooms()
        {
            IRoomPrefabSelector prefabSelector = new RoomPrefabSelector(roomPrefabs, allowPrefabReuseWhenExhausted);

            foreach (RoomNode room in _layout.Rooms.Values)
            {
                RoomDirection actualDirections = room.GetConnectedDirections();
                RoomDefinition prefab = prefabSelector.Select(actualDirections);

                if (prefab == null)
                {
                    Debug.LogWarning($"{room.GridPosition} 방향({actualDirections})을 지원하는 미사용 Room Prefab이 없습니다.");
                    continue;
                }

                Vector3 worldPosition = transform.position + new Vector3(room.GridPosition.x, room.GridPosition.y, 0f) * roomSpacing;
                RoomDefinition instance = Instantiate(prefab, worldPosition, Quaternion.identity, transform);

                _roomInstances.Add(room, instance.gameObject);

                CreatePortals(room, instance, actualDirections);
            }

            // 현재는 시작 방만 활성화하고 나머지는 비활성화 (실제 이동은 Portal 시스템에서 처리)
            foreach (var pair in _roomInstances)
            {
                pair.Value.SetActive(pair.Key == _layout.StartRoom);
            }
        }

        private void CreatePortals(RoomNode room, RoomDefinition instance, RoomDirection actualDirections)
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("Portal Prefab이 지정되지 않아 Portal을 생성하지 않았습니다.");
                return;
            }

            foreach (RoomDirection direction in RoomDirectionExtensions.All)
            {
                if ((actualDirections & direction) == 0)
                    continue;

                RoomNode targetRoom = room.GetNeighbor(direction);
                Transform portalPoint = instance.GetPortalPoint(direction);

                if (portalPoint == null)
                {
                    Debug.LogWarning($"{instance.name}에 {direction} 방향 Portal Point가 지정되지 않았습니다.");
                    continue;
                }

                Portal portal = Instantiate(portalPrefab, portalPoint.position, portalPoint.rotation, instance.transform);
                portal.Initialize(direction, targetRoom, this, screenFader);
            }
        }

        // 맵(방+포탈)이 모두 생성된 뒤, 시작 방의 SpawnPoint에 플레이어를 생성/재배치한다.
        // 플레이어는 방 오브젝트의 자식이 아니라 별도로 존재해야 한다.
        // (Portal 이동 시 이전 방을 SetActive(false)하는데, 방의 자식이면 플레이어까지 함께 비활성화된다)
        private void SpawnPlayer()
        {
            if (!Application.isPlaying)
                return;

            if (playerPrefab == null)
            {
                Debug.LogWarning("Player Prefab이 지정되지 않아 플레이어를 생성하지 않았습니다.");
                return;
            }

            if (!_roomInstances.TryGetValue(_layout.StartRoom, out GameObject startRoomInstance))
            {
                Debug.LogWarning("시작 방 인스턴스를 찾을 수 없어 플레이어를 생성하지 않았습니다.");
                return;
            }

            Vector3 spawnPosition = GetSpawnPosition(startRoomInstance);

            if (_player == null)
            {
                _player = Instantiate(playerPrefab, spawnPosition, Quaternion.identity);
                return;
            }

            _player.transform.position = spawnPosition;

            Rigidbody2D playerBody = _player.GetComponent<Rigidbody2D>();
            if (playerBody != null)
                playerBody.position = spawnPosition;
        }

        private static Vector3 GetSpawnPosition(GameObject roomInstance)
        {
            RoomDefinition roomDefinition = roomInstance.GetComponent<RoomDefinition>();
            Transform spawnPoint = roomDefinition != null ? roomDefinition.SpawnPoint : null;

            return spawnPoint != null ? spawnPoint.position : roomInstance.transform.position;
        }

        // Portal이 이동 시점에 목표 방의 실제 인스턴스를 조회하기 위해 사용한다.
        public GameObject GetRoomInstance(RoomNode room)
        {
            return _roomInstances.TryGetValue(room, out GameObject instance) ? instance : null;
        }

        private void ClearInstantiatedRooms()
        {
            foreach (GameObject instance in _roomInstances.Values)
            {
                if (instance == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(instance);
                else
                    DestroyImmediate(instance);
            }

            _roomInstances.Clear();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            if (_layout == null || _layout.Rooms.Count == 0)
                return;

            foreach (RoomNode room in _layout.Rooms.Values)
            {
                Vector3 roomWorldPos = GridToWorld(room.GridPosition);

                Gizmos.color = room == _layout.StartRoom ? Color.green : Color.cyan;
                Gizmos.DrawWireCube(roomWorldPos, Vector3.one * gizmoRoomSize);

                Gizmos.color = Color.white;
                DrawConnectionGizmo(roomWorldPos, room.Up);
                DrawConnectionGizmo(roomWorldPos, room.Right);
            }
        }

        private void DrawConnectionGizmo(Vector3 fromWorldPos, RoomNode neighbor)
        {
            if (neighbor == null)
                return;

            Gizmos.DrawLine(fromWorldPos, GridToWorld(neighbor.GridPosition));
        }

        private Vector3 GridToWorld(Vector2Int gridPosition)
        {
            return transform.position + new Vector3(gridPosition.x, gridPosition.y, 0f) * roomSpacing;
        }

        #endregion
    }
}
