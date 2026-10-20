using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 지상 이동 모듈과 함께 쓰지 않습니다. 보스 FSM만 FixedUpdate에서 Tick을 호출합니다.
    [DisallowMultipleComponent]
    public sealed class BossFlightModule : Module
    {
        private Rigidbody2D _body;
        private Vector2 _destination;
        private float _speed;
        public bool IsMoving { get; private set; }
        public bool HasArrived => _body != null && Vector2.Distance(_body.position, _destination) <= 0.01f;

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

        public void MoveTo(Vector2 destination, float speed)
        {
            _destination = destination;
            _speed = Mathf.Max(0.01f, speed);
            IsMoving = true;
        }

        public void Tick()
        {
            if (!isActiveAndEnabled || _body == null || !IsMoving) return;
            // 벽/접촉 충돌 없는 기획의 비행체. 실제 이동 위치로 같은 틱의 레이저 판정도 갱신합니다.
            SetPosition(Vector2.MoveTowards(_body.position, _destination, _speed * Time.fixedDeltaTime));
            if (HasArrived) IsMoving = false;
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
            if (_body == null) return;
            _body.linearVelocity = Vector2.zero;
            _destination = _body.position;
        }

        private void OnDisable() => Stop();
    }
}
