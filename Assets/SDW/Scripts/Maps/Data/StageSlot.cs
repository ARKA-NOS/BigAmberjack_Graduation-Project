using System;
using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps.Data
{
    // 스테이지의 한 칸. 후보가 1개면 고정 방, 여러 개면 그중 하나를 랜덤으로 고른다.
    [Serializable]
    public class StageSlot
    {
        [SerializeField] private List<RoomDataSO> candidates = new();

        public IReadOnlyList<RoomDataSO> Candidates => candidates;
    }
}
