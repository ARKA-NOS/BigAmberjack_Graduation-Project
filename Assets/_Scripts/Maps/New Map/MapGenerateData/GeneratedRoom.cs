using _Scripts.Maps.New_Map.MapSettingAsset;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    // 생성 완료 이벤트에 담는 방 하나. 적·보상 배치 시스템이 Role과 Instance의 생성 지점을 쓴다.
    public readonly struct GeneratedRoom
    {
        public Room Instance { get; }
        public RoomRole Role { get; }
        public string NodeId { get; }

        public GeneratedRoom(Room instance, RoomRole role, string nodeId)
        {
            Instance = instance;
            Role = role;
            NodeId = nodeId;
        }
    }
}
