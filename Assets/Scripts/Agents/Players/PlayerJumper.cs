using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Agents.Players
{
    public class PlayerJumper : Module, IControlJumper,IAfterInitModule
    {
        //[SerializeField] private float jumpFor;//12
        [SerializeField] private StatData jumpPowerStatData;
        
        [SerializeField, Range(0f, 1f)] private float jumpCutMultiplier = 0.5f;

        [field: SerializeField]
        public float CoyoteTime { get; private set; } = 0.1f;

        public bool IsJumpFall { get; set; }

        private Rigidbody2D _playerRb;
        private IStatModule _statModule;
        private Stat _jumpPowerStat;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _playerRb = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_playerRb != null, "Player에는 Rigidbody2D가 필요합니다.");
            
            _statModule = owner.GetModule<IStatModule>();
            FDebug.Assert(_statModule != null,"StatModule is not found");
            
        }
        
        public void AfterInit()
        {
            _jumpPowerStat = _statModule.GetStat(jumpPowerStatData, 1f);
        }

        public void Jump()
        {
            if (_playerRb == null)
                return;

            _playerRb.linearVelocityY = 0f;
            _playerRb.AddForceY(_jumpPowerStat.Value, ForceMode2D.Impulse);
        }

        public void CancelJump()
        {
            if (_playerRb == null)
                return;

            if (_playerRb.linearVelocityY <= 0f)
                return;

            _playerRb.linearVelocityY *= jumpCutMultiplier;
        }

        
    }
}