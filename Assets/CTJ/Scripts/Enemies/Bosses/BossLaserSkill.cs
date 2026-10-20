using System.Collections.Generic;
using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace CTJ.Enemies.Bosses
{
    // 투사체가 아닙니다. 준비 후 켜진 빔의 현재 면적과 직전 물리 틱 사이의 이동 면적을 검사합니다.
    public sealed class BossLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform firePoint;
        [SerializeField] private SpriteRenderer beamVisual;
        [SerializeField] private StatData damageStat;
        [SerializeField, Min(0.01f)] private float beamWidth = 0.65f;
        private readonly List<Collider2D> _overlaps = new List<Collider2D>();
        private bool _hitTargetThisTraversal;
        private Stat _damage;
        private float _floorY;
        private Bounds _previousBounds;
        public bool IsFiring => IsUsing && HasReleased;
        public bool HasHitTargetThisTraversal => _hitTargetThisTraversal;
        public Bounds BeamBounds => MakeBounds();

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            if (damageStat != null && owner.TryGetModule(out IStatModule stats)) _damage = stats.GetStat(damageStat);
            if (beamVisual != null) beamVisual.enabled = false;
        }

        public void ConfigureTraversal(float floorY) => _floorY = floorY;

        protected override bool CanUseSkillCondition() => base.CanUseSkillCondition() && firePoint != null && beamVisual != null;

        protected override void PrepareAttack()
        {
            _hitTargetThisTraversal = false; // 1회 사용 = 편도 1회. 같은 횡단 안에서는 퇴장/재진입해도 다시 맞지 않습니다.
            beamVisual.enabled = false;
        }

        protected override void UpdateTelegraph() => Telegraph.SetBounds(MakeBounds());

        protected override void OnReleaseAttack()
        {
            Telegraph?.Hide();
            _previousBounds = MakeBounds();
            beamVisual.enabled = true;
            TickBeam();
        }

        private Bounds MakeBounds()
        {
            Vector2 origin = firePoint != null ? (Vector2)firePoint.position : (Vector2)transform.position;
            float bottom = Mathf.Min(_floorY, origin.y);
            return new Bounds(new Vector3(origin.x, (origin.y + bottom) * 0.5f, 0f),
                new Vector3(Mathf.Max(0.01f, beamWidth), Mathf.Max(0.01f, origin.y - bottom), 0f));
        }

        public void TickBeam()
        {
            if (!IsFiring || !CanContinue) return;
            Bounds bounds = MakeBounds();
            beamVisual.transform.SetPositionAndRotation(bounds.center, Quaternion.identity);
            Vector3 parentScale = beamVisual.transform.parent.lossyScale;
            Vector2 spriteSize = beamVisual.sprite.bounds.size;
            beamVisual.transform.localScale = new Vector3(bounds.size.x / (spriteSize.x * Mathf.Abs(parentScale.x)),
                bounds.size.y / (spriteSize.y * Mathf.Abs(parentScale.y)), 1f);

            // 이 보스는 수평 직선으로만 횡단합니다. 두 사각형 사이를 포함한 사각형이 이동 판정 면적입니다.
            Bounds swept = bounds;
            swept.Encapsulate(_previousBounds);
            _previousBounds = bounds;
            if (_hitTargetThisTraversal) return;
            Physics2D.SyncTransforms();
            Physics2D.OverlapBox(swept.center, swept.size, 0f,
                new ContactFilter2D { useTriggers = true }, _overlaps);
            foreach (Collider2D hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget)) continue;
                IDamageable receiver = hit.GetComponentInParent<IDamageable>();
                if (receiver == null) continue;
                // 한 플레이어 아래 서로 다른 IDamageable이 있어도 이 타깃에는 횡단당 한 번만 전달합니다.
                _hitTargetThisTraversal = true;
                EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, Mathf.Max(0f, _damage?.Value ?? 30f),
                    hit.ClosestPoint(bounds.center), Vector2.down);
                break;
            }
        }

        public void FinishTraversal() => EndSkill();
        protected override void OnEndSkill() { ClearBeam(); base.OnEndSkill(); }
        protected override void OnStopSkill() { ClearBeam(); base.OnStopSkill(); }
        private void ClearBeam()
        {
            if (beamVisual != null) beamVisual.enabled = false;
            _hitTargetThisTraversal = false;
        }
    }
}
