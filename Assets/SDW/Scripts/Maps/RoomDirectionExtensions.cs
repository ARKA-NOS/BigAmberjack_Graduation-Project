using UnityEngine;

namespace SDW.Scripts.Maps
{
    public static class RoomDirectionExtensions
    {
        public static readonly RoomDirection[] All =
        {
            RoomDirection.Up,
            RoomDirection.Down,
            RoomDirection.Left,
            RoomDirection.Right
        };

        private static readonly Vector2Int[] Offsets =
        {
            Vector2Int.up,
            Vector2Int.down,
            Vector2Int.left,
            Vector2Int.right
        };

        private static readonly RoomDirection[] Opposites =
        {
            RoomDirection.Down,
            RoomDirection.Up,
            RoomDirection.Right,
            RoomDirection.Left
        };

        // Up/Down/Left/Right 각각을 All/Offsets/Opposites와 같은 순서의 배열 인덱스로 변환한다.
        // RoomNode의 이웃 배열, RoomDefinition의 포탈 포인트 배열도 이 인덱스를 공유해서
        // 방향별 switch문이 여러 파일에 중복되지 않도록 한다.
        public static int Index(this RoomDirection direction)
        {
            switch (direction)
            {
                case RoomDirection.Up:    return 0;
                case RoomDirection.Down:  return 1;
                case RoomDirection.Left:  return 2;
                case RoomDirection.Right: return 3;
                default:                  return -1;
            }
        }

        public static Vector2Int ToVector2Int(this RoomDirection direction)
        {
            int index = direction.Index();
            return index >= 0 ? Offsets[index] : Vector2Int.zero;
        }

        public static RoomDirection Opposite(this RoomDirection direction)
        {
            int index = direction.Index();
            return index >= 0 ? Opposites[index] : RoomDirection.None;
        }
    }
}
