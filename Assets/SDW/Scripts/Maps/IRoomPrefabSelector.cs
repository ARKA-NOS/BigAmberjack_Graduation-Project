namespace SDW.Scripts.Maps
{
    // 방이 실제로 연결된 방향(actualDirections)에 맞는 Room Prefab을 후보 목록에서 선택한다.
    public interface IRoomPrefabSelector
    {
        RoomDefinition Select(RoomDirection actualDirections);
    }
}
