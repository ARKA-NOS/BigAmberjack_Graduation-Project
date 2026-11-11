using _Scripts._Core._Debug;
using _Scripts.Agent.StatSystem;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Players.Modules
{
    public class PlayerJumper : Module, IControlJumper,IAfterInitModule
    {
        //[SerializeField] private float jumpFor;//12
        [SerializeField] private StatData jumpPowerStatData;

        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;
        [SerializeField, Range(0f, 1f)] private float jumpMinPower = 2f;
        [field: SerializeField]
        public float CoyoteTime { get; private set; } = 0.08f;

        public bool IsJumpFall { get; set; }

        private Rigidbody2D _playerRb;
        private IStatModule _statModule;
        private Stat _jumpPowerStat;

        private IGroundChecker _groundChecker;

        private float _lastGroundedTime = float.NegativeInfinity;
        private bool _coyoteConsumed;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _playerRb = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_playerRb != null, "Player에는 Rigidbody2D가 필요합니다.");

            _statModule = owner.GetModule<IStatModule>();
            FDebug.Assert(_statModule != null,"StatModule is not found");

            _groundChecker = owner.GetModule<IGroundChecker>();
            FDebug.Assert(_groundChecker != null, "GroundChecker가 없습니다");
        }

        private void FixedUpdate()
        {
            if (_groundChecker.FixedIsGround)
                _lastGroundedTime = Time.time;
        }

        public void AfterInit()
        {
            _jumpPowerStat = _statModule.GetStat(jumpPowerStatData);
        }

        public bool CanCoyoteJump()
            => !_coyoteConsumed && Time.time - _lastGroundedTime <= CoyoteTime;

        public void ResetCoyote()
        {
            _coyoteConsumed = false;
        }

        public void Jump()
        {
            if (_playerRb == null)
                return;

            _coyoteConsumed = true;
            _playerRb.linearVelocityY = 0f;
            _playerRb.AddForceY(_jumpPowerStat.Value, ForceMode2D.Impulse);
        }

        public void CancelJump()
        {
            if (_playerRb == null)
                return;

            if (_playerRb.linearVelocityY <= 0f)
                return;

            _playerRb.linearVelocityY = Mathf.Max(_playerRb.linearVelocityY * jumpCutMultiplier,jumpMinPower);
        }
    }
}
