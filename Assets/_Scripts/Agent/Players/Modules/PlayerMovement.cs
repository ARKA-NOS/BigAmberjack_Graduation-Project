using _Scripts._Core._Debug;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.StatSystem;
using _Scripts.Audio;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.Modules
{
    public class PlayerMovement : Module, IControlMovement, IAfterInitModule, ICanInputOnOff
    {
        [SerializeField] private StatData moveSpeed;

        [SerializeField] private SoundCueSO[] moveSoundCues;
        [SerializeField] private float stepInterval = 0.35f;

        private Rigidbody2D _playerRb;
        
        private IRenderer _renderer;
        private IStatModule _statModule;
        public float MovementDirectionX { get; private set; }
        
        private Stat _moveSpeedStat;
        
        private bool _isMoveLock = false;

        private ISoundManager _soundManager;
        private IGroundChecker _groundChecker;
        private float _stepTimer;
        private int _stepIndex;
        
        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _renderer = _owner.GetModule<IRenderer>();
            FDebug.Assert(_renderer != null,"IRenderer is Not Found");
            
            _statModule = _owner.GetModule<IStatModule>();
            FDebug.Assert(_statModule != null,"Stat Module is Not Found");
            
            _playerRb = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_playerRb != null,"Rigidbody2D is Not Found");

            _soundManager = ServiceLocator.Get<ISoundManager>();
            FDebug.Assert(_soundManager != null, "Soundmangaer is not Found");

            _groundChecker = _owner.GetModule<IGroundChecker>();
            FDebug.Assert(_groundChecker != null, "IGroundChecker is Not Found");
        }
        
        public void AfterInit()
        {
            _moveSpeedStat = _statModule.GetStat(moveSpeed);
        }
        
        public void SetMovementDirectionX(float movementXInput)
        {
            MovementDirectionX = movementXInput;
        }
        
        public void MoveXLock(bool value)
        {
            _isMoveLock = value;
            if(value) _playerRb.linearVelocityX = 0;
        }

        public void SetVelocityX(float velocityX) => _playerRb.linearVelocityX = velocityX;
        
        private void FixedUpdate()
        {
            MovePlayer();
        }
        
        private void MovePlayer() 
        {
            if (_playerRb == null) return;
            if(_isMoveLock) return;
            _playerRb.linearVelocityX = MovementDirectionX * _moveSpeedStat.Value;
            UpdateStepSound();
        }

        private void UpdateStepSound()
        {
            bool isWalking = !Mathf.Approximately(MovementDirectionX, 0f)
                             && _groundChecker != null && _groundChecker.FixedIsGround;
            if (!isWalking)
            {
                _stepTimer = 0f;
                return;
            }

            _stepTimer -= Time.fixedDeltaTime;
            if (_stepTimer > 0f) return;

            _stepTimer = stepInterval;
            PlayStepSound();
        }

        private void PlayStepSound()
        {
            if (moveSoundCues == null || moveSoundCues.Length == 0) return;

            _soundManager.PlaySfx(moveSoundCues[_stepIndex], transform.position);
            _stepIndex = (_stepIndex + 1) % moveSoundCues.Length;
        }
        
        public void AddForce(Vector2 force, ForceMode2D impulse = ForceMode2D.Impulse) 
            => _playerRb.AddForce(force,impulse);
        
        public void SetActive(bool active)
        {
            
        }
        
    }
}