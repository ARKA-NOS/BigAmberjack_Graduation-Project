using System.Collections.Generic;
using Agents.Players;
using SDW.Scripts.Maps.Data;
using SDW.Scripts.UI;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDW.Scripts.Maps
{
    // StageSO의 슬롯 순서대로 방을 왼쪽에서 오른쪽으로 일직선 배치하고,
    // 인접한 방끼리 좌/우 Portal로 연결한다.
    public class LinearStageGenerator : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private StageSO stage;
        [SerializeField] private Portal portalPrefab;

        [Header("Player")]
        [SerializeField] private PlayerController playerPrefab;

        [Header("UI")]
        [SerializeField] private ScreenFader screenFader;

        [Header("Layout")]
        [SerializeField] private float roomSpacing = 20f;

        [Header("Gizmos")]
        [SerializeField] private float gizmoRoomSize = 1f;

        private readonly List<RoomController> _rooms = new();

        private PlayerController _player;

        public IReadOnlyList<RoomController> Rooms => _rooms;

        private void Start()
        {
            GenerateStage();
        }

        [ContextMenu("Test")]
        private void GenerateStage()
        {
            ClearInstantiatedRooms();

            if (stage == null)
            {
                Debug.LogWarning("StageSO가 지정되지 않아 방을 생성하지 않았습니다.");
                return;
            }

            InstantiateRooms(stage.ResolveRooms());

            if (_rooms.Count == 0)
                return;

            CreatePortals();
            ActivateOnlyStartRoom();
            SpawnPlayer();
            EnterStartRoom();

#if UNITY_EDITOR
            if (!Application.isPlaying)
                SceneView.RepaintAll();
#endif
        }

        #region RoomsCreate

        private void InstantiateRooms(List<RoomDataSO> roomDatas)
        {
            foreach (RoomDataSO roomData in roomDatas)
            {
                if (roomData.RoomPrefab == null)
                {
                    Debug.LogWarning($"{roomData.name}에 Room Prefab이 지정되지 않아 건너뜁니다.");
                    continue;
                }

                int index = _rooms.Count;
                Vector3 worldPosition = transform.position + Vector3.right * (index * roomSpacing);
                RoomDefinition instance = Instantiate(roomData.RoomPrefab, worldPosition, Quaternion.identity, transform);

                if (!instance.TryGetComponent(out RoomController room))
                    room = instance.gameObject.AddComponent<RoomController>();

                room.Initialize(roomData, index);
                _rooms.Add(room);
            }
        }

        private void CreatePortals()
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("Portal Prefab이 지정되지 않아 Portal을 생성하지 않았습니다.");
                return;
            }

            for (int i = 0; i < _rooms.Count; i++)
            {
                if (i > 0)
                    CreatePortal(_rooms[i], RoomDirection.Left, _rooms[i - 1]);

                if (i < _rooms.Count - 1)
                    CreatePortal(_rooms[i], RoomDirection.Right, _rooms[i + 1]);
            }
        }

        private void CreatePortal(RoomController ownerRoom, RoomDirection direction, RoomController targetRoom)
        {
            Transform portalPoint = ownerRoom.Definition.GetPortalPoint(direction);

            if (portalPoint == null)
            {
                Debug.LogWarning($"{ownerRoom.name}에 {direction} 방향 Portal Point가 지정되지 않았습니다.");
                return;
            }

            Portal portal = Instantiate(portalPrefab, portalPoint.position, portalPoint.rotation, ownerRoom.transform);
            portal.Initialize(direction, ownerRoom, targetRoom, screenFader);
        }

        // 시작 방만 활성화하고 나머지는 비활성화 (실제 이동은 Portal 시스템에서 처리)
        private void ActivateOnlyStartRoom()
        {
            for (int i = 0; i < _rooms.Count; i++)
                _rooms[i].gameObject.SetActive(i == 0);
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

            Vector3 spawnPosition = GetSpawnPosition(_rooms[0]);

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

        // 시작 방은 Portal을 거치지 않고 곧바로 활성화되므로, 스테이지 생성 직후 별도로 입장 처리한다.
        private void EnterStartRoom()
        {
            if (!Application.isPlaying)
                return;

            _rooms[0].Enter();
        }

        private static Vector3 GetSpawnPosition(RoomController room)
        {
            Transform spawnPoint = room.Definition != null ? room.Definition.SpawnPoint : null;
            return spawnPoint != null ? spawnPoint.position : room.transform.position;
        }

        private void ClearInstantiatedRooms()
        {
            foreach (RoomController room in _rooms)
            {
                if (room == null)
                    continue;

                if (Application.isPlaying)
                    Destroy(room.gameObject);
                else
                    DestroyImmediate(room.gameObject);
            }

            _rooms.Clear();
        }

        #endregion

        #region Gizmos

        private void OnDrawGizmos()
        {
            for (int i = 0; i < _rooms.Count; i++)
            {
                RoomController room = _rooms[i];
                if (room == null)
                    continue;

                Vector3 roomWorldPos = room.transform.position;

                Gizmos.color = GetGizmoColor(room);
                Gizmos.DrawWireCube(roomWorldPos, Vector3.one * gizmoRoomSize);

                if (i < _rooms.Count - 1 && _rooms[i + 1] != null)
                {
                    Gizmos.color = Color.white;
                    Gizmos.DrawLine(roomWorldPos, _rooms[i + 1].transform.position);
                }
            }
        }

        private static Color GetGizmoColor(RoomController room)
        {
            if (room.Data == null)
                return Color.gray;

            switch (room.Data.RoomType)
            {
                case RoomType.Start:  return Color.green;
                case RoomType.Boss:   return Color.red;
                case RoomType.Reward: return Color.yellow;
                default:              return Color.cyan;
            }
        }

        #endregion
    }
}
