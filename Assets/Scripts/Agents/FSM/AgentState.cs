namespace Agents.FSM
{
    public abstract class AgentState
    {
        protected readonly Agent _agent;
        protected readonly int _stateClipHash; // 해당 상태의 애니메이션 클립 해시
        protected readonly int _layerIndex; // 해당 상태에서 재생할 애니메이션의 Index
        protected readonly IRenderer _renderer;

        public AgentState(Agent agent, int stateClipHash, int layerIndex)
        {
            _agent = agent;
            _stateClipHash = stateClipHash;
            _layerIndex = layerIndex;
            _renderer = agent.GetModule<IRenderer>();
        }

        public virtual void Enter(float transitionDuration = 0.1f)
        {
            _renderer.PlayClip(_stateClipHash, 0.1f, transitionDuration, _layerIndex);
        }

        public virtual void Update() {}
        public virtual void Exit() {}
    }
}