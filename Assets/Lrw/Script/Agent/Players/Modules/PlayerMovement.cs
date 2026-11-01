using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Modules
{
    public class PlayerMovement : Module, IControlMovement, IAfterInitModule, ICanInputOnOff
    {
        [SerializeField] private StatData moveSpeed;
        private Rigidbody2D _playerRb;
        
        private IRenderer _renderer;
        private IStatModule _statModule;
        public float MovementDirectionX { get; private set; }

        private Stat _moveSpeedStat;

        private bool _isMoveLock = false;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _renderer = _owner.GetModule<IRenderer>();
            FDebug.Assert(_renderer != null,"IRenderer is Not Found");
            
            _statModule = _owner.GetModule<IStatModule>();
            FDebug.Assert(_statModule != null,"Stat Module is Not Found");
            
            _playerRb = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_playerRb != null,"Rigidbody2D is Not Found");
        }
        
        public void AfterInit()
        {
            _moveSpeedStat = _statModule.GetStat(moveSpeed);
        }

        public void SetMovementDirectionX(float movementXInput)
        {
            MovementDirectionX = movementXInput;
        }

        public void MoveLock(bool value)
        {
            _isMoveLock = value;
            if(value) _playerRb.linearVelocityX = 0;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }
        
        private void MovePlayer() 
        {
            if (_playerRb == null) return;
            if(_isMoveLock) return;
            _playerRb.linearVelocityX = MovementDirectionX * _moveSpeedStat.Value;
        }

        public void AddForce(Vector2 force, ForceMode2D impulse = ForceMode2D.Impulse) 
            => _playerRb.AddForce(force,impulse);


        public void SetActive(bool active)
        {
            
        }
    }
}