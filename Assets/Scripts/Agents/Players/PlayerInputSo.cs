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

        #region SkillKeyEvent

        public event Action OnAttackKeyPressed;
        public event Action OnAttackKeyReleased;

        public event Action OnSkill1KeyPressed;
        public event Action OnSkill1KeyReleased;
        
        public event Action OnSkill2KeyPressed;
        public event Action OnSkill2KeyReleased;

        #endregion
        
        public event Action OnJumpKeyPressed;
        public event Action OnJumpKeyReleased;
        
        public event Action OnInteractKeyPressed;
        public event Action OnInventoryKeyReleased;

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

        public void OnAttack(InputAction.CallbackContext context)
        {
            if(context.started)
                OnAttackKeyPressed?.Invoke();
            if (context.canceled)
                OnAttackKeyReleased?.Invoke();
        }

        public void OnSkill1(InputAction.CallbackContext context)
        {
            if(context.started)
                OnSkill1KeyPressed?.Invoke();
            if (context.canceled)
                OnSkill1KeyReleased?.Invoke();
        }

        public void OnSkill2(InputAction.CallbackContext context)
        {
            if(context.started)
                OnSkill2KeyPressed?.Invoke();
            if (context.canceled)
                OnSkill2KeyReleased?.Invoke();
        }

        public void OnInteract(InputAction.CallbackContext context)
        {
            if(context.started)
                OnInteractKeyPressed?.Invoke();
        }

        public void OnInventory(InputAction.CallbackContext context)
        {
            if(context.started)
                OnInventoryKeyReleased?.Invoke();
        }

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }
    }
}