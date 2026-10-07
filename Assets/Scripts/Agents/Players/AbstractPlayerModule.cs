using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents.Players
{
    public abstract class AbstractPlayerModule : Module
    {
        protected PlayerController PlayerController;
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            PlayerController = owner as PlayerController;
            FDebug.Assert(PlayerController != null,"owner is not PlayerController");
        }
        
    }
}