using System.Collections.Generic;
using Lrw.Script.Agent.Players;
using SDW.Scripts.Maps.Data;
using SDW.Scripts.UI;
using UnityEngine;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players.CharacterSystem;
using Lrw.Script.Agent.Players.PlayerPackSystem;
using Lrw.Script.Agent.HealthSystem;
using SDW.Scripts.RunEnd;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace SDW.Scripts.Maps
{
    // StageSO가 고른 방 순서대로 방을 왼쪽에서 오른쪽으로 일직선 배치하고,
    // 각 방의 출구(Portal)를 다음 방으로만 연결한다. 이전 방으로는 돌아갈 수 없다.
    public class LinearStageGenerator : MonoBehaviour
    {
        [Header("Stage")]
        [SerializeField] private StageSO stage;
        [SerializeField] private Portal portalPrefab;

        [Header("UI")]
        [SerializeField] private ScreenFader screenFader;

        [Header("Layout")]
        [SerializeField] private float roomSpacing = 20f;

        [Header("Gizmos")]
        [SerializeField] private float gizmoRoomSize = 1f;

        private readonly List<RoomController> _rooms = new();

        private PlayerController _player;
        private IHealthModule _playerHealth;
        private RoomController _lastRoom;

        public IReadOnlyList<RoomController> Rooms => _rooms;

        private void Start()
        {
            GenerateStage();
        }

        private void OnDestroy()
        {
            if (_playerHealth != null)
                _playerHealth.OnDead -= HandlePlayerDead;

            if (_lastRoom != null)
                _lastRoom.Cleared -= HandleLastRoomCleared;
        }

        [ContextMenu("Test")]
        private void GenerateStage()
        {
            ClearInstantiatedRooms();

            if (_lastRoom != null)
            {
                _lastRoom.Cleared -= HandleLastRoomCleared;
                _lastRoom = null;
            }

            if (stage == null)
            {
                Debug.LogWarning("StageSO가 지정되지 않아 방을 생성하지 않았습니다.");
                return;
            }

            InstantiateRooms(stage.ResolveRooms());

            if (_rooms.Count == 0)
                return;

            CreatePortals();
            WatchLastRoomCleared();
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

        // 마지막 방을 뺀 모든 방의 Exit Point에 다음 방으로 가는 출구를 하나씩 만든다. (되돌아가는 출구는 없다)
        private void CreatePortals()
        {
            if (portalPrefab == null)
            {
                Debug.LogWarning("Portal Prefab이 지정되지 않아 Portal을 생성하지 않았습니다.");
                return;
            }

            for (int i = 0; i < _rooms.Count - 1; i++)
                CreatePortal(_rooms[i], _rooms[i + 1]);
        }

        private void CreatePortal(RoomController ownerRoom, RoomController targetRoom)
        {
            Transform exitPoint = ownerRoom.Definition.ExitPoint;

            if (exitPoint == null)
            {
                Debug.LogWarning($"{ownerRoom.name}에 Exit Point가 지정되지 않아 다음 방으로 갈 수 없습니다.");
                return;
            }

            Portal portal = Instantiate(portalPrefab, exitPoint.position, exitPoint.rotation, ownerRoom.transform);
            portal.Initialize(ownerRoom, targetRoom, screenFader);
        }

        // 스테이지 마지막 방(최종 보스)을 클리어하면 탐험이 끝난다.
        // 미니 보스도 같은 BossRoomDataSO를 쓰므로, 최종 여부는 SO가 아니라 순서로 판단한다.
        private void WatchLastRoomCleared()
        {
            _lastRoom = _rooms[_rooms.Count - 1];
            _lastRoom.Cleared += HandleLastRoomCleared;
        }

        private void HandleLastRoomCleared()
        {
            _lastRoom.Cleared -= HandleLastRoomCleared;
            RunEndEvents.RaiseRunEnded(RunEndReason.BossCleared);
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
        // 플레이어(PlayerPack)는 DontDestroyOnLoad라 이전 씬에서 넘어올 수 있으므로,
        // PlayerSpawner처럼 IPlayerPack이 없을 때만 새로 생성하고 있으면 위치만 옮긴다.
        private void SpawnPlayer()
        {
            if (!Application.isPlaying)
                return;

            Vector3 spawnPosition = _rooms[0].GetPlayerSpawnPosition();
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();

            if (!playerPack.IsExist())
                ServiceLocator.Get<ICharacterManager>().CreateCharacter(spawnPosition);
            else
                playerPack.SetPos(spawnPosition);

            if (_player == null)
            {
                _player = FindAnyObjectByType<PlayerController>();

                if (_player == null)
                {
                    Debug.LogWarning("플레이어 생성 후 PlayerController를 찾지 못했습니다.");
                    return;
                }

                WatchPlayerDeath();
            }

            _player.transform.position = spawnPosition;

            Rigidbody2D playerBody = _player.GetComponent<Rigidbody2D>();
            if (playerBody != null)
                playerBody.position = spawnPosition;
        }

        // 플레이어가 죽으면 탐험 종료를 알린다.
        // TODO: Lrw에 공용 플레이어 사망 이벤트가 생기면 그 이벤트로 교체한다.
        private void WatchPlayerDeath()
        {
            if (!_player.TryGetModule(out _playerHealth))
                return;

            _playerHealth.OnDead += HandlePlayerDead;
        }

        private void HandlePlayerDead()
        {
            RunEndEvents.RaiseRunEnded(RunEndReason.PlayerDead);
        }

        // 첫 방은 Portal을 거치지 않고 곧바로 활성화되므로, 스테이지 생성 직후 별도로 입장 처리한다.
        private void EnterStartRoom()
        {
            if (!Application.isPlaying)
                return;

            _rooms[0].Enter();
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
                case RoomType.Boss: return Color.red;
                default:            return Color.cyan;
            }
        }

        #endregion
    }
}
