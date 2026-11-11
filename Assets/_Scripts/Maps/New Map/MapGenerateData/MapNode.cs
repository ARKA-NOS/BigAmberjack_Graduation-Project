using System.Collections.Generic;
using _Scripts.Maps.New_Map.MapSettingAsset;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    // 연결 그래프의 노드. 스레드에서 쓰므로 순수 C# 클래스로 둔다.
    public class MapNode
    {
        public string Id { get; }
        public RoomRole Role { get; }
        public int Parent { get; }                         // 그래프 노드 목록 안의 부모 인덱스, 시작방은 -1
        public List<int> Children { get; } = new List<int>();

        public MapNode(string id, RoomRole role, int parent)
        {
            Id = id;
            Role = role;
            Parent = parent;
        }
    }
}
