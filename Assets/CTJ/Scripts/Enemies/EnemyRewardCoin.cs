using System.Collections.Generic;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent;
using Lrw.Script.Agent.Players;
using Lrw.Script.Currency;
using UnityEngine;

namespace CTJ.Enemies
{
    // 적과 분리된 연출 객체. 분산 낙하 → 큰 바운스 → 플레이어 중심 추적 → 소멸/지급.
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
        [Tooltip("낭떠러지 등에서 바닥을 못 찾으면 이 시간 뒤 플레이어 추적으로 넘어갑니다.")]
        [SerializeField, Min(0.1f)] private float maxAirTime = 3f;

        [Header("Ground Bounce")]
        [Tooltip("플레이어 몸체 높이에 대한 바운스 높이 비율. 최소 절반 이상 튀어 오릅니다.")]
        [SerializeField, Min(0.5f)] private float bounceHeightRatio = 0.65f;
        [SerializeField, Min(0.1f)] private float minimumBounceHeight = 0.8f;
        [Tooltip("플레이어가 아직 없거나 몸체 크기를 알 수 없을 때 사용할 키(월드 유닛).")]
        [SerializeField, Min(0.1f)] private float fallbackPlayerHeight = 2f;
        [SerializeField, Range(0f, 1f)] private float horizontalRetention = 0.65f;

        [Header("Magnet")]
        [SerializeField, Min(0.1f)] private float initialHomingSpeed = 2.5f;
        [SerializeField, Min(0.1f)] private float maxHomingSpeed = 16f;
        [SerializeField, Min(0.1f)] private float homingAcceleration = 35f;
        [SerializeField, Min(0.01f)] private float collectDistance = 0.12f;
        [Tooltip("플레이어가 없으면 재탐색하며 기다립니다. 끝까지 없을 때만 현재 위치에서 소멸/정산합니다.")]
        [SerializeField, Min(0.1f)] private float targetWaitTimeout = 5f;

        [Header("Sprite Animation")]
        [SerializeField] private Sprite[] spinFrames;
        [SerializeField, Min(1f)] private float spinFramesPerSecond = 12f;
        [SerializeField] private Sprite[] collectFrames;
        [SerializeField, Min(1f)] private float collectFramesPerSecond = 12f;

        [Header("Collect Scale Pulse")]
        [SerializeField] private bool enableCollectScalePulse = true;
        [Tooltip("도착 직후 원래 크기 대비 최대 배율입니다.")]
        [SerializeField, Min(1f)] private float collectScalePeak = 1.8f;
        [SerializeField, Range(0.1f, 1f)] private float collectScaleDip = 0.35f;
        [Tooltip("확대 → 축소 → 원래 크기까지 걸리는 시간. 소멸 애니메이션 안에서 재생합니다.")]
        [SerializeField, Min(0.01f)] private float collectScaleDuration = 0.26f;

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
        private enum CoinPhase { Scatter, Bounce, Homing, Collect }
        private CoinPhase _phase;
        private Transform _collector;
        private Collider2D _collectorBody;
        private SpriteRenderer _collectorSprite;
        private float _nextTargetSearch;
        private float _homingSpeed;
        private float _missingTargetTime;
        private Vector3 _originalScale;

        public int Amount { get; private set; }
        public int BounceCount { get; private set; }
        public bool IsCollecting => _phase == CoinPhase.Collect;
        public bool IsHoming => _phase == CoinPhase.Homing;

        internal void SetCollector(Transform collector)
        {
            PlayerController player = collector != null ? collector.GetComponentInParent<PlayerController>() : null;
            if (player != null) collector = player.transform;
            _collector = collector;
            _collectorBody = collector != null ? collector.GetComponent<Collider2D>() : null;
            _collectorSprite = collector != null ? collector.GetComponentInChildren<SpriteRenderer>() : null;
            if (_collectorBody == null && collector != null)
                foreach (Collider2D candidate in collector.GetComponentsInChildren<Collider2D>())
                    if (!candidate.isTrigger) { _collectorBody = candidate; break; }
        }

        private bool TryGetCollectorBounds(out Vector2 center, out float height)
        {
            if ((_collector == null || !_collector.gameObject.activeInHierarchy) && Time.time >= _nextTargetSearch)
            {
                _nextTargetSearch = Time.time + 0.2f;
                PlayerController player = ServiceLocator.Get<IPlayerController>() as PlayerController;
                if (player == null || !player.gameObject.activeInHierarchy) player = FindFirstObjectByType<PlayerController>();
                SetCollector(player != null ? player.transform : null);
            }
            center = transform.position;
            height = fallbackPlayerHeight;
            if (_collector == null || !_collector.gameObject.activeInHierarchy) return false;
            center = _collector.position;
            if (_collectorBody != null && _collectorBody.enabled && _collectorBody.gameObject.activeInHierarchy)
            {
                center = _collectorBody.bounds.center;
                height = _collectorBody.bounds.size.y;
            }
            else if (_collectorSprite != null && _collectorSprite.enabled && _collectorSprite.gameObject.activeInHierarchy)
            {
                center = _collectorSprite.bounds.center;
                height = _collectorSprite.bounds.size.y;
            }
            if (height <= 0f) height = fallbackPlayerHeight;
            return true;
        }

        internal void Launch(CurrencyData currency, int amount, float spread)
        {
            if (_launched) return;
            _launched = true;
            _currency = currency;
            Amount = amount;
            _visual = GetComponent<SpriteRenderer>();
            _originalScale = transform.localScale;
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
            if (IsHoming) { TickHoming(dt); return; }
            _airTime += dt;
            Vector2 gravity = Physics2D.gravity * gravityScale;
            Vector2 delta = _velocity * dt + gravity * (0.5f * dt * dt);
            _velocity += gravity * dt;
            float distance = delta.magnitude;
            if (distance > 0f && FindTerrain(delta / distance, distance + Skin, out RaycastHit2D hit))
            {
                // 이동 방향이 아닌 표면 법선 방향으로 틈을 둡니다. 비스듬히 부딪혀도
                // 다음 프레임의 수직 이동이 벽에 다시 걸려 공중에 붙지 않게 합니다.
                Vector2 contactPosition = hit.centroid + hit.normal * Skin;
                transform.position = new Vector3(contactPosition.x, contactPosition.y, transform.position.z);
                if (_velocity.y <= 0f && hit.normal.y >= 0.5f)
                {
                    HandleGroundContact();
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

            // 꼭대기까지 먼저 올라간 다음에만 자석 이동을 시작합니다.
            if ((_phase == CoinPhase.Bounce && _velocity.y <= 0f) || _airTime >= maxAirTime) BeginHoming();
        }

        private bool FindTerrain(Vector2 direction, float distance, out RaycastHit2D nearest)
        {
            nearest = default;
            Physics2D.CircleCast(transform.position, collisionRadius, direction, _terrainFilter, _hits, distance);
            float closest = float.PositiveInfinity;
            foreach (RaycastHit2D hit in _hits)
            {
                if (hit.collider == null || hit.distance >= closest || hit.collider.GetComponentInParent<Agent>() != null) continue;
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

        private void HandleGroundContact()
        {
            if (BounceCount > 0)
            {
                BeginHoming();
                return;
            }
            TryGetCollectorBounds(out _, out float playerHeight);
            float height = Mathf.Max(minimumBounceHeight, playerHeight * Mathf.Max(0.5f, bounceHeightRatio));
            float gravity = Mathf.Max(0.01f, -Physics2D.gravity.y * gravityScale);
            _velocity = new Vector2(_velocity.x * horizontalRetention, Mathf.Sqrt(2f * gravity * height));
            BounceCount++;
            _phase = CoinPhase.Bounce;
            _airTime = 0f;
        }

        private void BeginHoming()
        {
            _phase = CoinPhase.Homing;
            _velocity = Vector2.zero;
            _homingSpeed = Mathf.Max(0.1f, initialHomingSpeed);
            _missingTargetTime = 0f;
        }

        private void TickHoming(float dt)
        {
            if (!TryGetCollectorBounds(out Vector2 center, out _))
            {
                _missingTargetTime += dt;
                if (_missingTargetTime >= targetWaitTimeout) BeginCollect();
                return;
            }
            _missingTargetTime = 0f;
            _homingSpeed = Mathf.MoveTowards(_homingSpeed, Mathf.Max(initialHomingSpeed, maxHomingSpeed), homingAcceleration * dt);
            float distance = Vector2.Distance(transform.position, center);
            if (distance <= Mathf.Max(collectDistance, _homingSpeed * dt))
            {
                SetPosition(center);
                BeginCollect();
                return;
            }
            // 보상 회수 단계는 지형에 막히지 않습니다. 움직이는 플레이어 중심을 매번 다시 읽습니다.
            SetPosition(Vector2.MoveTowards(transform.position, center, _homingSpeed * dt));
        }

        private void SetPosition(Vector2 position) => transform.position = new Vector3(position.x, position.y, transform.position.z);

        private void BeginCollect()
        {
            _phase = CoinPhase.Collect;
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

            if (TryGetCollectorBounds(out Vector2 center, out _)) SetPosition(center);
            UpdateCollectScale();

            int frame = Mathf.FloorToInt(_animationTime * Mathf.Max(1f, collectFramesPerSecond));
            if (frame >= collectFrames.Length) CompleteReward();
            else _visual.sprite = collectFrames[frame];
        }

        private void UpdateCollectScale()
        {
            float t = Mathf.Clamp01(_animationTime / Mathf.Max(0.01f, collectScaleDuration));
            float scale = 1f;
            if (enableCollectScalePulse && t < 1f)
            {
                if (t < 0.3f) scale = Mathf.Lerp(1f, collectScalePeak, Mathf.SmoothStep(0f, 1f, t / 0.3f));
                else if (t < 0.75f) scale = Mathf.Lerp(collectScalePeak, collectScaleDip, Mathf.SmoothStep(0f, 1f, (t - 0.3f) / 0.45f));
                else scale = Mathf.Lerp(collectScaleDip, 1f, Mathf.SmoothStep(0f, 1f, (t - 0.75f) / 0.25f));
            }
            // 원래 배율에서 계산하므로 프레임마다 크기가 누적되지 않습니다.
            transform.localScale = _originalScale * scale;
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
