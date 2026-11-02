using SDW.Scripts.Maps.Conditions;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 보스 방. 미니 보스와 최종 보스를 같이 관리한다.
    // 보스와 부하는 방 프리팹의 EncounterZone이 생성한다. (트리거를 밟으면 등장, 배리어로 전투 공간을 막는다)
    // 최종 보스인지는 이 SO가 아니라 스테이지의 마지막 방인지로 정한다.
    // (마지막 방이 아니면 출구가 생기고, 마지막 방이면 LinearStageGenerator가 클리어 시 탐험 종료를 알린다)
    [CreateAssetMenu(fileName = "Room_Boss", menuName = "Map/Room/Boss Room")]
    public class BossRoomDataSO : RoomDataSO
    {
        public override RoomType RoomType => RoomType.Boss;

        // 방 안의 전투 구역(보스전)이 모두 끝나야 출구가 열린다.
        protected override IRoomClearCondition CreateDefaultClearCondition(RoomController room)
        {
            return new ClearAllEncountersCondition(room);
        }
    }
}
