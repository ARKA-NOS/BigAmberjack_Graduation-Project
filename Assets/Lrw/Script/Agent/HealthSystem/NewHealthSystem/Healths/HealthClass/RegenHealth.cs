using DevLib.ModuleSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthSO;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem.Healths.HealthClass
{
    /// <summary>
    /// 마지막 피해 후 RegenDelay초가 지나면 초당 RegenPerSecond만큼 다시 찬다.
    /// </summary>
    public class RegenHealth : AbstractHealth
    {
        private readonly RegenHealthSo _regenData;
        private float _timeSinceDamaged;

        public RegenHealth(RegenHealthSo data, ModuleOwner owner) : base(data, owner)
        {
            _regenData = data;
        }

        protected override void OnTick(float deltaTime)
        {
            _timeSinceDamaged += deltaTime;
            if (_timeSinceDamaged < _regenData.RegenDelay || Current >= Max) return;

            SetValue(Current + _regenData.RegenPerSecond * deltaTime);
        }

        protected override void OnDamaged(float taken, in HealthChangeData data)
            => _timeSinceDamaged = 0f;

        protected override void OnOwnerDamaged(in HealthChangeData data)
        {
            if (_regenData.ResetOnOwnerDamaged)
                _timeSinceDamaged = 0f;
        }
    }
}
