using System;
using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerSkillModule : SkillModule
    {
        [SerializeField] private SkillSO meleeSkill;
        [SerializeField] private SkillSO skill1;
        [SerializeField] private SkillSO skill2;
        
        private PlayerController _playerController;
        private PlayerInputSo _input;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _playerController = owner as PlayerController;
            FDebug.Assert(_playerController != null,"Owner is not PlayerController");

            _input = _playerController.PlayerInput;
            FDebug.Assert(_input != null,"Input SO is null");
            
            _input.OnAttackKeyPressed += AttackKeyPressed;
            _input.OnSkill1KeyPressed += Skill1KeyPressed;
            _input.OnSkill2KeyPressed += Skill2KeyPressed;
        }

        private void OnDestroy()
        {
            _input.OnAttackKeyPressed -= AttackKeyPressed;
            _input.OnSkill1KeyPressed -= Skill1KeyPressed;
            _input.OnSkill2KeyPressed -= Skill2KeyPressed;
        }

        public void SetSkill(SkillSlot slot,SkillSO skill)
        {
            switch (slot)
            {
                case SkillSlot.Melee:
                    meleeSkill = skill;
                    break;
                case SkillSlot.First:
                    skill1 = skill;
                    break;
                case SkillSlot.Second:
                    skill2 = skill;
                    break;
            }
        }
        
        private void AttackKeyPressed()
            => TryUseSkill(meleeSkill);
        private void Skill2KeyPressed()
            => TryUseSkill(skill1);
        private void Skill1KeyPressed()
            => TryUseSkill(skill2);

        private void TryUseSkill(SkillSO skillSo)
        {
            if (CanUseSkill(skillSo))
            {
                UseSkill(skillSo);
            }
        }
        
        
    }
}