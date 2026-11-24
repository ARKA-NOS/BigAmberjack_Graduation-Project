using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.TitleSystem.Titles
{
    [CreateAssetMenu(fileName = "Multitask Title SO", menuName = "Title/Titles/Multitask Title SO")]
    public class MultitaskTitleSo : AbstractTitleSo
    {
        [SerializeField] private StatData damageStat;
        [SerializeField] private StatData dashRangeStat;
        [SerializeField] private float firstAttackBonus = 0.35f;
        [SerializeField] private float dashRangeBonus = 0.5f;
        [SerializeField] private float boostedInvincibleTime = 0.225f;
        [SerializeField, Min(1)] private int requiredHits = 2;
        public override void Upgrade(ModuleOwner owner)
        {
            if (!owner.TryGetModule(out IStatModule statModule)) return;
            if (damageStat == null || dashRangeStat == null) return;

            MultitaskBuff buff = owner.gameObject.AddComponent<MultitaskBuff>();
            buff.Init(owner, statModule.GetStat(damageStat), statModule.GetStat(dashRangeStat),
                firstAttackBonus, dashRangeBonus, boostedInvincibleTime, requiredHits);
        }

        // 버프 컴포넌트를 파괴하면 OnDestroy에서 이벤트 구독, 스탯 보정, 무적이 정리된다.
        public override void Remove(ModuleOwner owner)
        {
            foreach (MultitaskBuff buff in owner.GetComponents<MultitaskBuff>())
                Destroy(buff);
        }
    }
}
