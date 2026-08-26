using DevLib.ModuleSystem;
using DG.Tweening;
using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;
using CoreSystem.Effect;
using CoreSystem.EffectSystem;

namespace Agents.Players
{
    public class PlayerDasher : Module, IControlDasher
    {
        [SerializeField] private float dashTime = 0.1f;
        public float DashTime => dashTime;

        [SerializeField] private float dashRange = 2f;
        [SerializeField] private float dashCoolTime = 0.2f;
        [SerializeField] private int maxAirDashCount = 1;
        [SerializeField] private AssetNameSo assetNameSo;
        
        private Rigidbody2D _playerRb;
        private IRenderer _renderer;
        private IGroundChecker _groundChecker;
        private IAfterImageEmitter _afterImageEmitter;
        
        private Tween _dashTween;
        private float _lastDashTime = float.NegativeInfinity;
        private int _currentAirDashCount;

        private bool _canDash = true;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);

            _groundChecker = _owner.GetModule<IGroundChecker>();
            _playerRb = _owner.GetComponent<Rigidbody2D>();
            _afterImageEmitter = owner.GetModule<IAfterImageEmitter>();

            Debug.Assert(_playerRb != null, "Player에는 Rigidbody2D가 필요합니다.");
            Debug.Assert(_groundChecker != null, "Player에는 IGroundChecker도 필요합니다.");
            Debug.Assert(_afterImageEmitter != null, "Player에는 IAfterImageEmitter 필요합니다.");
        }


        public bool CanDash()
        {
            if (!_canDash)
                return false;

            if (_groundChecker.IsGroundChecking())
                return true;

            return _currentAirDashCount < maxAirDashCount;
        }
        
        public void Dash()
        {
            if (Time.time < _lastDashTime + dashCoolTime || !_canDash)
                return;

            Camera cam = Camera.main;

            if (cam == null || Mouse.current == null)
                return;
            
            Vector2 mousePos = Mouse.current.position.ReadValue();
            Vector2 worldMousePos = cam.ScreenToWorldPoint(mousePos);

            Vector2 direction =
                worldMousePos - _playerRb.position;

            if (direction.sqrMagnitude <= 0f)
                return;

            direction.Normalize();
            
            _afterImageEmitter.Play(DashTime);

            float rotationY = direction.x >= 0f ? 0f : 180f;

            _owner.transform.rotation =
                Quaternion.Euler(0f, rotationY, 0f);

            Vector2 targetPosition =
                _playerRb.position + direction * dashRange;
            

            _lastDashTime = Time.time;

            _dashTween?.Kill();

            _playerRb.linearVelocity = Vector2.zero;

            _dashTween = _playerRb
                .DOMove(targetPosition, dashTime)
                .SetEase(Ease.Linear)
                .SetUpdate(UpdateType.Fixed)
                .OnComplete(DashCoolTimeCoroutineStart);

            if (!_groundChecker.IsGroundChecking()) _currentAirDashCount++;
        }

        public void ResetAirDashCount()
        {
            _currentAirDashCount = 0;
        }
        
        private void DashCoolTimeCoroutineStart()
        {
            StartCoroutine(DashCoolTimeCoroutine());
        }

        private IEnumerator DashCoolTimeCoroutine()
        {
            _canDash = false;
            yield return new WaitForSeconds(dashCoolTime);
            _canDash = true;
        }

        private void OnDestroy()
        {
            _dashTween?.Kill();
        }
    }
}