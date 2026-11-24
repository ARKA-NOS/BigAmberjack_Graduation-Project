using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem.Titles
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

        // 버프 컴포넌트를 파괴하면 OnDestroy에서 이벤트 구독과 스탯 보정이 정리된다.
        public override void Remove(ModuleOwner owner)
        {
            foreach (MeleeHitStackBuff buff in owner.GetComponents<MeleeHitStackBuff>())
                Destroy(buff);
        }
    }
}
