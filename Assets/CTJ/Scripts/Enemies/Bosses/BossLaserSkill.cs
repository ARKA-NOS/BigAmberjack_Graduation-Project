using System.Collections.Generic;
using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 경고와 빔 판정이 같은 고정 위험 구역을 사용합니다. 회전 빔은 이동 중간 구간도 검사합니다.
    public sealed class BossLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private SpriteRenderer beamVisual;
        [SerializeField] private StatData damageStat;
        [SerializeField, Min(0.05f)] private float beamWidth = 0.65f;
        [Tooltip("바닥을 훑는 진행을 보스보다 늦춰 빔이 비스듬하게 회전하게 합니다. 1이면 선형 진행입니다.")]
        [SerializeField, Range(1f, 3f)] private float sweepLag = 2f;
        [SerializeField] private BossLaserShake cameraShake;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private bool _hitTargetThisTraversal;
        private Stat _damage;
        private Bounds _dangerBounds;
        private Vector2 _traversalStart;
        private Vector2 _traversalEnd;
        private Vector2 _previousOrigin;
        private float _previousProgress;
        private bool _configured;
        public bool IsFiring => IsUsing && HasReleased;
        public bool HasHitTargetThisTraversal => _hitTargetThisTraversal;
        public Bounds DangerBounds => _dangerBounds;
        public Bounds BeamBounds => CurrentBeam.Bounds;
        public Vector2 BeamDirection => CurrentBeam.Direction;
        public Vector2 BeamCenter => CurrentBeam.Center;
        public Vector2 BeamSize => CurrentBeam.Size;
        public float BeamAngle => CurrentBeam.Angle;
        private Vector2 Origin => firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
        private BeamPose CurrentBeam => MakeBeam(Origin, Progress(Origin));

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            if (damageStat != null && owner.TryGetModule(out IStatModule stats)) _damage = stats.GetStat(damageStat);
            if (beamVisual != null) beamVisual.enabled = false;
            BossArenaModule arena = owner.GetModule<BossArenaModule>();
            if (arena != null) ConfigureTraversal(arena.GetLaserDangerBounds(0), arena.GetLaserStart(0), arena.GetLaserEnd(0));
        }

        public void ConfigureTraversal(Bounds danger, Vector2 start, Vector2 end)
        {
            _dangerBounds = danger;
            _traversalStart = start;
            _traversalEnd = end;
            _configured = danger.size.x > 0f && danger.size.y > 0f;
        }

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && _configured && firePoint != null && beamVisual != null;

        protected override void PrepareAttack()
        {
            _hitTargetThisTraversal = false;
            beamVisual.enabled = false;
            cameraShake?.StopShake();
        }

        protected override void UpdateTelegraph() => Telegraph.SetBounds(_dangerBounds);

        protected override void OnReleaseAttack()
        {
            // 발사 중에도 영역 표시를 유지해 교대한 안전 구역을 알 수 있도록 합니다.
            Telegraph?.EndBlink();
            _previousOrigin = Origin;
            _previousProgress = Progress(Origin);
            cameraShake?.StartShake(_dangerBounds);
            TickBeam();
        }

        private float Progress(Vector2 origin)
        {
            Vector2 path = _traversalEnd - _traversalStart;
            return path.sqrMagnitude > 0.0001f ? Mathf.Clamp01(Vector2.Dot(origin - _traversalStart, path) / path.sqrMagnitude) : 1f;
        }

        private BeamPose MakeBeam(Vector2 origin, float progress)
        {
            float width = Mathf.Max(0.05f, beamWidth);
            float inset = Mathf.Min(width * 0.5f, _dangerBounds.size.x * 0.25f);
            float left = _dangerBounds.min.x + inset;
            float right = _dangerBounds.max.x - inset;
            float t = Mathf.Pow(Mathf.Clamp01(progress), Mathf.Clamp(sweepLag, 1f, 3f));
            float aimX = _traversalEnd.x >= _traversalStart.x ? Mathf.Lerp(left, right, t) : Mathf.Lerp(right, left, t);
            Vector2 direction = (new Vector2(aimX, _dangerBounds.min.y) - origin).normalized;
            if (direction.sqrMagnitude < 0.001f) direction = Vector2.down;
            Vector2 normal = new Vector2(-direction.y, direction.x);
            // 빔의 두께까지 위험 구역 안에 들어오도록 중심선이 사용할 영역을 안쪽으로 줄입니다.
            Vector2 padding = new Vector2(Mathf.Abs(normal.x), Mathf.Abs(normal.y)) * (width * 0.5f);
            Vector2 min = (Vector2)_dangerBounds.min + padding;
            Vector2 max = (Vector2)_dangerBounds.max - padding;
            float near = 0f;
            float far = float.PositiveInfinity;
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
            float progress = Progress(origin);
            BeamPose pose = MakeBeam(origin, progress);
            UpdateVisual(pose);
            if (!_hitTargetThisTraversal)
            {
                // AABB 전체에 피해를 주지 않습니다. 중간 위치·각도의 실제 회전 사각형을 촘촘히 검사합니다.
                float travel = Vector2.Distance(_previousOrigin, origin) +
                    _dangerBounds.size.magnitude * Mathf.Abs(progress - _previousProgress) * Mathf.Max(1f, sweepLag);
                int steps = Mathf.Max(1, Mathf.CeilToInt(travel / (Mathf.Max(0.05f, beamWidth) * 0.25f)));
                Physics2D.SyncTransforms();
                for (int i = 0; i <= steps && !_hitTargetThisTraversal; i++)
                {
                    float t = (float)i / steps;
                    CheckDamage(MakeBeam(Vector2.Lerp(_previousOrigin, origin, t), Mathf.Lerp(_previousProgress, progress, t)));
                }
            }
            _previousOrigin = origin;
            _previousProgress = progress;
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
