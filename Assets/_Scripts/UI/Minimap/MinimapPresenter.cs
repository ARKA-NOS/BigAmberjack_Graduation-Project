using System.Collections.Generic;
using _Scripts.Agent.Players;
using _Scripts.Maps;
using _Scripts.Maps.Data;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 플레이어가 방에 들어올 때마다 그 방의 미니맵 데이터를 MinimapViewModel에 Bind한다.
    // 지형 텍스처는 방마다 처음 들어갈 때 한 번만 굽고 캐시한다.
    // 화면 표시는 씬의 MinimapUIBinder가 맡으므로 이 컴포넌트는 UI를 직접 참조하지 않는다.
    // 보스전처럼 미니맵을 숨길 방에서는 null을 Bind해서 Binder가 미니맵을 감추게 한다.
    public class MinimapPresenter : MonoBehaviour
    {
        [Tooltip("비워두면 씬에서 찾는다. (프리팹으로 배치할 때는 씬 참조를 걸 수 없으므로)")]
        [SerializeField] private LinearStageGenerator stageGenerator;
        [SerializeField] private MinimapViewModel viewModel;

        [Tooltip("이 종류의 방에 들어가면 미니맵을 숨긴다.")]
        [SerializeField] private List<RoomType> hiddenRoomTypes = new() { RoomType.Boss };

        [Header("Terrain")]
        [Tooltip("월드 1유닛(타일 1칸)을 텍스처 몇 픽셀로 칠할지.")]
        [SerializeField, Min(1)] private int pixelsPerUnit = 1;
        [SerializeField] private Color terrainColor = new(1f, 1f, 1f, 0.85f);

        private readonly Dictionary<RoomController, MinimapData> _roomData = new();
        private Transform _player;

        private void OnEnable()
        {
            if (stageGenerator == null)
                stageGenerator = FindAnyObjectByType<LinearStageGenerator>();

            if (stageGenerator == null)
            {
                Debug.LogWarning($"{name} : LinearStageGenerator가 지정되지 않아 미니맵을 갱신하지 않습니다.");
                return;
            }

            stageGenerator.RoomEntered += HandleRoomEntered;

            // 스테이지 생성기가 먼저 첫 방에 들어갔으면 이벤트를 놓쳤으므로 현재 방으로 바로 맞춘다.
            if (stageGenerator.CurrentRoom != null)
                HandleRoomEntered(stageGenerator.CurrentRoom);
        }

        private void OnDisable()
        {
            if (stageGenerator != null)
                stageGenerator.RoomEntered -= HandleRoomEntered;
        }

        private void OnDestroy()
        {
            foreach (MinimapData data in _roomData.Values)
            {
                if (data.Terrain != null)
                    Destroy(data.Terrain);
            }

            _roomData.Clear();

            // ViewModel 에셋은 씬이 바뀌어도 남으므로, 파괴된 방을 계속 가리키지 않게 비운다.
            if (viewModel != null)
                viewModel.Bind(null);
        }

        private void HandleRoomEntered(RoomController room)
        {
            if (IsHiddenRoom(room))
            {
                if (viewModel != null)
                    viewModel.Bind(null);

                return;
            }

            if (!_roomData.TryGetValue(room, out MinimapData data))
            {
                data = CreateData(room);

                if (data == null)
                    return;

                _roomData.Add(room, data);
            }

            if (viewModel != null)
                viewModel.Bind(data);
        }

        // 미니 보스도 BossRoomDataSO를 쓰므로 함께 숨겨진다.
        private bool IsHiddenRoom(RoomController room)
        {
            return room.Data != null && hiddenRoomTypes.Contains(room.Data.RoomType);
        }

        private MinimapData CreateData(RoomController room)
        {
            if (!TilemapBoundsUtility.TryGetWorldBounds(room.transform, out Bounds bounds))
            {
                Debug.LogWarning($"{room.name} : 타일맵이 없어 미니맵을 만들지 않았습니다.");
                return null;
            }

            Rect worldRect = new Rect(bounds.min, bounds.size);
            Texture2D terrain = MinimapTerrainBaker.Bake(room.transform, worldRect, pixelsPerUnit, terrainColor);

            return new MinimapData(room, terrain, worldRect, FindPlayer());
        }

        // 플레이어는 스테이지 생성기가 첫 방에 들어가기 전에 생성/배치하므로 이 시점에는 항상 있다.
        private Transform FindPlayer()
        {
            if (_player == null)
            {
                PlayerController player = FindAnyObjectByType<PlayerController>();
                _player = player != null ? player.transform : null;
            }

            return _player;
        }
    }
}
