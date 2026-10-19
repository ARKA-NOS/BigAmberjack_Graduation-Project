using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.EffectSystem
{
    public abstract class AbstractEffect : IEffect
    {
        public EffectType Type { get; private set; }
        protected readonly ModuleOwner Owner;
        public bool IsEnd { get; private set; }

        protected AbstractEffect(EffectType type,ModuleOwner owner) 
        {
            Type = type; 
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