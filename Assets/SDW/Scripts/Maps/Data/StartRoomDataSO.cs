using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Start", menuName = "SDW/Map/Room/Start Room")]
    public class StartRoomDataSO : RoomDataSO
    {
        public override RoomType RoomType => RoomType.Start;

        public override void OnFirstEnter(RoomController room)
        {
        }
    }
}
