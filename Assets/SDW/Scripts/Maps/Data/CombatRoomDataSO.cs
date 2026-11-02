using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 넓은 탐험 맵. 적은 프리팹의 EncounterZone이, 상자/NPC는 InteractableSpawnPoint가 배치한다.
    // 출구 조건은 기본값(AlwaysClearedCondition)이라 적이 남아 있어도 출구는 항상 열려 있다.
    [CreateAssetMenu(fileName = "Room_Combat", menuName = "Map/Room/Combat Room")]
    public class CombatRoomDataSO : RoomDataSO
    {
        public override RoomType RoomType => RoomType.Combat;
    }
}
