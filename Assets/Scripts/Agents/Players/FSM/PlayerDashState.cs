using UnityEngine;
using System.Collections;
using Agents.Players.Enum;

namespace Agents.Players.FSM
{
    public class PlayerDashState : AbstractPlayerState
    {
        private readonly IControlDasher _controlDasher;
        private readonly IControlJumper _controlJumper;
        
        public PlayerDashState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash, layerIndex)
        {
            _controlDasher = agent.GetModule<IControlDasher>();
            Debug.Assert(_controlDasher != null, "대쉬 친구가 없어영");            
            _controlJumper = agent.GetModule<IControlJumper>();
            Debug.Assert(_controlJumper != null, "점퍼 친구가 없어영");
        }

        public override void Enter(float transitionDuration = 0.1f)
        {
            base.Enter(transitionDuration);
            _controlDasher.Dash();
            Player.StartCoroutine(DashEndTime());
        }

        private IEnumerator DashEndTime()
        {
            yield return new WaitForSeconds(_controlDasher.DashTime);
            DashEnd();
        }

        private void DashEnd()
        {
            Player.ChangeState(PlayerStateEnum.FALL, 0.1f);
        }
    }
}