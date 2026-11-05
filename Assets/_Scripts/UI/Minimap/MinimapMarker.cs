using UnityEngine;

namespace SDW.Scripts.Minimap
{
    // 이번 프레임에 미니맵에 찍을 아이콘 하나. 위치는 월드 좌표다.
    public readonly struct MinimapMarker
    {
        public Vector2 Position { get; }
        public MinimapMarkerType Type { get; }

        public MinimapMarker(Vector2 position, MinimapMarkerType type)
        {
            Position = position;
            Type = type;
        }
    }
}
