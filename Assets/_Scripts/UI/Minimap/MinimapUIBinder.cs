using System;
using System.Collections.Generic;
using Lrw.Script.UI.BindSystem;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.Minimap
{
    // 현재 방의 지형과 플레이어/적/상자/NPC 아이콘을 그린다.
    // 같은 ViewModel 에셋을 연결한 Binder 두 개를 두고, 하나는 FollowPlayer(오른쪽 아래 작은 맵), 하나는 FitRoom(전체 맵)으로 쓴다.
    //
    // 보여줄 데이터가 없으면(보스전 등) 배경까지 포함한 Visual Root 전체를 숨긴다.
    //
    // 계층 구조
    //   Minimap (이 컴포넌트)
    //   └ Frame (배경 Image) ← Visual Root
    //     └ Viewport (RectMask2D, 테두리 안쪽으로 들여서 배치)
    //       └ Content (앵커/피벗 중앙)
    //           ├ Terrain (RawImage, Content 전체로 늘림)
    //           └ Player Icon
    // 아이콘은 Content 아래에 생성되며 크기는 프리팹 그대로 두고 위치만 배율을 따른다.
    public class MinimapUIBinder : AbstractUIBinder<MinimapData>
    {
        private enum ViewMode
        {
            // 플레이어를 가운데에 두고 주변만 보여준다.
            FollowPlayer,
            // 방 전체가 Viewport 안에 들어오게 줄여서 보여준다.
            FitRoom
        }

        [Serializable]
        private class MarkerIcon
        {
            public MinimapMarkerType type;
            public RectTransform prefab;
        }

        [SerializeField] private ViewMode viewMode = ViewMode.FollowPlayer;

        [Header("Layout")]
        [Tooltip("보여줄 데이터가 없을 때 끄는 오브젝트. 배경까지 함께 숨기려면 배경 Image 오브젝트를 넣는다.")]
        [SerializeField] private GameObject visualRoot;
        [SerializeField] private RectTransform viewport;
        [SerializeField] private RectTransform content;
        [SerializeField] private RawImage terrainImage;
        [SerializeField] private RectTransform playerIcon;

        [Header("Scale")]
        [Tooltip("FollowPlayer 모드에서 월드 1유닛이 UI 몇 픽셀인지.")]
        [SerializeField, Min(0.1f)] private float followScale = 6f;
        [Tooltip("FitRoom 모드에서 방이 Viewport를 얼마나 채울지 (1이면 가장자리에 딱 맞음).")]
        [SerializeField, Range(0.1f, 1f)] private float fitFill = 0.9f;

        [Header("Markers")]
        [SerializeField] private List<MarkerIcon> markerIcons = new();

        private readonly List<MinimapMarker> _markers = new();
        private readonly Dictionary<MinimapMarkerType, List<RectTransform>> _iconPools = new();
        private readonly Dictionary<MinimapMarkerType, int> _usedIcons = new();
        private MinimapData _shownData;

        protected override void DataUpdate()
        {
            MinimapData data = CurrentValue;
            bool hasData = data != null && data.Terrain != null;

            if (visualRoot.activeSelf != hasData)
                visualRoot.SetActive(hasData);

            if (!hasData)
            {
                _shownData = null;
                return;
            }

            // 비활성 상태로 시작한 전체 맵은 Bind 순간(OnDataChanged)을 놓칠 수 있으므로 매 프레임 바뀌었는지 비교한다.
            if (_shownData != data)
            {
                _shownData = data;
                terrainImage.texture = data.Terrain;
            }

            Rect worldRect = data.WorldRect;
            float scale = GetScale(worldRect.size);
            bool hasPlayer = data.Player != null;
            Vector2 playerOffset = hasPlayer ? ToContentPosition(data.Player.position, worldRect, scale) : Vector2.zero;

            content.sizeDelta = worldRect.size * scale;
            content.anchoredPosition = viewMode == ViewMode.FollowPlayer
                ? ClampToRoom(-playerOffset, content.sizeDelta)
                : Vector2.zero;

            if (playerIcon != null)
            {
                playerIcon.gameObject.SetActive(hasPlayer);
                playerIcon.anchoredPosition = playerOffset;
                playerIcon.SetAsLastSibling();
            }

            UpdateMarkers(data, worldRect, scale);
        }

        private float GetScale(Vector2 roomSize)
        {
            if (viewMode == ViewMode.FollowPlayer || roomSize.x <= 0f || roomSize.y <= 0f)
                return followScale;

            Vector2 viewSize = viewport.rect.size;
            return Mathf.Min(viewSize.x / roomSize.x, viewSize.y / roomSize.y) * fitFill;
        }

        // 카메라 Confiner처럼 Viewport가 방 밖을 비추지 않게 Content 위치를 제한한다.
        // 방이 Viewport보다 작은 축은 움직일 여유가 없으므로 가운데에 고정한다. (플레이어 아이콘은 그대로 움직인다)
        private Vector2 ClampToRoom(Vector2 position, Vector2 contentSize)
        {
            Vector2 slack = Vector2.Max((contentSize - viewport.rect.size) * 0.5f, Vector2.zero);
            return new Vector2(
                Mathf.Clamp(position.x, -slack.x, slack.x),
                Mathf.Clamp(position.y, -slack.y, slack.y));
        }

        // Content의 피벗이 방 중심이므로, 방 중심에서 떨어진 거리에 배율을 곱하면 Content 안의 위치가 된다.
        private static Vector2 ToContentPosition(Vector2 worldPosition, Rect worldRect, float scale)
        {
            return (worldPosition - worldRect.center) * scale;
        }

        private void UpdateMarkers(MinimapData data, Rect worldRect, float scale)
        {
            data.CollectMarkers(_markers);
            _usedIcons.Clear();

            foreach (MinimapMarker marker in _markers)
            {
                RectTransform icon = GetIcon(marker.Type);

                if (icon != null)
                    icon.anchoredPosition = ToContentPosition(marker.Position, worldRect, scale);
            }

            // 이번 프레임에 쓰지 않은 아이콘은 다음에 다시 쓰도록 꺼둔다.
            foreach (KeyValuePair<MinimapMarkerType, List<RectTransform>> pool in _iconPools)
            {
                _usedIcons.TryGetValue(pool.Key, out int used);

                for (int i = 0; i < pool.Value.Count; i++)
                {
                    bool active = i < used;
                    if (pool.Value[i].gameObject.activeSelf != active)
                        pool.Value[i].gameObject.SetActive(active);
                }
            }
        }

        private RectTransform GetIcon(MinimapMarkerType type)
        {
            RectTransform prefab = FindPrefab(type);

            if (prefab == null)
                return null;

            if (!_iconPools.TryGetValue(type, out List<RectTransform> pool))
            {
                pool = new List<RectTransform>();
                _iconPools.Add(type, pool);
            }

            _usedIcons.TryGetValue(type, out int used);
            _usedIcons[type] = used + 1;

            if (used < pool.Count)
                return pool[used];

            RectTransform icon = Instantiate(prefab, content);
            pool.Add(icon);
            return icon;
        }

        private RectTransform FindPrefab(MinimapMarkerType type)
        {
            foreach (MarkerIcon markerIcon in markerIcons)
            {
                if (markerIcon.type == type)
                    return markerIcon.prefab;
            }

            return null;
        }
    }
}
