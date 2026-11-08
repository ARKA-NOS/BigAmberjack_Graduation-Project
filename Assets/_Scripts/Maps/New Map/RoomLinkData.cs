using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public struct RoomLinkData
    {
        public Vector2 Position;
        public RoomDirection Direction;
        public int Size;

        public RoomLinkData(Vector2 position, RoomDirection direction, int size)
        {
            Position = position;
            Direction = direction;
            Size = size;
        }
        
    }
}