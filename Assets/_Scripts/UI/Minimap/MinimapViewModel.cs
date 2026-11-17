using System.Collections.Generic;
using _Scripts.Agent.Enemies;
using _Scripts.Interactable.Chest;
using _Scripts.Interactable.NPC;
using _Scripts.Maps;
using _Scripts.Maps.Data;
using _Scripts.Maps.Spawn;
using _Scripts.UI.MVVM.Observables;
using _Scripts.UI.MVVM.Ticking;
using _Scripts.UI.MVVM.ViewModels;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵 ViewModel. 방에 들어갈 때마다 그 방의 지형을 Room에 넣고, 매 프레임(Tick) 플레이어 위치와 아이콘 목록을 갱신한다.
    // 지형 텍스처는 방마다 처음 들어갈 때 한 번만 굽고 캐시한다.
    // 적은 웨이브로 나중에 생기고 상자/NPC는 입장 직후에 생기므로, 아이콘은 입장 시점에 복사하지 않고 Tick에서 방을 직접 읽는다.
    [CreateAssetMenu(fileName = "Minimap View Model", menuName = "UI/MVVM/Minimap View Model")]
    public class MinimapViewModel : ViewModelBase<IMinimapSource>, ITickableViewModel
    {
        [Tooltip("이 종류의 방에 들어가면 미니맵을 숨긴다. (미니 보스도 BossRoomDataSO를 쓰므로 함께 숨겨진다)")]
        [SerializeField] private List<RoomType> hiddenRoomTypes = new() { RoomType.Boss };

        [Header("Terrain")]
        [Tooltip("월드 1유닛(타일 1칸)을 텍스처 몇 픽셀로 칠할지.")]
        [SerializeField, Min(1)] private int pixelsPerUnit = 1;
        [SerializeField] private Color terrainColor = new(1f, 1f, 1f, 0.85f);

        private readonly Observable<MinimapData> _room = new();
        private readonly Observable<bool> _hasPlayer = new();
        private readonly Observable<Vector2> _playerPosition = new();
        private readonly ObservableList<MinimapMarker> _markers = new();

        private readonly Dictionary<RoomController, RoomCache> _roomCache = new();
        private readonly List<MinimapMarker> _markerBuffer = new();
        private IMinimapSource _source;
        private RoomCache _currentRoom;

        // 지금 방의 지형. null이면 미니맵을 숨긴다(숨길 방, 지형이 없는 방, 연결 전).
        public IReadOnlyObservable<MinimapData> Room => _room;
        public IReadOnlyObservable<bool> HasPlayer => _hasPlayer;
        // 플레이어의 월드 좌표.
        public IReadOnlyObservable<Vector2> PlayerPosition => _playerPosition;
        // 지금 방의 적/상자/NPC 아이콘(월드 좌표). 개수가 같으면 움직인 칸만 Replace로 알린다.
        public IReadOnlyObservableList<MinimapMarker> Markers => _markers;

        private sealed class RoomCache
        {
            public RoomController Room;
            public MinimapData Data;
            public InteractableSpawnPoint[] InteractablePoints;
        }

        protected override void OnConnect(IMinimapSource source)
        {
            _source = source;
            _source.RoomEntered += HandleRoomEntered;

            // 스테이지 생성기가 먼저 첫 방에 들어갔으면 이벤트를 놓쳤으므로 현재 방으로 바로 맞춘다.
            if (_source.CurrentRoom != null)
                HandleRoomEntered(_source.CurrentRoom);

            Tick();
        }

        protected override void OnDisconnect()
        {
            _source.RoomEntered -= HandleRoomEntered;
            _source = null;

            // 텍스처를 지우기 전에 화면이 먼저 놓도록 Room부터 비운다.
            _currentRoom = null;
            _room.Value = null;
            ReleaseRoomCache();
        }

        protected override void ResetState()
        {
            _currentRoom = null;
            _room.Value = null;
            _hasPlayer.Value = false;
            _playerPosition.Value = Vector2.zero;
            _markers.Clear();
        }

        public void Tick()
        {
            if (_source == null) return;

            Transform player = _source.Player;
            _hasPlayer.Value = player != null;
            if (player != null)
                _playerPosition.Value = player.position;

            CollectMarkers(_markerBuffer);
            SyncMarkers(_markerBuffer);
        }

        #region Room

        private void HandleRoomEntered(RoomController room)
        {
            if (room == null || IsHiddenRoom(room))
            {
                ShowRoom(null);
                return;
            }

            if (!_roomCache.TryGetValue(room, out RoomCache cache))
            {
                cache = CreateCache(room);

                // 지형이 없는 방은 캐시하지 않고 숨긴다(다음에 들어오면 다시 시도).
                if (cache == null)
                {
                    ShowRoom(null);
                    return;
                }

                _roomCache.Add(room, cache);
            }

            ShowRoom(cache);
        }

        private void ShowRoom(RoomCache cache)
        {
            _currentRoom = cache;
            _room.Value = cache?.Data;
        }

        private bool IsHiddenRoom(RoomController room)
            => room.Data != null && hiddenRoomTypes.Contains(room.Data.RoomType);

        private RoomCache CreateCache(RoomController room)
        {
            if (!TilemapBoundsUtility.TryGetWorldBounds(room.transform, out Bounds bounds))
            {
                Debug.LogWarning($"{room.name} : 타일맵이 없어 미니맵을 만들지 않았습니다.");
                return null;
            }

            Rect worldRect = new Rect(bounds.min, bounds.size);
            Texture2D terrain = MinimapTerrainBaker.Bake(room.transform, worldRect, pixelsPerUnit, terrainColor);

            return new RoomCache
            {
                Room = room,
                Data = new MinimapData(terrain, worldRect),
                InteractablePoints = room.GetComponentsInChildren<InteractableSpawnPoint>(true),
            };
        }

        private void ReleaseRoomCache()
        {
            foreach (RoomCache cache in _roomCache.Values)
            {
                if (cache.Data.Terrain != null)
                    Destroy(cache.Data.Terrain);
            }

            _roomCache.Clear();
        }

        #endregion

        #region Markers

        private void CollectMarkers(List<MinimapMarker> result)
        {
            result.Clear();

            if (_currentRoom == null || _currentRoom.Room == null)
                return;

            foreach (EnemyBase enemy in _currentRoom.Room.SpawnedEnemies)
            {
                if (RoomController.IsAlive(enemy))
                    result.Add(new MinimapMarker(enemy.transform.position, MinimapMarkerType.Enemy));
            }

            foreach (InteractableSpawnPoint point in _currentRoom.InteractablePoints)
            {
                if (point == null || point.Spawned == null)
                    continue;

                if (TryGetMarkerType(point.Spawned, out MinimapMarkerType type))
                    result.Add(new MinimapMarker(point.Spawned.transform.position, type));
            }
        }

        // 개수가 같으면 바뀐 칸만 Replace로 알리고, 개수가 달라지면 Reset 한 번으로 통째로 바꾼다.
        private void SyncMarkers(List<MinimapMarker> next)
        {
            if (next.Count != _markers.Count)
            {
                _markers.SetAll(next);
                return;
            }

            for (int i = 0; i < next.Count; i++)
            {
                if (!_markers[i].Equals(next[i]))
                    _markers[i] = next[i];
            }
        }

        private static bool TryGetMarkerType(Component interactable, out MinimapMarkerType type)
        {
            switch (interactable)
            {
                case Chest:
                    type = MinimapMarkerType.Chest;
                    return true;
                case Npc:
                    type = MinimapMarkerType.Npc;
                    return true;
                default:
                    type = default;
                    return false;
            }
        }

        #endregion
    }
}
