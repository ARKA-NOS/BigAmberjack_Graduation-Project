namespace Lrw.Script.Agent.SkillSystem.ChargeSkill
{
    public interface IChargeSkill : ISkill
    {
        void ChargeStart();
        void ChargeEnd();
        void ChargeCancel();
    }
}