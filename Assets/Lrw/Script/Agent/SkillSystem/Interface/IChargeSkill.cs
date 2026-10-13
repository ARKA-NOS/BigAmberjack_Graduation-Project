namespace Lrw.Script.Agent.SkillSystem.Interface
{
    public interface IChargeSkill : ISkill
    {
        bool IsCharging { get; }
        void ChargeStart();
        void ChargeEnd();
        void ChargeCancel();
    }
}