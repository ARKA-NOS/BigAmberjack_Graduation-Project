using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Player.Skills
{
    public class PlayerBulletSkill : AbstractCoolTimeSkill
    {
        [Header("Stat Data")]
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData coolDownSpeed;
        
        private Stat damageStat;
        private Stat coolDownSpeedStat;
        
        public override float CoolTime => BaseCoolTime / coolDownSpeedStat.Value;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[PlayerBulletSkill : {owner.name}] IStatModule is not found");

            damageStat = statModule.GetStat(physicalDamage);
            coolDownSpeedStat = statModule.GetStat(coolDownSpeed);
        }


        protected override void OnUseSkill()
        {
            base.OnUseSkill();
            FDebug.Log("[PlayerBulletSkill] OnUseSkill");
            EndSkill();
        }
    }
}