using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.Players.Modules;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 이동·점프·대시·공격·스킬 입력 없이 서 있는 시간(초). 입력이 들어오면 0으로 돌아간다.
    public class IdleQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField] private PlayerInputSo playerInput;

        private IControlMovement _movement;
        private float _idleTime;

        protected override void OnInit(ModuleOwner owner)
        {
            FDebug.Assert(playerInput != null, $"[IdleQuestUpdateModule : {name}] playerInput is null");
            FDebug.Assert(owner.TryGetModule(out _movement),
                $"[IdleQuestUpdateModule : {owner.name}] IControlMovement is not found");

            playerInput.OnJumpKeyPressed += ResetIdle;
            playerInput.OnDashKeyPressed += ResetIdle;
            playerInput.OnMeleeSkillKeyPressed += OnSkillKey;
            playerInput.OnSkill1KeyPressed += OnSkillKey;
            playerInput.OnSkill2KeyPressed += OnSkillKey;
        }

        protected override void OnRelease()
        {
            playerInput.OnJumpKeyPressed -= ResetIdle;
            playerInput.OnDashKeyPressed -= ResetIdle;
            playerInput.OnMeleeSkillKeyPressed -= OnSkillKey;
            playerInput.OnSkill1KeyPressed -= OnSkillKey;
            playerInput.OnSkill2KeyPressed -= OnSkillKey;
        }

        private void OnSkillKey(bool isPressed)
        {
            if (isPressed) ResetIdle();
        }

        private void ResetIdle()
        {
            _idleTime = 0f;
            ResetQuestAmount();
        }

        private void Update()
        {
            if (!IsInitialized) return;

            if (!Mathf.Approximately(_movement.MovementDirectionX, 0f))
            {
                ResetIdle();
                return;
            }

            _idleTime += Time.deltaTime;
            SetQuestAmount(_idleTime);
        }
    }
}
