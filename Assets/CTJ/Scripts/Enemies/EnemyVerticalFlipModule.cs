using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // 반전을 지원하는 적에만 자식으로 배치합니다. 컴포넌트 활성화가 곧 반전 설정입니다.
    [DisallowMultipleComponent]
    public sealed class EnemyVerticalFlipModule : Module, IAfterInitModule
    {
        private Transform _root;
        private EnemySkillModule _skills;
        private float _normalScaleY;
        private bool _initialized;

        public bool IsFlipped { get; private set; }

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            if (!(owner is EnemyBase))
            {
                Debug.LogWarning("EnemyVerticalFlipModule은 EnemyBase의 자식 모듈로 배치하세요.", this);
                return;
            }
            _root = owner.transform;
            _normalScaleY = _root.localScale.y;
            _skills = owner.GetModule<EnemySkillModule>();
        }

        public void AfterInit()
        {
            _initialized = _root != null;
            ApplyFlip(isActiveAndEnabled);
        }

        // 비활성 모듈도 ModuleOwner에 등록되므로 GetModule로 찾아 켤 수 있습니다.
        public void SetFlipped(bool flipped) => enabled = flipped;

        private void OnEnable() => ApplyFlip(true);
        private void OnDisable() => ApplyFlip(false);

        private void ApplyFlip(bool flipped)
        {
            if (!_initialized || _root == null || IsFlipped == flipped) return;
            // 반전 직전 고정한 조준 방향을 새 발사 위치에서 사용하지 않도록 정리합니다.
            if (_skills != null) _skills.StopAttack();
            Vector3 scale = _root.localScale;
            scale.y = _normalScaleY * (flipped ? -1f : 1f);
            _root.localScale = scale;
            IsFlipped = flipped;
        }

        // 인스펙터에서 새로 붙일 때도 기본 상태는 꺼짐입니다.
        private void Reset() => enabled = false;
    }
}
