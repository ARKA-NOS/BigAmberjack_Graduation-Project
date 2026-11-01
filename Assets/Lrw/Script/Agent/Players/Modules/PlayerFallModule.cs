using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.Modules
{
    // 떨어지는 동안에만 중력을 Fall Speed 스탯 비율로 줄인다(1 = 그대로, 0.6 = 떨어질 때 중력 60%).
    public class PlayerFallModule : Module, IAfterInitModule
    {
        [SerializeField] private StatData fallSpeed;

        private Rigidbody2D _playerRb;
        private IStatModule _statModule;
        private Stat _fallSpeedStat;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _statModule = _owner.GetModule<IStatModule>();
            FDebug.Assert(_statModule != null,"Stat Module is Not Found");

            _playerRb = _owner.GetComponent<Rigidbody2D>();
            FDebug.Assert(_playerRb != null,"Rigidbody2D is Not Found");
        }

        public void AfterInit()
        {
            _fallSpeedStat = _statModule.GetStat(fallSpeed);
        }

        private void FixedUpdate()
        {
            if (_fallSpeedStat == null || _playerRb.linearVelocityY >= 0f) return;

            float reduceRatio = 1f - _fallSpeedStat.Value;
            if (Mathf.Approximately(reduceRatio, 0f)) return;

            _playerRb.AddForce(-Physics2D.gravity * (_playerRb.gravityScale * _playerRb.mass * reduceRatio));
        }
    }
}
