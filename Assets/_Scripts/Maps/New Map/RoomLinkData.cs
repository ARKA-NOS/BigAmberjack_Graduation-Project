using _Scripts._Core;
using UnityEngine;

namespace _Scripts.Maps.New_Map
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
        
    }
}