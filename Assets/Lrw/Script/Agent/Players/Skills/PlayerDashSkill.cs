using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.NormalSkill;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Skills
{
    public class PlayerDashSkill : AbstractSkill
    {
        [SerializeField] private StatData dashCountStatData;
        [SerializeField] private StatData dashTimeStatData;
        
        private int dashCount = 0;

        private IGroundChecker _groundChecker;
        
        private Stat _dashCountStat;
        private Stat _dashTimeStat;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            FDebug.Assert(owner.TryGetModule(out _groundChecker),
                "[PlayerDashSkill] IGroundChecker is not found");
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                "[PlayerDashSkill] IStatModule is not found");

            _dashCountStat = statModule.GetStat(dashCountStatData);
            _dashTimeStat = statModule.GetStat(dashTimeStatData);
        }

        public override bool CanUseSkill()
        {
            return dashCount < _dashCountStat.Value;
        }

        protected override void OnUseSkill()
        {
            
            
        }

        private void FixedUpdate()
        {
            if (!IsUsing && _groundChecker.IsGroundChecking())
            {
                dashCount = 0;
            }
        }
        
        
        
        
    }
}