using _Scripts._Core;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    public struct RoomLinkData
    {
        public Vector2 Position;
        public DirEnum Direction;
        public int Size;

        public RoomLinkData(Vector2 position, DirEnum direction, int size)
        {
            Position = position;
            Direction = direction;
            Size = size;
        }
        
        public static bool CanLink(RoomLinkData link1, RoomLinkData link2)
            => link1.Direction.GetVector2() * link1.Size + link2.Direction.GetVector2() * link2.Size == Vector2.zero;
        
    }
}