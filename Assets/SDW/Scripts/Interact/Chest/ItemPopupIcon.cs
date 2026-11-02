using UnityEngine;

namespace SDW.Scripts.Interact.Chest
{
    // 상자에서 얻은 아이템 아이콘이 코인처럼 위로 튀어 오른 뒤 아래 Ground까지 떨어져 통통 튀다가 작아지며 사라지는 연출.
    // 스스로 생성/소멸하므로 프리팹이 필요 없다.
    public class ItemPopupIcon : MonoBehaviour
    {
        private const float UpwardSpeed = 7f;
        private const float Gravity = 20f;
        private const float HorizontalDrift = 0.6f;
        private const int MaxBounces = 2;
        private const float BounceDamping = 0.45f;
        private const float HoldTime = 0.3f;
        private const float VanishTime = 0.25f;
        private const float IconWorldSize = 0.9f;
        private const int SortingOrder = 20;
        private const float IconHalfSize = IconWorldSize * 0.5f;
        // 바닥을 찾지 못해도 이 시간이 지나면 멈추고 사라진다. 바운스할 때마다 다시 센다.
        private const float MaxAirTime = 3f;

        // 코인(EnemyRewardCoin)의 기본 지형 레이어와 같은 Ground(6번) 레이어에 착지한다.
        private static readonly int GroundMask = 1 << 6;

        private SpriteRenderer _renderer;
        private Vector3 _position;
        private float _airTime;
        private float _targetScale;
        private Vector2 _velocity;
        private int _bounces;
        private float _landedTime = -1f;

        public static void Spawn(Sprite icon, Vector3 origin)
        {
            if (icon == null)
                return;

            var go = new GameObject("ItemPopupIcon");
            go.transform.position = origin;
            go.AddComponent<ItemPopupIcon>().Setup(icon);
        }

        private void Setup(Sprite icon)
        {
            _renderer = gameObject.AddComponent<SpriteRenderer>();
            _renderer.sprite = icon;
            _renderer.sortingOrder = SortingOrder;

            // 아이콘 원본 크기와 상관없이 비슷한 크기로 보이게 맞춘다.
            Vector3 size = icon.bounds.size;
            float longest = Mathf.Max(size.x, size.y, 0.0001f);
            _targetScale = IconWorldSize / longest;
            transform.localScale = Vector3.one * _targetScale;

            _position = transform.position;
            _velocity = new Vector2(Random.Range(-HorizontalDrift, HorizontalDrift), UpwardSpeed);
        }

        private void Update()
        {
            if (_landedTime < 0f)
            {
                Fall(Time.deltaTime);
                return;
            }

            // 마지막 착지 후 잠시 머물렀다가 코인이 사라지듯 작아지며 투명해진다.
            float t = Time.time - _landedTime - HoldTime;
            if (t < 0f)
                return;

            float k = Mathf.Clamp01(t / VanishTime);
            transform.localScale = Vector3.one * _targetScale * (1f - k);
            Color c = _renderer.color;
            c.a = 1f - k;
            _renderer.color = c;

            if (k >= 1f)
                Destroy(gameObject);
        }

        private void Fall(float dt)
        {
            _airTime += dt;
            _velocity.y -= Gravity * dt;
            Vector3 next = _position + (Vector3)(_velocity * dt);

            // 내려가는 중에는 이동 경로 아래의 Ground를 찾아 그 위에 착지한다.
            bool landed = false;
            if (_velocity.y < 0f)
            {
                float distance = _position.y - next.y + IconHalfSize;
                RaycastHit2D hit = Physics2D.Raycast(_position, Vector2.down, distance, GroundMask);
                if (hit.collider != null)
                {
                    next.y = hit.point.y + IconHalfSize;
                    landed = true;
                }
            }

            _position = next;

            // 바닥을 못 찾고 계속 떨어지면(낭떠러지 등) 그 자리에서 사라진다.
            if (!landed && _airTime >= MaxAirTime)
            {
                _velocity = Vector2.zero;
                _landedTime = Time.time;
            }
            else if (landed)
            {
                _airTime = 0f;

                if (_bounces >= MaxBounces)
                {
                    _velocity = Vector2.zero;
                    _landedTime = Time.time;
                }
                else
                {
                    // 튈 때마다 높이와 옆으로 가는 속도가 줄어든다.
                    _velocity.y = UpwardSpeed * Mathf.Pow(BounceDamping, _bounces + 1);
                    _velocity.x *= 0.65f;
                    _bounces++;
                }
            }

            transform.position = _position;
        }
    }
}
