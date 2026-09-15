using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerMovement : Module, IControlMovement,IAfterInitModule
    {
        [SerializeField] private StatData moveSpeed;
        private Rigidbody2D _playerRb;
        
        private IRenderer _renderer;
        private IStatModule _statModule;
        private float _movementDirectionX;

        private Stat _moveSpeedStat;

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
            _moveSpeedStat = _statModule.GetStat(moveSpeed, 1f);
        }

        public void SetMovementDirectionX(float movementXInput)
        {
            _movementDirectionX = movementXInput;
        }

        private void FixedUpdate()
        {
            MovePlayer();
        }
        
        private void MovePlayer() 
        {
            if (_playerRb == null) return;
            _playerRb.linearVelocityX = _movementDirectionX * _moveSpeedStat.Value;
        }

        
    }
}