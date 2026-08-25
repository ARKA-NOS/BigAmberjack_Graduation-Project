using DevLib.ModuleSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerMovement : Module, IControlMovement
    {
        [SerializeField] private float playerMoveSpeed;
        private Rigidbody2D _playerRb;
        
        private IRenderer _renderer;
        private float _movementDirectionX;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _renderer = _owner.GetModule<IRenderer>();
            _playerRb = _owner.GetComponent<Rigidbody2D>();
        }

        public void SetMovementDirectionX(float movementXInput)
        {
            _movementDirectionX = movementXInput;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }

        public void UpdateFacingDirection(float movementXKey)
        {
            if (movementXKey == 0) return;
            
            Transform rendererTrm = _renderer.Animator.transform;
            
            rendererTrm.rotation = Quaternion.Euler(
                0f, 
                movementXKey > 0 ? 0f : 180f,
                0f);
        }
        
        private void MovePlayer() 
        {
            if (_playerRb == null) return;
            _playerRb.linearVelocityX = _movementDirectionX * playerMoveSpeed;
        }
    }
}