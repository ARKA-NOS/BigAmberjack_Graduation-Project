namespace SDW.Scripts.Maps
{
    // 방과 방 사이의 연결 그래프(레이아웃)만 생성한다. 프리팹 선택이나 인스턴스화는 다루지 않는다.
    public interface IDungeonLayoutGenerator
    {
        DungeonLayout Generate(int minRoomCount, int maxRoomCount, float expandChance);
    }
}
