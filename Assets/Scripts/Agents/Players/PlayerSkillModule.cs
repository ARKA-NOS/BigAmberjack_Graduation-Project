using System;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerSkillModule : SkillModule
    {
        [SerializeField] private SkillSO meleeSkill;
        
        private PlayerController _playerController;
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _playerController = owner as PlayerController;
            FDebug.Assert(_playerController != null,"Owner is not PlayerController");

            _playerController.PlayerInput.OnAttackKeyPressed += HandleMeleeSkill;
        }

        private void OnDestroy()
        {
            _playerController.PlayerInput.OnAttackKeyPressed -= HandleMeleeSkill;
        }

        private void HandleMeleeSkill()
        {
            if (CanUseSkill(meleeSkill))
            {
                UseSkill(meleeSkill);
            }
        }
        
        
    }
}