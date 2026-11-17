using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    // 스레드에서 만들고 메인 스레드가 읽는 방 하나의 배치 결과
    public struct RoomDeploymentData
    {
        public Room RoomPrefab;
        public Vector2 Position;
        public RoomRole Role;
        public string NodeId;          // "Start", "Combat1", "Shop", "RewardCombat1" 처럼 디버그용
        public int ParentIndex;        // 결과 목록 안의 부모 인덱스, 시작방은 -1
        public bool[] ConnectedLinks;  // Room.links와 같은 순서, 들어오는 링크와 자식에 쓴 링크가 true

        public RoomDeploymentData(Room roomPrefab, Vector2 position, RoomRole role, string nodeId, int parentIndex, bool[] connectedLinks)
        {
            RoomPrefab = roomPrefab;
            Position = position;
            Role = role;
            NodeId = nodeId;
            ParentIndex = parentIndex;
            ConnectedLinks = connectedLinks;
        }
    }
}
