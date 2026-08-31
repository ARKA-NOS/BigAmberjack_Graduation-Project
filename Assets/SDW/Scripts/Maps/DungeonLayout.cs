using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // IDungeonLayoutGenerator가 만들어낸 방 그래프 결과물. GameObject 인스턴스화와는 무관하다.
    public class DungeonLayout
    {
        public readonly Dictionary<Vector2Int, RoomNode> Rooms;
        public readonly RoomNode StartRoom;

        public DungeonLayout(Dictionary<Vector2Int, RoomNode> rooms, RoomNode startRoom)
        {
            Rooms = rooms;
            StartRoom = startRoom;
        }
    }
}
