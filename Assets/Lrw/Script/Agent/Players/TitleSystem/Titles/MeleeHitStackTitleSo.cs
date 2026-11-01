using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.TitleSystem.Titles
{
    // 기본 공격이 맞을 때마다 스탯이 중첩해서 오르고, 일정 시간 동안 안 맞히면 사라지는 칭호
    [CreateAssetMenu(fileName = "Melee Hit Stack Title SO", menuName = "Title/Titles/Melee Hit Stack Title SO")]
    public class MeleeHitStackTitleSo : AbstractTitleSo
    {
        [SerializeField] private StatData statData;
        [SerializeField] private float valuePerStack = 0.01f;
        [SerializeField] private ModifyMathType modifyType = ModifyMathType.Multiply;
        [SerializeField] private float duration = 10f;
        [SerializeField, Min(1)] private int maxStack = 20;

        public override void Upgrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;
            if (statData == null) return;

            MeleeHitStackBuff buff = owner.gameObject.AddComponent<MeleeHitStackBuff>();
            buff.Init(owner, statModule.GetStat(statData), valuePerStack, modifyType, duration, maxStack);
        }
    }
}
