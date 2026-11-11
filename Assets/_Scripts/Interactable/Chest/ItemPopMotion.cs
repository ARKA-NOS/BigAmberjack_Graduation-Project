using UnityEngine;

namespace _Scripts.Interactable.Chest
{
    // 상자에서 나온 아이템 픽업이 코인처럼 위로 튀어 오른 뒤 아래 Ground까지 떨어져 통통 튀고 멈추게 하는 연출.
    // 움직이는 동안은 물리와 콜라이더를 꺼서 줍지 못하게 하고, 멈추면 원래대로 돌려 일반 픽업처럼 동작한다.
    public class ItemPopMotion : MonoBehaviour
    {
        private const float UpwardSpeed = 7f;
        private const float Gravity = 20f;
        private const float HorizontalDrift = 0.6f;
        private const int MaxBounces = 2;
        private const float BounceDamping = 0.45f;
        // 바닥을 찾지 못해도 이 시간이 지나면 멈춘다. 바운스할 때마다 다시 센다.
        private const float MaxAirTime = 3f;

        // Ground(6번)에는 어디서든, Platform(10번) 발판에는 위에서 내려올 때만 착지한다.
        private const int GroundLayer = 6;
        private const int PlatformLayer = 10;
        private static readonly int LandMask = (1 << GroundLayer) | (1 << PlatformLayer);
        private readonly RaycastHit2D[] _hits = new RaycastHit2D[8];

        private Rigidbody2D _body;
        private Collider2D _collider;
        private float _halfHeight;
        private Vector3 _position;
        private Vector2 _velocity;
        private float _airTime;
        private int _bounces;
        private bool _running;

        public static void Play(GameObject pickup, Sprite icon)
        {
            pickup.AddComponent<ItemPopMotion>().Begin(icon);
        }

        private void Begin(Sprite icon)
        {
            _body = GetComponent<Rigidbody2D>();
            _collider = GetComponent<Collider2D>();

            // 물리 중력과 겹치지 않도록 직접 움직이는 동안만 끈다.
            if (_body != null)
                _body.simulated = false;
            if (_collider != null)
                _collider.enabled = false;

            _halfHeight = icon != null ? icon.bounds.extents.y * Mathf.Abs(transform.lossyScale.y) : 0.25f;
            _position = transform.position;
            _velocity = new Vector2(Random.Range(-HorizontalDrift, HorizontalDrift), UpwardSpeed);
            _running = true;
        }

        private void Update()
        {
            if (!_running)
                return;

            float dt = Time.deltaTime;
            _airTime += dt;
            _velocity.y -= Gravity * dt;
            Vector3 next = _position + (Vector3)(_velocity * dt);

            // 내려가는 중에는 이동 경로 아래의 Ground를 찾아 그 위에 착지한다.
            bool landed = false;
            if (_velocity.y < 0f)
            {
                float distance = _position.y - next.y + _halfHeight;
                if (TryFindLanding(distance, out float landingY))
                {
                    next.y = landingY;
                    landed = true;
                }
            }

            _position = next;
            transform.position = _position;

            if (landed)
            {
                _airTime = 0f;

                if (_bounces >= MaxBounces)
                {
                    Finish();
                    return;
                }

                // 튈 때마다 높이와 옆으로 가는 속도가 줄어든다.
                _velocity.y = UpwardSpeed * Mathf.Pow(BounceDamping, _bounces + 1);
                _velocity.x *= 0.65f;
                _bounces++;
            }
            else if (_airTime >= MaxAirTime)
            {
                // 바닥이 없는 곳(낭떠러지 등)이면 그 자리에서 멈춘다.
                Finish();
            }
        }

        // 아래로 쏜 레이에서 가장 가까운 착지면의 중심 높이를 찾는다. 발판은 아이콘 바닥이 윗면보다 위에 있을 때만 인정한다(아래에서 통과).
        private bool TryFindLanding(float distance, out float landingY)
        {
            landingY = 0f;
            int count = Physics2D.RaycastNonAlloc(_position, Vector2.down, _hits, distance, LandMask);
            float closest = float.PositiveInfinity;
            bool found = false;

            for (int i = 0; i < count; i++)
            {
                RaycastHit2D hit = _hits[i];
                if (hit.collider == null || hit.distance >= closest || hit.normal.y < 0.5f)
                    continue;
                if (hit.collider.gameObject.layer == PlatformLayer && hit.point.y + _halfHeight > _position.y + 0.05f)
                    continue;

                closest = hit.distance;
                landingY = hit.point.y + _halfHeight;
                found = true;
            }

            return found;
        }

        private void Finish()
        {
            _running = false;
            _velocity = Vector2.zero;

            // 멈춘 자리에 그대로 있도록 중력은 켜지 않고, 줍기만 가능하게 콜라이더를 되살린다.
            if (_body != null)
            {
                _body.bodyType = RigidbodyType2D.Kinematic;
                _body.simulated = true;
            }
            if (_collider != null)
                _collider.enabled = true;

            Destroy(this);
        }
    }
}
