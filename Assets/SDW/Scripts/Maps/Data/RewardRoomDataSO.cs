using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    [CreateAssetMenu(fileName = "Room_Reward", menuName = "SDW/Map/Room/Reward Room")]
    public class RewardRoomDataSO : RoomDataSO
    {
        public override RoomType RoomType => RoomType.Reward;

        // TODO: 보상 시스템이 생기면 보상 데이터를 필드로 추가하고 여기서 배치한다.
        public override void OnFirstEnter(RoomController room)
        {
        }
    }
}
