using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Interact.Chest.Data
{
    [CreateAssetMenu(fileName = "Chest", menuName = "Chest/Chest Data", order = 0)]
    public class ChestDataSO : ScriptableObject
    {
        [field: Header("기본 정보")]
        [field: Tooltip("플레이어에게 표시되는 이름")]
        [field: SerializeField] public string ChestName { get; private set; }
        [field: Tooltip("데이터 식별용 고유 ID")]
        [field: SerializeField] public string ChestId { get; private set; }

        [field: Header("출현 조건")]
        [field: Tooltip("등장하는 챕터. 비워두면 전체 챕터")]
        [field: SerializeField] public List<int> AppearChapters { get; private set; } = new();
        [field: Tooltip("등장하는 방. 전용 RoomType이 생기면 교체예정.")]
        [field: SerializeField] public string AppearRoomName { get; private set; }
        [field: Tooltip("선행 조건: 해금, 클리어, 보유 조건 등")]
        [field: SerializeField] public List<string> Preconditions { get; private set; } = new();

        [field: Header("보상")]
        [field: Tooltip("상자를 열었을 때 적용할 효과들")]
        [field: SerializeField] public List<ChoiceEffectSO> Rewards { get; private set; } = new();

        [field: Header("외형")]
        [field: Tooltip("닫힌 상자 스프라이트. 비워두면 프리팹의 기본 스프라이트를 사용한다.")]
        [field: SerializeField] public Sprite ClosedSprite { get; private set; }
        [field: Tooltip("열린 상자 스프라이트. 비워두면 열어도 스프라이트를 바꾸지 않는다.")]
        [field: SerializeField] public Sprite OpenedSprite { get; private set; }
    }
}
