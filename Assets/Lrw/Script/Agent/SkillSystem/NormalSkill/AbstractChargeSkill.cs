using Lrw.Script.Agent.SkillSystem.Interface;

namespace Lrw.Script.Agent.SkillSystem.NormalSkill
{
    /// <summary>
    /// 쿨타임이 필요 없으면 baseCoolTime을 0으로 둔다.
    /// </summary>
    public abstract class AbstractChargeSkill : AbstractCoolTimeSkill, IChargeSkill
    {
        public bool IsCharging { get; private set; }

        public void ChargeStart()
        {
            if (IsCharging || IsUsing) return;
            IsCharging = true;
            OnChargeStart();
        }

        /// <summary>
        /// 차징을 끝내고 스킬을 발동한다.
        /// </summary>
        public void ChargeEnd()
        {
            if (!IsCharging) return;
            IsCharging = false;
            OnChargeEnd();
            UseSkill();
        }

        /// <summary>
        /// 발동하지 않고 차징만 취소한다. 쿨타임은 돌지 않는다.
        /// </summary>
        public void ChargeCancel()
        {
            if (!IsCharging) return;
            IsCharging = false;
            OnChargeCancel();
        }

        protected virtual void OnChargeStart() { }

        /// <summary>
        /// 발동(OnUseSkill) 직전에 호출된다. 차지량 계산 등에 쓴다.
        /// </summary>
        protected virtual void OnChargeEnd() { }

        protected virtual void OnChargeCancel() { }
    }
}
