using System.Collections.Generic;
using Lrw.Script.Currency;
using UnityEngine;

namespace CTJ.Enemies
{
    // 적과 분리된 연출 객체. 플레이어 접촉/아이템 습득 시스템 없이 연출 종료 시 지급합니다.
    [DisallowMultipleComponent, RequireComponent(typeof(SpriteRenderer))]
    public sealed class EnemyRewardCoin : MonoBehaviour
    {
        [Header("Scatter (World Units)")]
        [SerializeField, Min(0f)] private float horizontalSpeed = 1.8f;
        [SerializeField] private Vector2 upwardSpeedRange = new Vector2(2.8f, 4.2f);
        [SerializeField, Min(0.01f)] private float gravityScale = 1.5f;
        [Tooltip("바닥/벽 레이어만 선택하세요. 기본값은 Ground입니다. 트리거는 무시합니다.")]
        [SerializeField] private LayerMask terrainLayers = 1 << 6;
        [SerializeField, Min(0.01f)] private float collisionRadius = 0.1f;
        [Tooltip("낭떠러지 등에서 바닥을 못 찾으면 이 시간 뒤 소멸/지급으로 넘어갑니다. 바운스 시 다시 셉니다.")]
        [SerializeField, Min(0.1f)] private float maxAirTime = 3f;

        [Header("Ground Bounce")]
        [SerializeField, Min(0)] private int maxBounces = 2;
        [Tooltip("첫 바운스의 초기 속도입니다. 이후에는 Bounce Damping만큼 약해집니다.")]
        [SerializeField, Min(0f)] private float bounceSpeed = 2.6f;
        [SerializeField, Range(0f, 1f)] private float bounceDamping = 0.55f;
        [SerializeField, Range(0f, 1f)] private float horizontalRetention = 0.65f;

        [Header("Sprite Animation")]
        [SerializeField] private Sprite[] spinFrames;
        [SerializeField, Min(1f)] private float spinFramesPerSecond = 12f;
        [SerializeField] private Sprite[] collectFrames;
        [SerializeField, Min(1f)] private float collectFramesPerSecond = 12f;

        private const float Skin = 0.005f;
        private readonly List<RaycastHit2D> _hits = new List<RaycastHit2D>(8);
        private SpriteRenderer _visual;
        private ContactFilter2D _terrainFilter;
        private CurrencyData _currency;
        private Vector2 _velocity;
        private float _airTime;
        private float _animationTime;
        private bool _launched;
        private bool _completed;
        private bool _quitting;

        public int Amount { get; private set; }
        public int BounceCount { get; private set; }
        public bool IsCollecting { get; private set; }

        internal void Launch(CurrencyData currency, int amount, float spread)
        {
            if (_launched) return;
            _launched = true;
            _currency = currency;
            Amount = amount;
            _visual = GetComponent<SpriteRenderer>();
            _terrainFilter = new ContactFilter2D { useTriggers = false };
            _terrainFilter.SetLayerMask(terrainLayers);
            float low = Mathf.Max(0f, Mathf.Min(upwardSpeedRange.x, upwardSpeedRange.y));
            float high = Mathf.Max(low, Mathf.Max(upwardSpeedRange.x, upwardSpeedRange.y));
            _velocity = new Vector2(Mathf.Lerp(-horizontalSpeed, horizontalSpeed, spread), Random.Range(low, high));
            if (spinFrames != null && spinFrames.Length > 0)
            {
                // 같은 순간에 생성된 코인도 회전 자세가 조금씩 다르게 시작합니다.
                _animationTime = Random.Range(0, spinFrames.Length) / Mathf.Max(1f, spinFramesPerSecond);
                UpdateSpinSprite();
            }
        }

        private void FixedUpdate()
        {
            if (!_launched || _completed || IsCollecting) return;
            float dt = Time.fixedDeltaTime;
            _airTime += dt;
            _velocity += Physics2D.gravity * gravityScale * dt;
            Vector2 delta = _velocity * dt;
            float distance = delta.magnitude;
            if (distance > 0f && FindTerrain(delta / distance, distance + Skin, out RaycastHit2D hit))
            {
                // 이동 방향이 아닌 표면 법선 방향으로 틈을 둡니다. 비스듬히 부딪혀도
                // 다음 프레임의 수직 이동이 벽에 다시 걸려 공중에 붙지 않게 합니다.
                Vector2 contactPosition = hit.centroid + hit.normal * Skin;
                transform.position = new Vector3(contactPosition.x, contactPosition.y, transform.position.z);
                if (_velocity.y <= 0f && hit.normal.y >= 0.5f)
                {
                    HandleGroundContact(hit.normal);
                    return;
                }

                // 벽/천장에서는 안쪽 속도만 제거해 바닥까지 떨어뜨립니다.
                float inwardSpeed = Vector2.Dot(_velocity, hit.normal);
                if (inwardSpeed < 0f) _velocity -= hit.normal * inwardSpeed;
            }
            else
            {
                transform.position += (Vector3)delta;
            }

            if (_airTime >= maxAirTime) BeginCollect();
        }

        private bool FindTerrain(Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            nearest = default;
            Physics2D.CircleCast(transform.position, collisionRadius, direction, _terrainFilter, _hits, distance);
            float closest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in _hits)
            {
                if (hit.collider == null || hit.distance >= closest) continue;
                if (Vector2.Dot(direction, hit.normal) >= -0.0001f) continue;
                // 일방향 발판은 아래/옆에서 통과하고, 내려올 때 윗면에서만 착지합니다.
                if (hit.collider.usedByEffector && hit.collider.TryGetComponent(out PlatformEffector2D platform) &&
                    platform.useOneWay && (_velocity.y > 0f || hit.normal.y < 0.5f ||
                    transform.position.y < hit.point.y)) continue;
                closest = hit.distance;
                nearest = hit;
            }
            return nearest.collider != null;
        }

        private void HandleGroundContact(Vector2 normal)
        {
            if (BounceCount >= maxBounces || bounceSpeed <= 0f)
            {
                BeginCollect();
                return;
            }

            float speed = bounceSpeed * Mathf.Pow(bounceDamping, BounceCount);
            Vector2 tangentVelocity = _velocity - normal * Vector2.Dot(_velocity, normal);
            _velocity = tangentVelocity * horizontalRetention + normal * speed;
            BounceCount++;
            _airTime = 0f;
            // FixedUpdate의 중력이 계속 작용하므로 다시 내려와 바닥에 닿습니다.
        }

        private void BeginCollect()
        {
            IsCollecting = true;
            _velocity = Vector2.zero;
            _animationTime = 0f;
            if (collectFrames == null || collectFrames.Length == 0)
            {
                Debug.LogWarning("코인의 Collect Frames가 비어 있어 소멸 연출 없이 정산합니다.", this);
                CompleteReward();
                return;
            }
            _visual.sprite = collectFrames[0];
        }

        private void Update()
        {
            if (!_launched || _completed) return;
            _animationTime += Time.deltaTime;
            if (!IsCollecting)
            {
                UpdateSpinSprite();
                return;
            }

            int frame = Mathf.FloorToInt(_animationTime * Mathf.Max(1f, collectFramesPerSecond));
            if (frame >= collectFrames.Length) CompleteReward();
            else _visual.sprite = collectFrames[frame];
        }

        private void UpdateSpinSprite()
        {
            if (spinFrames == null || spinFrames.Length == 0) return;
            int frame = Mathf.FloorToInt(_animationTime * Mathf.Max(1f, spinFramesPerSecond)) % spinFrames.Length;
            _visual.sprite = spinFrames[frame];
        }

        private void CompleteReward()
        {
            if (_completed) return;
            _visual.enabled = false;
            Destroy(gameObject);
            PayReward();
        }

        private void OnApplicationQuit() => _quitting = true;

        private void OnDestroy()
        {
            // 룸/씬 정리로 연출이 먼저 제거되어도 이미 획득한 처치 보상은 잃지 않습니다.
            // 게임 종료/에디터 Play 종료 때는 별도 정산하지 않습니다.
            if (_launched && !_quitting && Application.isPlaying) PayReward();
        }

        private void PayReward()
        {
            if (_completed) return;
            _completed = true; // Add의 변경 이벤트에서 재진입해도 한 번만 지급합니다.
            if (_currency == null || Amount <= 0) return;
            // 연출 중 다른 보상도 들어올 수 있으므로 지급 시점에 다시 한도를 검사합니다.
            if ((long)_currency.Count + Amount > int.MaxValue)
            {
                Debug.LogWarning("코인 보상이 재화 보유량 한도를 초과하여 지급하지 않았습니다.", this);
                return;
            }
            _currency.Add(Amount);
        }
    }
}
