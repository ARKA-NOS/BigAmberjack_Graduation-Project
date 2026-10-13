using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.Interface;
using Lrw.Script.Agent.SkillSystem.NormalSkill;

namespace Lrw.Script.Agent.SkillSystem
{
    public class SkillModule : Module, ISkillModule,IAfterInitModule
    {
        private Dictionary<SkillSO, ISkill> _skillPlayers = new();
        
        public ModuleOwner Owner { get; private set; }
        
        private ISkill _currentUsingSkill;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            Owner = owner;
        }
        
        public void AfterInit()
        {
            _skillPlayers = GetSkillPlayers(GetComponentsInChildren<ISkill>());
        }

        private Dictionary<SkillSO, ISkill> GetSkillPlayers(ISkill[] skillPlayers)
        {
            Dictionary<SkillSO, ISkill> skillDict = new();

            foreach (ISkill skillPlayer in skillPlayers)
            {
                if (skillPlayer.SkillSo == null)
                {
                    FDebug.LogError($"[{skillPlayer}] Skill SO가 null 입니다.");
                    continue;
                }

                if (!skillDict.TryAdd(skillPlayer.SkillSo, skillPlayer))
                {
                    FDebug.LogError($"[{skillPlayer}] 같은 Skill SO가 있습니다.");
                    continue;
                }
                
                skillPlayer.InitSkill(_owner);
            }

            return skillDict;
        }
        
        public bool CanUseSkill(SkillSO skillSo)
        {
            if(skillSo == null) return false;
            if (!_skillPlayers.TryGetValue(skillSo, out ISkill skill)) return false;
            if (!skill.CanUseSkill()) return false;

            ISkill current = GetCurrentSkill();
            if (current == null) return true;

            // 같은 스킬은 끝나기 전까지 다시 쓸 수 없다
            if (current == skill) return false;

            return CanCancel(current, skill);
        }

        public void UseSkill(SkillSO skillSo, bool keyPressed)
        {
            if(skillSo == null) return;
            if (!_skillPlayers.TryGetValue(skillSo, out ISkill skill)) return;

            if (keyPressed)
            {
                if (!CanUseSkill(skillSo)) return;

                ISkill current = GetCurrentSkill();
                current?.StopSkill();

                _currentUsingSkill = skill;

                if (skill is IChargeSkill chargeSkill)
                    chargeSkill.ChargeStart();
                else
                    skill.UseSkill();
            }
            else
            {
                // 키를 뗀 건 차징 중인 현재 스킬에만 의미가 있다
                if (_currentUsingSkill != skill) return;
                if (skill is IChargeSkill { IsCharging: true } chargeSkill)
                    chargeSkill.ChargeEnd();
            }
        }

        /// <summary>
        /// 사용 중인 스킬을 반환한다. 이미 끝난 스킬이면 비우고 null을 반환한다.
        /// </summary>
        private ISkill GetCurrentSkill()
        {
            if (_currentUsingSkill == null) return null;

            bool running = _currentUsingSkill.IsUsing
                           || _currentUsingSkill is IChargeSkill { IsCharging: true };
            if (!running) _currentUsingSkill = null;

            return _currentUsingSkill;
        }

        /// <summary>
        /// next가 current를 끊고 들어갈 수 있는가?
        /// </summary>
        private bool CanCancel(ISkill current, ISkill next)
            => !current.SkillSo.Uninterruptible
               && (current.SkillSo.Cancelable || next.SkillSo.CanInterrupt);

        
    }
}