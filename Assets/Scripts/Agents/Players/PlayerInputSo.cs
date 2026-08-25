using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Agents.Players
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/Player input", order = 0)]
    public class PlayerInputSo : ScriptableObject, Controls.IPlayerActions
    {
        [SerializeField] private LayerMask whatIsGround;
        
        public event Action OnDashKeyPressed;
        public event Action OnJumpKeyPressed;
        public event Action OnJumpKeyReleased;

        private Controls _controls;
        
        public Vector2 MovementKey { get; private set; }
        

        private void OnEnable()
        {
            if (_controls == null)
            {
                _controls = new Controls();
                _controls.Player.SetCallbacks(this);
            }
            _controls.Player.Enable();
        }

        private void OnDisable()
        {
            if(_controls != null)
                _controls.Player.Disable();
        }
        
        
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

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }
    }
}