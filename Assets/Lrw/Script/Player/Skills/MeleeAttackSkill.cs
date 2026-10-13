using Agents;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Player.Skills
{
    public class MeleeAttackSkill : AbstractCoolTimeSkill
    {
        [SerializeField] private StatData physicalDamage;
        [SerializeField] private StatData attackSpeed;
        
        private IRenderer _renderer;
        
        private Stat damageStat;
        private Stat attackSpeedStat;

        public override float CoolTime => GetCoolTime();
        

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[MeleeAttackSkill : {gameObject.name}] StatModule is not found]");

            damageStat = statModule.GetStat(physicalDamage);
            attackSpeedStat = statModule.GetStat(attackSpeed);
            
        }


        private float GetCoolTime()
        {
            return CoolTime;
        }
        
    }
}