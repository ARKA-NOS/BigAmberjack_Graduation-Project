using DevLib.BattleSystem;
using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    /// <summary>
    /// 피해/회복 요청. 체력을 하나씩 통과하면서 Amount가 남은 양으로 줄어든다.
    /// </summary>
    public struct HealthChangeData
    {
        public float Amount;
        public bool IsCritical;
        public ModuleOwner Dealer;
        public HealSourceType HealSource; // 회복일 때만 사용
        public HealthTag IgnoreTags;      // 피해일 때 이 태그를 가진 체력은 건너뛴다

        public bool IsValid => Amount > 0f && !float.IsNaN(Amount) && !float.IsInfinity(Amount);

        public static HealthChangeData Damage(float amount, ModuleOwner dealer = null, bool isCritical = false,
            HealthTag ignoreTags = HealthTag.None)
            => new()
            {
                Amount = amount,
                Dealer = dealer,
                IsCritical = isCritical,
                IgnoreTags = ignoreTags,
            };

        public static HealthChangeData Heal(float amount, HealSourceType source = HealSourceType.Normal,
            ModuleOwner dealer = null)
            => new()
            {
                Amount = amount,
                HealSource = source,
                Dealer = dealer,
            };

        public static HealthChangeData FromDamage(DamageData damageData)
            => Damage(damageData.DamageAmount, damageData.Dealer, damageData.IsCritical);
    }
}
