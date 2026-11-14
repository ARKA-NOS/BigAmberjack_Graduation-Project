using _Scripts._Core._Debug;
using _Scripts.Agent.SkillSystem;
using _Scripts.Agent.SkillSystem.Interface;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.Modules
{
    public class PlayerSkillModule : SkillModule, IPlayerSkillModule,ICanInputOnOff
    {
        [SerializeField] private SkillSO dashSkill;
        
        [Header("Skill Key")]
        [SerializeField] private SkillSO meleeSkill;
        [SerializeField] private SkillSO skill1;
        [SerializeField] private SkillSO skill2;

        [Header("Input Buffer")]
        [SerializeField] private float meleeInputBufferTime = 0.2f;

        private SkillInputBuffer _inputBuffer;

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
            
            _input.OnDashKeyPressed += DashKeyPressed;
            _input.OnMeleeSkillKeyPressed += AttackKeyPressed;
            _input.OnSkill1KeyPressed += Skill1KeyPressed;
            _input.OnSkill2KeyPressed += Skill2KeyPressed;

            _inputBuffer = new SkillInputBuffer(meleeInputBufferTime);

            inputActive = true;

        }

        private void Update()
        {
            if (!_inputBuffer.TryGet(Time.time, out SkillSO buffered)) return;
            if (!CanUseSkill(buffered)) return;

            _inputBuffer.Clear();
            UseSkill(buffered, true);
        }
        
        private void OnDestroy()
        {
            _input.OnDashKeyPressed -= DashKeyPressed;
            _input.OnMeleeSkillKeyPressed -= AttackKeyPressed;
            _input.OnSkill1KeyPressed -= Skill1KeyPressed;
            _input.OnSkill2KeyPressed -= Skill2KeyPressed;
        }

        public ISkill GetSlotSkill(PlayerSkillSlot slot)
        {
            return slot switch
            {
                PlayerSkillSlot.Melee => GetSkill(meleeSkill),
                PlayerSkillSlot.First => GetSkill(skill1),
                PlayerSkillSlot.Second => GetSkill(skill2),
                _ => null,
            };
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
        
        private void DashKeyPressed()
            => TryUseSkill(dashSkill, true);
        
        private void AttackKeyPressed(bool pressed)
        {
            if (TryUseSkill(meleeSkill, pressed) || !pressed || !inputActive) return;
            if (GetSkill(meleeSkill) is IChargeSkill) return;
            _inputBuffer.Store(meleeSkill, Time.time);
        }

        private void Skill1KeyPressed(bool pressed)
            => TryUseSkill(skill1, pressed);
        private void Skill2KeyPressed(bool pressed)
            => TryUseSkill(skill2, pressed);

        private bool TryUseSkill(SkillSO skillSo,bool pressed)
        {
            if(!inputActive) return false;
            if (!CanUseSkill(skillSo)) return false;

            if (pressed) _inputBuffer.Clear();
            UseSkill(skillSo, pressed);
            return true;
        }

        public void SetActive(bool active)
        {
            inputActive = active;
            if (!active) _inputBuffer?.Clear();
        }
        
    }
}