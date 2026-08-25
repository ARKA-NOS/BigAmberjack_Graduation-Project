using UnityEngine;
using System.Collections;
using Agents.Players.Enum;

namespace Agents.Players.FSM
{
    public class PlayerDashState : AbstractPlayerState
    {
        private readonly IControlDasher _dasher;
        
        public PlayerDashState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _dasher = agent.GetModule<IControlDasher>();
            Debug.Assert(_dasher != null, "대쉬 친구가 없어영");
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
            _dasher.Dash();
            Player.StartCoroutine(DashEndTime());
        }

        private IEnumerator DashEndTime()
        {
            yield return new WaitForSeconds(_dasher.DashTime);
            DashEnd();
        }

        private void DashEnd()
        {
            Player.ChangeState(PlayerStateEnum.IDLE, 0.1f);
            _renderer.Animator.transform.rotation = Quaternion.Euler(
                _renderer.Animator.transform.eulerAngles.x,
                _renderer.Animator.transform.eulerAngles.y,
                0f);       
        }
    }
}