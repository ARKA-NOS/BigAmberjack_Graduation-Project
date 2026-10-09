using System;
using System.Collections.Generic;
using SDW.Scripts.NPC.Effects;
using UnityEngine;

namespace SDW.Scripts.NPC.Data
{
    // NPC 선택지 하나의 데이터.
    [Serializable]
    public class NpcChoiceData
    {
        [field: Tooltip("플레이어가 고를 수 있는 행동")]
        [field: SerializeField] public string ChoiceText { get; private set; }

        [field: Tooltip("선택 취소용 선택지. 고르면 취소 대사만 출력하고 아무것도 소모하지 않는다.")]
        [field: SerializeField] public bool IsCancel { get; private set; }

        [field: Tooltip("선택 시 실제로 적용할 효과들")]
        [field: SerializeField] public List<NpcChoiceEffectSO> Effects { get; private set; } = new();
    }
}
