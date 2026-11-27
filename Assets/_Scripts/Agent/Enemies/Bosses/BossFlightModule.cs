using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Bosses
{
    // 지상 이동 모듈과 함께 쓰지 않습니다. 보스 FSM만 FixedUpdate에서 Tick을 호출합니다.
    [DisallowMultipleComponent]
    public sealed class BossFlightModule : Module
    {
        [Tooltip("최고 속도까지 가속하는 시간이며 감속에도 같은 값을 사용합니다. 짧은 이동은 최고 속도를 낮춥니다.")]
        [SerializeField, Min(0.01f)] private float easeDuration = 0.2f;
        [Tooltip("출발 시 최고 속도로 움직이고 도착할수록 부드럽게 감속합니다. 끄면 기존 가속·등속·감속을 사용합니다.")]
        [SerializeField] private bool decelerateFromStart;

        private Rigidbody2D _body;
        private Vector2 _start;
        private Vector2 _destination;
        private float _distance;
        private float _peakSpeed;
        private float _rampTime;
        private float _cruiseTime;
        private float _duration;
        private float _elapsed;
        private bool _deceleratingMove;

        public bool IsMoving { get; private set; }
        public bool HasArrived => _body != null && !IsMoving && Vector2.Distance(_body.position, _destination) <= 0.01f;

        // 최고 속도에서 정지하는 데 필요한 거리. 레이저 퇴장 목적지를 화면 밖으로 연장할 때 사용합니다.
        public float GetBrakingDistance(float speed, float? easeOverride = null) =>
            0.5f * Mathf.Max(0.01f, speed) * ResolveEaseDuration(easeOverride);

        private float ResolveEaseDuration(float? easeOverride) => Mathf.Max(0.01f, easeOverride ?? easeDuration);

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _body = owner.GetComponent<Rigidbody2D>();
            _body.bodyType = RigidbodyType2D.Kinematic;
            _body.gravityScale = 0f;
            Stop();
        }

        public void Place(Vector2 position)
        {
            SetPosition(position);
            Stop();
        }

        // 이번 이동만 가감속을 다르게 지정할 수 있습니다. 생략하면 일반 이동 설정을 그대로 사용합니다.
        public void MoveTo(Vector2 destination, float speed, float? easeOverride = null)
        {
            _start = _body.position;
            _destination = destination;
            _distance = Vector2.Distance(_start, destination);
            _elapsed = 0f;
            _deceleratingMove = decelerateFromStart;

            if (_distance <= 0.0001f)
            {
                Place(destination);

                return;
            }

            float maxSpeed = Mathf.Max(0.01f, speed);

            if (_deceleratingMove)
            {
                _peakSpeed = maxSpeed;
                _duration = 2f * _distance / maxSpeed;
                IsMoving = true;

                return;
            }

            float ramp = ResolveEaseDuration(easeOverride);

            // 가속+감속만으로 목적지를 넘어갈 짧은 거리에서는 최고 속도를 낮추고 등속 구간을 생략합니다.
            _rampTime = Mathf.Min(ramp, Mathf.Sqrt(_distance * ramp / maxSpeed));
            _peakSpeed = maxSpeed * _rampTime / ramp;
            _cruiseTime = Mathf.Max(0f, _distance / _peakSpeed - _rampTime);
            _duration = 2f * _rampTime + _cruiseTime;
            IsMoving = true;
        }

        // 타임라인에 지정된 특수 이동. 이동 거리가 지형에 의해 줄어도 동작 시간은 유지합니다.
        public void MoveToDuration(Vector2 destination, float duration)
        {
            if (!decelerateFromStart)
            {
                duration = Mathf.Max(0.01f, duration);
                float ramp = Mathf.Min(ResolveEaseDuration(null), duration * 0.45f);
                float speed = Vector2.Distance(_body.position, destination) / (duration - ramp);
                MoveTo(destination, speed, ramp);

                return;
            }

            MoveTo(destination, 1f);
            if (!IsMoving) return;

            _deceleratingMove = true;
            _duration = Mathf.Max(0.01f, duration);
            _peakSpeed = 2f * _distance / _duration;
        }

        public void Tick()
        {
            if (!isActiveAndEnabled || _body == null || !IsMoving) return;

            _elapsed = Mathf.Min(_duration, _elapsed + Time.fixedDeltaTime);
            float travelled;
            float acceleration = _peakSpeed / _rampTime;

            if (_deceleratingMove)
            {
                // 최고 속도에서 SmoothStep으로 0까지 줄이는 속도를 적분해 정확한 끝점에 도착합니다.
                float t = _elapsed / _duration;
                travelled = _distance * (2f * t - 2f * t * t * t + t * t * t * t);
            }
            else if (_elapsed < _rampTime)
                travelled = 0.5f * acceleration * _elapsed * _elapsed;
            else if (_elapsed < _rampTime + _cruiseTime)
                travelled = _peakSpeed * (_elapsed - 0.5f * _rampTime);
            else
            {
                float remaining = _duration - _elapsed;

                travelled = _distance - 0.5f * acceleration * remaining * remaining;
            }

            // 매 틱 현재 위치를 Lerp하지 않고 출발점 기준 누적 거리를 사용해, 유한 시간 안에 정확히 도착합니다.
            SetPosition(Vector2.Lerp(_start, _destination, travelled / _distance));

            if (_elapsed >= _duration)
            {
                SetPosition(_destination);
                IsMoving = false;
            }
        }

        private void SetPosition(Vector2 position)
        {
            _body.position = position;
            // Rigidbody.position만 대입하면 자식 Transform은 물리 시뮬레이션 전까지 이전 위치일 수 있습니다.
            // 접촉 해석을 하지 않는 Kinematic 비행체이므로 같은 틱의 FirePoint/빔에도 새 좌표를 반영합니다.
            _body.transform.position = new Vector3(position.x, position.y, _body.transform.position.z);
        }

        public void Stop()
        {
            IsMoving = false;
            _elapsed = 0f;

            if (_body == null) return;

            _body.linearVelocity = Vector2.zero;
            _destination = _body.position;
        }

        private void OnDisable() => Stop();
    }
}
