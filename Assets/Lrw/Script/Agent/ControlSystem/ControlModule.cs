using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.ControlSystem
{
    public class ControlModule : Module
    {
        public IPlayable NormalAttack { get; private set; }
        public IPlayable Dash { get; private set; }
        public IPlayable SkillUse { get; private set; }
        
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
        }
        
        
    }
}