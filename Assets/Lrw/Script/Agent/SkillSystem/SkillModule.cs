using System.Collections.Generic;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem.Interface;

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
        
        public ISkill GetSkill(SkillSO skill)
            => skill != null ? _skillPlayers.GetValueOrDefault(skill) : null;
        
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

                ForceStopCurrentSkill();

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
        /// 현재 스킬을 캔슬 규칙과 상관없이 끊는다. 차징 중이면 차징만 취소한다.
        /// 피격, 사망, 가면 교체 등 외부에서도 이걸로 끊는다.
        /// </summary>
        public void ForceStopCurrentSkill()
        {
            ISkill current = GetCurrentSkill();
            if (current == null) return;

            if (current is IChargeSkill { IsCharging: true } chargingSkill)
                chargingSkill.ChargeCancel();
            else
                current.StopSkill();

            _currentUsingSkill = null;
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