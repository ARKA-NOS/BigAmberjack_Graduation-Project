using UnityEngine;

namespace SDW.Scripts.Maps
{
    public static class RoomDirectionExtensions
    {
        public static Vector2Int ToVector2Int(this RoomDirection direction)
        {
            return direction == RoomDirection.Left ? Vector2Int.left : Vector2Int.right;
        }

        public static RoomDirection Opposite(this RoomDirection direction)
        {
            return direction == RoomDirection.Left ? RoomDirection.Right : RoomDirection.Left;
        }
    }
}
