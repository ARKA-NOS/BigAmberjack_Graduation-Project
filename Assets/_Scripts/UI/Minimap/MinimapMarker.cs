using System;
using UnityEngine;

namespace _Scripts.UI.Minimap
{
    // 미니맵에 찍을 아이콘 하나. 위치는 월드 좌표다.
    // ObservableList가 바뀐 칸만 알리도록 값 비교(IEquatable)를 구현한다.
    public readonly struct MinimapMarker : IEquatable<MinimapMarker>
    {
        public Vector2 Position { get; }
        public MinimapMarkerType Type { get; }

        public MinimapMarker(Vector2 position, MinimapMarkerType type)
        {
            Position = position;
            Type = type;
        }

        public bool Equals(MinimapMarker other) => Position == other.Position && Type == other.Type;

        public override bool Equals(object obj) => obj is MinimapMarker other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(Position, Type);
    }
}
