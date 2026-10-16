using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.SkillSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Modules
{
    public class PlayerSkillModule : SkillModule, IPlayerSkillModule,ICanInputOnOff
    {
        [SerializeField] private SkillSO meleeSkill;
        [SerializeField] private SkillSO skill1;
        [SerializeField] private SkillSO skill2;
        
        private PlayerController _playerController;
        private PlayerInputSo _input;

        private bool inputActive;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _playerController = owner as PlayerController;
            FDebug.Assert(_playerController != null,"Owner is not PlayerController");

            _input = _playerController.PlayerInput;
            FDebug.Assert(_input != null,"Input SO is null");
            
            _input.OnMeleeSkillKeyPressed += AttackKeyPressed;
            _input.OnSkill1KeyPressed += Skill1KeyPressed;
            _input.OnSkill2KeyPressed += Skill2KeyPressed;

            inputActive = true;
        }

        private void OnDestroy()
        {
            _input.OnMeleeSkillKeyPressed -= AttackKeyPressed;
            _input.OnSkill1KeyPressed -= Skill1KeyPressed;
            _input.OnSkill2KeyPressed -= Skill2KeyPressed;
        }

        public void SetSkill(PlayerSkillSlot slot,SkillSO skill)
        {
            switch (slot)
            {
                case PlayerSkillSlot.Melee:
                    meleeSkill = skill;
                    break;
                case PlayerSkillSlot.First:
                    skill1 = skill;
                    break;
                case PlayerSkillSlot.Second:
                    skill2 = skill;
                    break;
            }
        }
        
        private void AttackKeyPressed(bool pressed)
            => TryUseSkill(meleeSkill, pressed);
        
        private void Skill1KeyPressed(bool pressed)
            => TryUseSkill(skill1, pressed);
        private void Skill2KeyPressed(bool pressed)
            => TryUseSkill(skill2, pressed);
        

        private void TryUseSkill(SkillSO skillSo,bool pressed)
        {
            if(!inputActive) return;
            if (CanUseSkill(skillSo))
            {
                UseSkill(skillSo, pressed);
            }
        }


        public void SetActive(bool active)
        {
            inputActive = active;
        }
        
    }
}