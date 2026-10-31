using Lrw.Script._Core._EventSystem;
using SDW.Scripts.Maps.Data;

namespace SDW.Scripts.Maps
{
    // 플레이어가 방에 처음 입장했음을 알리는 이벤트. 칭호처럼 현재 맵 기준으로 동작하는 시스템이 구독한다.
    // 같은 방이 다시 출현해도 같은 값으로 알 수 있도록 방 인스턴스가 아니라 RoomDataSO를 넘긴다.
    public class MapEnteredEvent : IEvent
    {
        public RoomDataSO Room { get; private set; }

        public MapEnteredEvent Init(RoomDataSO room)
        {
            Room = room;
            return this;
        }
    }

    public static class MapEvents
    {
        private static readonly MapEnteredEvent MapEntered = new();

        public static void RaiseMapEntered(RoomDataSO room)
            => EventBus<MapEnteredEvent>.Raise(MapEntered.Init(room));
    }
}
