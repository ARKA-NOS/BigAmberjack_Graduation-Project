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
        private bool _isUseing = false;
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _playerController = owner as PlayerController;
            FDebug.Assert(_playerController != null,"Owner is not PlayerController");

            _playerController.PlayerInput.OnAttackKeyPressed += AttackKeyPressed;
            _playerController.PlayerInput.OnAttackKeyReleased += AttackKeyReleased;
        }

        private void OnDestroy()
        {
            _playerController.PlayerInput.OnAttackKeyPressed -= AttackKeyPressed;
            _playerController.PlayerInput.OnAttackKeyReleased -= AttackKeyReleased;
        }
        
        private void AttackKeyPressed()
            => _isUseing = true;

        private void AttackKeyReleased()
            => _isUseing = false;

        
        private void Update()
        {
            if (_isUseing && CanUseSkill(meleeSkill))
            {
                UseSkill(meleeSkill);
            }
        }
    }
}