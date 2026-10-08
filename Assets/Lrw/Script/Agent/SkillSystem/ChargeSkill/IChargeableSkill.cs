namespace Lrw.Script.Agent.SkillSystem.ChargeSkill
{
    public interface IChargeableSkill
    {
        void ChargeStart();
        void ChargeEnd();
        void ChargeCancel();
    }
}