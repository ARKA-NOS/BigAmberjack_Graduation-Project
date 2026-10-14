using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace Agents.Players
{
    [CreateAssetMenu(fileName = "PlayerInput", menuName = "SO/Player input", order = 0)]
    public class PlayerInputSo : ScriptableObject, Controls.IPlayerActions
    {
        public event Action OnDashKeyPressed;

        #region SkillKeyEvent

        public event Action<bool> OnMeleeSkillKeyPressed;
        public event Action<bool> OnSkill1KeyPressed;
        public event Action<bool> OnSkill2KeyPressed;

        #endregion
        
        public event Action OnJumpKeyPressed;
        public event Action OnJumpKeyReleased;
        
        public event Action OnInteractKeyPressed;
        public event Action OnInventoryKeyReleased;

        private Controls _controls;
        
        public Vector2 MovementKey { get; private set; }
        public Vector2 MousePos { get; private set; }

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
                OnMeleeSkillKeyPressed?.Invoke(true);
            if (context.canceled)
                OnMeleeSkillKeyPressed?.Invoke(false);
        }

        public void OnSkill1(InputAction.CallbackContext context)
        {
            if(context.started)
                OnSkill1KeyPressed?.Invoke(true);
            if (context.canceled)
                OnSkill1KeyPressed?.Invoke(false);
        }

        public void OnSkill2(InputAction.CallbackContext context)
        {
            if(context.started)
                OnSkill2KeyPressed?.Invoke(true);
            if (context.canceled)
                OnSkill2KeyPressed?.Invoke(false);
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

        public void OnMouse(InputAction.CallbackContext context)
        {
            MousePos = context.ReadValue<Vector2>();
        }
        
        public Vector2 GetMouseWorldPosition()
            => Camera.main.ScreenToWorldPoint(MousePos);

        public void OnDash(InputAction.CallbackContext context)
        {
            if (context.performed)
                OnDashKeyPressed?.Invoke();
        }
    }
}