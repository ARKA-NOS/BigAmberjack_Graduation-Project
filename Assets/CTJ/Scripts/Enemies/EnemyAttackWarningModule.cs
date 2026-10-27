using DevLib.ModuleSystem;
using UnityEngine;

namespace CTJ.Enemies
{
    // 공격 경고와 피격 점멸이 같은 Renderer를 사용할 때 피격 표시를 우선합니다.
    [DisallowMultipleComponent, DefaultExecutionOrder(100)]
    public sealed class EnemyAttackWarningModule : Module
    {
        [SerializeField] private SpriteRenderer[] sprites;
        [SerializeField] private Material warningMaterial;
        [SerializeField, ColorUsage(false)] private Color warningColor = Color.red;
        private EnemyBase _enemy;
        private EnemyHitReactionModule _hitReaction;
        private Color[] _colors;
        private Material[] _materials;
        private bool _requested;
        private bool _applied;

        public bool IsShowing => _applied;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _hitReaction = owner.GetModule<EnemyHitReactionModule>();
            if (sprites == null || sprites.Length == 0) sprites = owner.GetComponentsInChildren<SpriteRenderer>(true);
            _colors = new Color[sprites.Length];
            _materials = new Material[sprites.Length];
        }

        internal void Show()
        {
            _requested = true;
            ApplyWarning();
        }

        internal void Hide()
        {
            _requested = false;
            SuspendForHit();
        }

        // 피격 모듈이 원본 색/머티리얼을 저장하기 전에 경고를 걷어냅니다.
        internal void SuspendForHit()
        {
            if (!_applied) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                sprites[i].color = _colors[i];
                sprites[i].sharedMaterial = _materials[i];
            }
            _applied = false;
        }

        private void LateUpdate()
        {
            if (!_requested || _enemy == null || !_enemy.isActiveAndEnabled || _enemy.IsDead)
            {
                Hide();
                return;
            }
            ApplyWarning();
        }

        private void ApplyWarning()
        {
            if (_applied || !isActiveAndEnabled || _enemy == null || !_enemy.isActiveAndEnabled || _enemy.IsDead ||
                warningMaterial == null || (_hitReaction != null && _hitReaction.IsBlinking)) return;
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                _colors[i] = sprites[i].color;
                _materials[i] = sprites[i].sharedMaterial;
                Color color = warningColor;
                color.a = _colors[i].a;
                sprites[i].sharedMaterial = warningMaterial;
                sprites[i].color = color;
            }
            _applied = true;
        }

        private void OnDisable() => Hide();
    }
}
