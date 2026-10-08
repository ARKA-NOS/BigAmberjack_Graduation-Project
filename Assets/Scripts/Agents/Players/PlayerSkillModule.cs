using System;
using DevLib.ModuleSystem;
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
        
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _playerController = owner as PlayerController;
            FDebug.Assert(_playerController != null,"Owner is not PlayerController");

            _playerController.PlayerInput.OnAttackKeyPressed += AttackKeyPressed;
        }

        private void OnDestroy()
        {
            _playerController.PlayerInput.OnAttackKeyPressed -= AttackKeyPressed;
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
        {
            if (CanUseSkill(meleeSkill))
            {
                UseSkill(meleeSkill);
            }
        }
        
        
    }
}