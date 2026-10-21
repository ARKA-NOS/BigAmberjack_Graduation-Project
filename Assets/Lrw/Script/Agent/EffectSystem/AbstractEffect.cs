using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.EffectSystem
{
    public abstract class AbstractEffect : IEffect
    {
        protected readonly ModuleOwner Owner;
        public bool IsEnd { get; protected set; }

        protected AbstractEffect(ModuleOwner owner) 
        {
            Owner = owner;
            IsEnd = false;
        }

        public void Enter()
        {
            OnEnter();
        }

        public void Update()
        {
            OnUpdate();
        }

        public void Exit()
        {
            OnExit();
        }
        
        protected virtual void OnEnter() {}
        protected virtual void OnUpdate() {}
        protected virtual void OnExit() {}
        
    }
}