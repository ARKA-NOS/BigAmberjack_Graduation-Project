using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // 시작 방에서부터 인접 방향으로 확률적으로 뻗어나가며 순환 없는(트리 형태) 던전 그래프를 생성한다.
    public class RandomWalkDungeonLayoutGenerator : IDungeonLayoutGenerator
    {
        public DungeonLayout Generate(int minRoomCount, int maxRoomCount, float expandChance)
        {
            Dictionary<Vector2Int, RoomNode> rooms = new();
            List<RoomNode> expandableRooms = new();

            // 매번 목표 방 개수를 min~max 사이에서 랜덤으로 뽑는다.
            // (expandChance만으로는 분기 계수가 1보다 커서 거의 항상 maxRoomCount까지 자라버림)
            int targetRoomCount = Random.Range(minRoomCount, maxRoomCount + 1);

            RoomNode startRoom = new RoomNode(Vector2Int.zero);
            rooms.Add(startRoom.GridPosition, startRoom);
            expandableRooms.Add(startRoom);

            while (expandableRooms.Count > 0 && rooms.Count < targetRoomCount)
            {
                RoomNode currentRoom = expandableRooms[Random.Range(0, expandableRooms.Count)];
                expandableRooms.Remove(currentRoom);

                TryExpandRoom(currentRoom, rooms, expandableRooms, minRoomCount, targetRoomCount, expandChance);
            }

            Debug.Log($"던전 생성 완료 - 목표 방 개수 : {targetRoomCount}, 실제 생성된 방 개수 : {rooms.Count}");

            return new DungeonLayout(rooms, startRoom);
        }

        private static void TryExpandRoom(RoomNode currentRoom, Dictionary<Vector2Int, RoomNode> rooms,
            List<RoomNode> expandableRooms, int minRoomCount, int targetRoomCount, float expandChance)
        {
            foreach (RoomDirection direction in GetShuffledDirections())
            {
                if (rooms.Count >= targetRoomCount)
                    return;

                // 이미 해당 방향으로 연결되어 있으면 스킵
                if (currentRoom.GetNeighbor(direction) != null)
                    continue;

                // 최소 방 수에 도달하기 전에는 강제로 확장, 이후에는 확률적으로 확장
                bool forceExpand = rooms.Count < minRoomCount;
                if (!forceExpand && Random.value > expandChance)
                    continue;

                Vector2Int newPosition = currentRoom.GridPosition + direction.ToVector2Int();

                // 이미 다른 경로로 방이 존재하는 위치라면 연결하지 않음 (순환 구조 방지)
                if (rooms.ContainsKey(newPosition))
                    continue;

                RoomNode newRoom = new RoomNode(newPosition);
                rooms.Add(newPosition, newRoom);
                expandableRooms.Add(newRoom);

                currentRoom.SetNeighbor(direction, newRoom);
                newRoom.SetNeighbor(direction.Opposite(), currentRoom);
            }
        }

        private static List<RoomDirection> GetShuffledDirections()
        {
            List<RoomDirection> directions = new(RoomDirectionExtensions.All);

            for (int i = directions.Count - 1; i > 0; i--)
            {
                int j = Random.Range(0, i + 1);
                (directions[i], directions[j]) = (directions[j], directions[i]);
            }

            return directions;
        }
    }
}
