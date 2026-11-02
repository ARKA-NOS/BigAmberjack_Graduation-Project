using System.Collections.Generic;
using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 발사 순서·피해·경고를 관리하고 원본 그림의 배치와 윤곽은 BossSpriteBeam에 맡깁니다.
    public sealed class BossLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private BossSpriteBeam spriteBeam;
        [SerializeField] private StatData damageStat;
        [SerializeField] private BossLaserShake cameraShake;
        [Header("Wall Safe Space")]
        [Tooltip("플레이어 한 캐릭터의 기준 월드 폭입니다. 현재 공용 Player의 몸통 Collider 폭은 0.5입니다.")]
        [SerializeField, Min(0.01f)] private float characterWidth = 0.5f;
        [Tooltip("출발한 벽에서 비워 둘 캐릭터 수입니다. 넓은 레이저 끝까지 이 공간을 벗어나야 발사합니다.")]
        [SerializeField, Min(0f)] private float safeCharacterCount = 2.5f;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private readonly List<Vector2> _dangerPolygon = new List<Vector2>();
        private readonly List<Vector2> _sweptHull = new List<Vector2>();
        private readonly List<Vector2> _clipBuffer = new List<Vector2>();
        private Stat _damage;
        private Bounds _dangerBounds;
        private Vector2 _previousOrigin;
        private bool _hitTargetThisTraversal;
        private bool _configured;
        private bool _travelsRight;
        private bool _beamStarted;
        private float _fireStartX;
        private float _safeBoundaryX;
        private float _emissionStartX, _traversalEndX;
        public bool IsTraversalReady => IsUsing && HasReleased;
        public bool IsFiring => IsTraversalReady && _beamStarted;
        public float SafeZoneWidth => Mathf.Max(0.01f, characterWidth) * Mathf.Max(0f, safeCharacterCount);
        public float FireStartX => _fireStartX;
        public Bounds SafeZoneBounds { get; private set; }
        public bool HasHitTargetThisTraversal => _hitTargetThisTraversal;
        public Bounds DangerBounds => _dangerBounds;
        public IReadOnlyList<Vector2> DangerPolygon => _dangerPolygon;
        public Bounds BeamBounds => spriteBeam != null ? spriteBeam.Bounds : default;
        public float VisualBeamLength => spriteBeam != null ? spriteBeam.WorldSize.y : 0f;
        public Vector2 BeamDirection => spriteBeam != null ? spriteBeam.Direction : Vector2.down;
        public Vector2 BeamCenter => BeamBounds.center;
        public Vector2 BeamSize => spriteBeam != null ? spriteBeam.WorldSize : Vector2.zero;
        public float BeamAngle => 0f;
        private Vector2 Origin => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            if (damageStat != null && owner.TryGetModule(out IStatModule stats)) _damage = stats.GetStat(damageStat);
            spriteBeam?.Hide();
            BossArenaModule arena = owner.GetModule<BossArenaModule>();
            if (arena != null)
            {
                MatchArenaHeight(arena);
                ConfigureTraversal(arena.LaserBounds, arena.GetLaserStart(0), arena.GetLaserEnd(0));
            }
        }

        public void MatchArenaHeight(BossArenaModule arena)
        {
            if (spriteBeam == null || !spriteBeam.Configure(true)) return;
            float offsetY = Enemy != null ? Origin.y - Enemy.transform.position.y : 0f;
            // 그림을 늘리지 않고, 그림의 발사구가 눈 아래에 오면서 바닥에 닿도록 보스 경로 높이를 정합니다.
            arena.SetLaserHeightFromFloor(spriteBeam.HeightFromFloor - offsetY);
        }

        public void ConfigureTraversal(Bounds danger, Vector2 start, Vector2 end)
        {
            _dangerBounds = danger;
            _travelsRight = end.x >= start.x;
            _configured = spriteBeam != null && spriteBeam.Configure(_travelsRight) && danger.size.x > 0f && danger.size.y > 0f;
            _dangerPolygon.Clear();
            if (!_configured) return;
            Vector2 muzzleOffset = Enemy != null ? Origin - (Vector2)Enemy.transform.position : Vector2.zero;
            start += muzzleOffset;
            end += muzzleOffset;
            _traversalEndX = end.x;
            float safeWidth = Mathf.Min(SafeZoneWidth, danger.size.x);
            _safeBoundaryX = _travelsRight ? danger.min.x + safeWidth : danger.max.x - safeWidth;
            SafeZoneBounds = new Bounds(new Vector3(_travelsRight ? danger.min.x + safeWidth * 0.5f : danger.max.x - safeWidth * 0.5f,
                danger.center.y, danger.center.z), new Vector3(safeWidth, danger.size.y, danger.size.z));
            // 보스 중심만 기준으로 삼으면 뒤로 퍼진 바닥 부분이 안전 공간을 침범합니다. 그림 전체 경계를 사용합니다.
            Bounds firstBeam = spriteBeam.WorldBoundsAt(start);
            _fireStartX = _travelsRight ? start.x + Mathf.Max(0f, _safeBoundaryX - firstBeam.min.x)
                : start.x - Mathf.Max(0f, firstBeam.max.x - _safeBoundaryX);
            Vector2 firingStart = new Vector2(_fireStartX, start.y);
            // 방이 너무 좁으면 발사 없이 횡단만 끝냅니다. 안전 공간을 줄여 억지로 공격하지 않습니다.
            if (safeWidth >= danger.size.x || (_travelsRight ? _fireStartX >= end.x : _fireStartX <= end.x)) return;
            spriteBeam.GetSweptHull(firingStart, end, _sweptHull);
            Vector2 min = danger.min, max = danger.max;
            _dangerPolygon.Add(min);
            _dangerPolygon.Add(new Vector2(min.x, max.y));
            _dangerPolygon.Add(max);
            _dangerPolygon.Add(new Vector2(max.x, min.y));
            for (int i = 0; i < _sweptHull.Count && _dangerPolygon.Count > 0; i++)
            {
                Vector2 edge = _sweptHull[(i + 1) % _sweptHull.Count] - _sweptHull[i];
                ClipWarning(_sweptHull[i], new Vector2(-edge.y, edge.x));
            }
            if (_dangerPolygon.Count > 0)
                ClipWarning(new Vector2(_safeBoundaryX, 0f), _travelsRight ? Vector2.right : Vector2.left);
        }

        private void ClipWarning(Vector2 edgePoint, Vector2 inwardNormal)
        {
            _clipBuffer.Clear();
            Vector2 previous = _dangerPolygon[_dangerPolygon.Count - 1];
            float previousSide = Vector2.Dot(previous - edgePoint, inwardNormal);
            foreach (Vector2 current in _dangerPolygon)
            {
                float side = Vector2.Dot(current - edgePoint, inwardNormal);
                if ((side >= 0f) != (previousSide >= 0f))
                    _clipBuffer.Add(Vector2.Lerp(previous, current, previousSide / (previousSide - side)));
                if (side >= 0f) _clipBuffer.Add(current);
                previous = current;
                previousSide = side;
            }
            _dangerPolygon.Clear();
            _dangerPolygon.AddRange(_clipBuffer);
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && _configured && firePoint != null && spriteBeam != null;
        protected override void PrepareAttack()
        {
            _hitTargetThisTraversal = false;
            _beamStarted = false;
            spriteBeam.Hide();
            cameraShake?.StopShake();
        }
        protected override void UpdateTelegraph() => Telegraph.SetPolygon(_dangerBounds, _dangerPolygon);
        protected override void OnReleaseAttack()
        {
            Telegraph?.EndBlink();
            _previousOrigin = Origin;
            TickBeam();
        }

        public void TickBeam()
        {
            if (!IsTraversalReady || !CanContinue) return;
            Vector2 origin = Origin;
            bool beyondSafeSpace = _travelsRight ? origin.x >= _fireStartX : origin.x <= _fireStartX;
            if (!beyondSafeSpace || _dangerPolygon.Count == 0)
            {
                spriteBeam.Hide();
                if (_beamStarted) cameraShake?.StopShake();
                _beamStarted = false;
                _previousOrigin = origin;
                return;
            }
            if (!_beamStarted)
            {
                _beamStarted = true;
                _emissionStartX = origin.x;
                // 첫 발사에서 안전 공간을 지나온 경로를 소급 검사하지 않습니다.
                _previousOrigin = origin;
                cameraShake?.StartShake(_dangerBounds);
            }
            spriteBeam.Show(origin);
            cameraShake?.SetTraversalProgress(Mathf.InverseLerp(_emissionStartX, _traversalEndX, origin.x));
            // 충분히 보이기 전에는 피해를 주지 않고, 페이드 중 지나온 경로도 소급해서 때리지 않습니다.
            if (!spriteBeam.CanDamage) { _previousOrigin = origin; return; }
            if (!_hitTargetThisTraversal)
            {
                float step = Mathf.Max(0.05f, spriteBeam.WorldSize.x * 0.1f);
                int steps = Mathf.Max(1, Mathf.CeilToInt(Vector2.Distance(_previousOrigin, origin) / step));
                for (int i = 0; i <= steps && !_hitTargetThisTraversal; i++)
                {
                    spriteBeam.OverlapAt(Vector2.Lerp(_previousOrigin, origin, (float)i / steps), _overlaps);
                    CheckDamage();
                }
                spriteBeam.Place(origin);
            }
            _previousOrigin = origin;
        }

        private void CheckDamage()
        {
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget) || hit.GetComponentInParent<IDamageable>() == null) continue;
                Vector2 point = hit.ClosestPoint(_dangerBounds.center);
                if (point.x < _dangerBounds.min.x || point.x > _dangerBounds.max.x ||
                    point.y < _dangerBounds.min.y || point.y > _dangerBounds.max.y) continue;
                if (_travelsRight ? hit.bounds.max.x <= _safeBoundaryX : hit.bounds.min.x >= _safeBoundaryX) continue;
                _hitTargetThisTraversal = true;
                EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, Mathf.Max(0f, _damage?.Value ?? 30f),
                    hit.ClosestPoint(BeamCenter), BeamDirection);
                break;
            }
        }

        public void FinishTraversal() => EndSkill();
        protected override void OnEndSkill() { ClearBeam(true); base.OnEndSkill(); }
        protected override void OnStopSkill() { ClearBeam(false); base.OnStopSkill(); }
        private void ClearBeam(bool fade)
        {
            if (fade) { spriteBeam?.FadeOut(); cameraShake?.FadeOut(); }
            else { spriteBeam?.Hide(); cameraShake?.StopShake(); }
            _hitTargetThisTraversal = false;
            _beamStarted = false;
        }
    }
}
