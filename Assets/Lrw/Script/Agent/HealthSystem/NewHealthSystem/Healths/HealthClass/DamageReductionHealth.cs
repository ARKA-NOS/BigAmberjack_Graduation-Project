using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass
{
    /// <summary>
    /// 받는 피해에 DamageTakenRate를 곱해서 깎인다. 다 못 받은 피해는 원래 단위로 되돌려 다음 체력에 넘긴다.
    /// 예) Rate 0.5, 남은 체력 10, 피해 30 → 15 중 10을 받고 남은 5를 원본 10으로 넘김
    /// </summary>
    public class DamageReductionHealth : AbstractHealth
    {
        private readonly DamageReductionHealthSo _reductionData;

        public DamageReductionHealth(DamageReductionHealthSo data, ModuleOwner owner) : base(data, owner)
        {
            _reductionData = data;
        }

        protected override float ToLayerDamage(float rawDamage, in HealthChangeData data)
            => rawDamage * _reductionData.DamageTakenRate;

        // Rate가 0이면 남아 있는 동안 피해를 전부 막는다
        protected override float ToRawDamage(float layerDamage, in HealthChangeData data)
            => _reductionData.DamageTakenRate > 0f ? layerDamage / _reductionData.DamageTakenRate : 0f;
    }
}
