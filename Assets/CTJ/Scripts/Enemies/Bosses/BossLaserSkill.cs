using System.Collections.Generic;
using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 각도는 횡단 시작 시 고정합니다. 경고는 빔 두께를 포함한 전체 횡단의 위험 다각형입니다.
    public sealed class BossLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private SpriteRenderer beamVisual;
        [SerializeField] private StatData damageStat;
        [SerializeField, Min(0.05f)] private float beamWidth = 0.65f;
        [Tooltip("월드 단위의 고정 빔 길이입니다. 바닥/벽에 닿아도 시각 길이를 줄이지 않습니다.")]
        [SerializeField, Min(0.1f)] private float beamLength = 18f;
        [Tooltip("수직 아래에서 아레나 안쪽으로 기울이는 고정 각도입니다.")]
        [SerializeField, Range(15f, 65f)] private float diagonalAngle = 45f;
        [SerializeField] private BossLaserShake cameraShake;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private bool _hitTargetThisTraversal;
        private Stat _damage;
        private Bounds _dangerBounds;
        private readonly List<Vector2> _dangerPolygon = new List<Vector2>(5);
        private readonly List<Vector2> _clipBuffer = new List<Vector2>(8);
        private Vector2 _direction;
        private float _castLength;
        private float _castWidth;
        private Vector2 _previousOrigin;
        private bool _configured;
        public bool IsFiring => IsUsing && HasReleased;
        public bool HasHitTargetThisTraversal => _hitTargetThisTraversal;
        public Bounds DangerBounds => _dangerBounds;
        public IReadOnlyList<Vector2> DangerPolygon => _dangerPolygon;
        // BeamBounds/Center/Size는 아레나 안의 피해 판정입니다. 시각 빔은 별도로 고정 길이를 유지합니다.
        public Bounds BeamBounds => CurrentBeam.Bounds;
        public float VisualBeamLength => _castLength;
        public Vector2 BeamDirection => _direction;
        public Vector2 BeamCenter => CurrentBeam.Center;
        public Vector2 BeamSize => CurrentBeam.Size;
        public float BeamAngle => CurrentBeam.Angle;
        private Vector2 Origin => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        private BeamPose CurrentBeam => MakeBeam(Origin);

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            if (damageStat != null && owner.TryGetModule(out IStatModule stats)) _damage = stats.GetStat(damageStat);
            if (beamVisual != null) beamVisual.enabled = false;
            BossArenaModule arena = owner.GetModule<BossArenaModule>();
            if (arena != null) ConfigureTraversal(arena.LaserBounds, arena.GetLaserStart(0), arena.GetLaserEnd(0));
        }

        public void ConfigureTraversal(Bounds danger, Vector2 start, Vector2 end)
        {
            _dangerBounds = danger;
            float angle = Mathf.Clamp(diagonalAngle, 15f, 65f) * Mathf.Deg2Rad;
            float sign = end.x >= start.x ? 1f : -1f;
            _direction = new Vector2(sign * Mathf.Sin(angle), -Mathf.Cos(angle));
            _castLength = Mathf.Max(0.1f, beamLength);
            _castWidth = Mathf.Max(0.05f, beamWidth);
            BuildDangerPolygon(start, end, new Vector2(sign * Mathf.Cos(angle), Mathf.Sin(angle)), sign);
            _configured = danger.size.x > 0f && danger.size.y > 0f;
        }

        private void BuildDangerPolygon(Vector2 start, Vector2 end, Vector2 normal, float sign)
        {
            _dangerPolygon.Clear();
            Vector2 min = _dangerBounds.min, max = _dangerBounds.max;
            _dangerPolygon.Add(min);
            _dangerPolygon.Add(new Vector2(min.x, max.y));
            _dangerPolygon.Add(max);
            _dangerPolygon.Add(new Vector2(max.x, min.y));
            Vector2 halfWidth = normal * (_castWidth * 0.5f), tip = _direction * _castLength;
            // 유한 길이 사각 빔이 수평으로 지나가는 전체 윤곽입니다. 짧게 설정해도 경고가 바닥까지 과장되지 않습니다.
            Vector2[] outline = { start - halfWidth, start + halfWidth, end + halfWidth,
                end + tip + halfWidth, end + tip - halfWidth, start + tip - halfWidth };
            for (int i = 0; i < outline.Length && _dangerPolygon.Count > 0; i++)
            {
                Vector2 edge = outline[(i + 1) % outline.Length] - outline[i];
                ClipWarning(outline[i], new Vector2(edge.y, -edge.x) * sign);
            }
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

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && _configured && firePoint != null && beamVisual != null;

        protected override void PrepareAttack()
        {
            _hitTargetThisTraversal = false;
            beamVisual.enabled = false;
            cameraShake?.StopShake();
        }

        protected override void UpdateTelegraph() => Telegraph.SetPolygon(_dangerBounds, _dangerPolygon);

        protected override void OnReleaseAttack()
        {
            // 발사 중에도 영역 표시를 유지해 교대한 안전 구역을 알 수 있도록 합니다.
            Telegraph?.EndBlink();
            _previousOrigin = Origin;
            cameraShake?.StartShake(_dangerBounds);
            TickBeam();
        }

        private BeamPose MakeBeam(Vector2 origin)
        {
            float width = _castWidth;
            Vector2 direction = _direction;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            // 빔의 두께까지 위험 구역 안에 들어오도록 중심선이 사용할 영역을 안쪽으로 줄입니다.
            Vector2 padding = new Vector2(Mathf.Abs(normal.x), Mathf.Abs(normal.y)) * (width * 0.5f);
            Vector2 min = (Vector2)_dangerBounds.min + padding;
            Vector2 max = (Vector2)_dangerBounds.max - padding;
            float near = 0f;
            float far = _castLength;
            if (min.x > max.x || min.y > max.y ||
                !ClipAxis(origin.x, direction.x, min.x, max.x, ref near, ref far) ||
                !ClipAxis(origin.y, direction.y, min.y, max.y, ref near, ref far) || far <= near)
                return default;
            return new BeamPose(origin + direction * near, origin + direction * far, width);
        }

        private static bool ClipAxis(float origin, float direction, float min, float max, ref float near, ref float far)
        {
            if (Mathf.Abs(direction) < 0.00001f) return origin >= min && origin <= max;
            float a = (min - origin) / direction;
            float b = (max - origin) / direction;
            near = Mathf.Max(near, Mathf.Min(a, b));
            far = Mathf.Min(far, Mathf.Max(a, b));
            return near <= far;
        }

        public void TickBeam()
        {
            if (!IsFiring || !CanContinue) return;
            Vector2 origin = Origin;
            // 지형까지의 거리로 메시를 잘라내지 않습니다. 벽/바닥이 앞에서 그려져 자연스럽게 가립니다.
            UpdateVisual(new BeamPose(origin, origin + _direction * _castLength, _castWidth));
            if (!_hitTargetThisTraversal)
            {
                // 고정 각도의 빔이 지나간 중간 위치도 검사합니다. AABB 전체에는 피해를 주지 않습니다.
                float travel = Vector2.Distance(_previousOrigin, origin);
                int steps = Mathf.Max(1, Mathf.CeilToInt(travel / (_castWidth * 0.25f)));
                Physics2D.SyncTransforms();
                for (int i = 0; i <= steps && !_hitTargetThisTraversal; i++)
                {
                    float t = (float)i / steps;
                    CheckDamage(MakeBeam(Vector2.Lerp(_previousOrigin, origin, t)));
                }
            }
            _previousOrigin = origin;
        }

        private void UpdateVisual(BeamPose pose)
        {
            beamVisual.enabled = pose.IsValid;
            if (!pose.IsValid) return;
            beamVisual.transform.SetPositionAndRotation(pose.Center, Quaternion.Euler(0f, 0f, pose.Angle));
            Vector3 parentScale = beamVisual.transform.parent.lossyScale;
            Vector2 spriteSize = beamVisual.sprite.bounds.size;
            beamVisual.transform.localScale = new Vector3(pose.Size.x / (spriteSize.x * Mathf.Abs(parentScale.x)),
                pose.Size.y / (spriteSize.y * Mathf.Abs(parentScale.y)), 1f);
        }

        private void CheckDamage(BeamPose pose)
        {
            if (!pose.IsValid) return;
            Physics2D.OverlapBox(pose.Center, pose.Size, pose.Angle, new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget) || hit.GetComponentInParent<IDamageable>() == null) continue;
                _hitTargetThisTraversal = true;
                EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, Mathf.Max(0f, _damage?.Value ?? 30f),
                    hit.ClosestPoint(pose.Center), pose.Direction);
                break;
            }
        }

        private readonly struct BeamPose
        {
            public readonly Vector2 Center, Size, Direction;
            public readonly float Angle;
            public bool IsValid => Size.y > 0.0001f;
            public Bounds Bounds => new Bounds(Center, new Vector3(
                Mathf.Abs(Direction.x) * Size.y + Mathf.Abs(Direction.y) * Size.x,
                Mathf.Abs(Direction.y) * Size.y + Mathf.Abs(Direction.x) * Size.x, 0f));
            public BeamPose(Vector2 start, Vector2 end, float width)
            {
                Center = (start + end) * 0.5f;
                Direction = (end - start).normalized;
                Size = new Vector2(width, Vector2.Distance(start, end));
                Angle = Mathf.Atan2(Direction.y, Direction.x) * Mathf.Rad2Deg - 90f;
            }
        }

        public void FinishTraversal() => EndSkill();
        protected override void OnEndSkill() { ClearBeam(); base.OnEndSkill(); }
        protected override void OnStopSkill() { ClearBeam(); base.OnStopSkill(); }
        private void ClearBeam()
        {
            if (beamVisual != null) beamVisual.enabled = false;
            cameraShake?.StopShake();
            _hitTargetThisTraversal = false;
        }
    }
}
