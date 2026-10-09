using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Agents.Players
{
    public abstract class AbstractPlayerModule : Module
    {
        protected PlayerController PlayerController { get; private set; }
        protected PlayerInputSo PlayerInput { get; private set; }
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            PlayerController = owner as PlayerController;
            FDebug.Assert(PlayerController != null,"owner is not PlayerController");
            PlayerInput = PlayerController.PlayerInput;
        }
        
    }
}