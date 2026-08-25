using Agents.FSM;
using UnityEngine;

namespace Agents.Players
{
    public abstract class AbstractPlayerState : AgentState
    {
        protected readonly PlayerController Player;
        protected readonly IControlMovement ControlMovement;
        protected const float INPUT_DEADZONE = 0.1f;

        protected AbstractPlayerState(Agent agent, int stateClipHash, int layerIndex) : base(agent, stateClipHash,
            layerIndex)
        {
            Player = agent as PlayerController;
            Debug.Assert(Player != null, "플레이어 상태는 플레이어한테 붙이세요.");
            ControlMovement = agent.GetModule<IControlMovement>();
            Debug.Assert(ControlMovement != null, "플레이어한테 ControlMovement 붙이세요.");
        }
    }
}