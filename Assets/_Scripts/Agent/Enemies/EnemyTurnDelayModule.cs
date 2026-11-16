using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyTurnDelayModule : Module
    {
        [Tooltip("플레이어가 반대편에 머물러야 방향을 바꾸는 시간입니다.")]
        [SerializeField, Min(0f)] private float turnDelay = 0.45f;
        [Tooltip("중심선 부근에서 좌우 판정이 반복되지 않도록 무시하는 수평 거리입니다.")]
        [SerializeField, Min(0f)] private float centerDeadZone = 0.1f;

        private float _requestedDirection;
        private float _turnAt;

        internal bool TryResolveFacing(float currentDirection, float targetOffset, out float direction)
        {
            direction = currentDirection;
            float requestedDirection = Mathf.Sign(targetOffset);
            if (Mathf.Abs(targetOffset) <= centerDeadZone || requestedDirection == currentDirection)
            {
                ResetTurn();
                return true;
            }

            if (_requestedDirection != requestedDirection)
            {
                _requestedDirection = requestedDirection;
                _turnAt = Time.time + turnDelay;
            }
            if (Time.time < _turnAt) return false;

            direction = requestedDirection;
            ResetTurn();
            return true;
        }

        internal void ResetTurn() => _requestedDirection = 0f;

        private void OnDisable() => ResetTurn();
    }
}
