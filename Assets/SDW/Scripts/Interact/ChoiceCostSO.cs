using System.Collections.Generic;
using DevLib.ModuleSystem;
using UnityEngine;

namespace SDW.Scripts.Interact
{
    // 선택지를 고를 때 치러야 하는 비용(재화, HP, 아이템 등)의 부모 SO.
    // 비용 종류마다 이 클래스를 상속한 자식 SO를 만들고 NpcChoiceData.Costs에 넣는다.
    // 효과와 달리 하나라도 치를 수 없으면 선택지 전체가 막힌다.
    public abstract class ChoiceCostSO : ScriptableObject
    {
        public abstract bool CanPay(ModuleOwner owner);
        public abstract void Pay(ModuleOwner owner);

        public static bool CanPayAll(IEnumerable<ChoiceCostSO> costs, ModuleOwner owner)
        {
            foreach (ChoiceCostSO cost in costs)
            {
                if (cost != null && !cost.CanPay(owner))
                    return false;
            }

            return true;
        }

        public static void PayAll(IEnumerable<ChoiceCostSO> costs, ModuleOwner owner)
        {
            foreach (ChoiceCostSO cost in costs)
            {
                if (cost != null)
                    cost.Pay(owner);
            }
        }
    }
}
