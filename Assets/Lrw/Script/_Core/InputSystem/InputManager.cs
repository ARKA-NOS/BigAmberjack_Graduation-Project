using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Lrw.Script._Core.InputSystem
{
    public interface IInputManager
    {
        event Action OnDashKeyPressed;
        event Action OnAttackKeyPressed;
        event Action OnJumpKeyPressed;
        event Action OnJumpKeyReleased;
        Vector2 MovementKey { get; }
    }

    public class InputManager : Controls.IPlayerActions, IInputManager
    {
        private static Controls _controls;
        
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void Init()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                InputManager input = new InputManager();
                Input = input;
                _controls.Player.AddCallbacks(input);
            }
            _controls.Player.Enable();
        }
        
        ~InputManager()
        {
            _controls.Player.Disable();
        }

        public static IInputManager Input { get; private set; }
        
        public event Action OnDashKeyPressed;
        public event Action OnAttackKeyPressed;
        public event Action OnJumpKeyPressed;
        public event Action OnJumpKeyReleased;
        public Vector2 MovementKey { get; private set; }
        
        public void OnMove(InputAction.CallbackContext context)
        {
            MovementKey = context.ReadValue<Vector2>(); 
        }

        public void OnJump(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnJumpKeyPressed?.Invoke();            
            if (context.canceled)
                OnJumpKeyReleased?.Invoke();
        }

        public void OnAttack(InputAction.CallbackContext context)
        {
            if (context.canceled)
                OnAttackKeyPressed?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }
    }
}