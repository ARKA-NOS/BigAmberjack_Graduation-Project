namespace _Scripts.Agent.SkillSystem.Interface
{
    public interface IChargeSkill : ISkill
    {
        bool IsCharging { get; }
        void ChargeStart();
        void ChargeEnd();
        void ChargeCancel();
    }
}