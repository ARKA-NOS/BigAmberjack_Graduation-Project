using CTJ.Enemies.Skills;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using Lrw.Script.Agent.SkillSystem.Interface;
using UnityEngine;
using UnityEngine.Serialization;

namespace CTJ.Enemies
{
    // 선택 모듈. 피해 계산은 EnemyBase, 피격 연출/짧은 이동 잠금만 여기서 처리합니다.
    [DisallowMultipleComponent]
    public sealed class EnemyHitReactionModule : Module
    {
        [Header("Knockback")]
        [SerializeField] private bool enableKnockback = true;
        [SerializeField, Min(0f)] private float knockbackSpeed = 5.1f;
        [SerializeField, Min(0f)] private float knockbackDuration = 0.18f;

        [Header("Attack Interruption")]
        [Tooltip("근접은 준비/타격/회수 동작을 취소하고, 나머지 스킬은 준비/차징 중에만 취소합니다. 이미 발사된 탄은 유지합니다.")]
        [SerializeField, FormerlySerializedAs("cancelPreparingAttack")] private bool cancelAttackOnHit;

        [Header("Blink")]
        [SerializeField] private bool enableBlink = true;
        [SerializeField, Min(0f)] private float blinkDuration = 0.35f;
        [SerializeField, Min(0.01f)] private float blinkInterval = 0.05f;
        [Tooltip("원본 무늬 대신 채울 단색. 투명도는 각 SpriteRenderer의 원래 값을 유지합니다.")]
        [SerializeField, ColorUsage(false)] private Color hitColor = Color.red;
        [Tooltip("텍스처 RGB를 사용하지 않고 알파 윤곽만 사용하는 CTJ 피격 머티리얼.")]
        [SerializeField] private Material hitMaterial;
        [Tooltip("피격 표시용 SpriteRenderer만 지정합니다. 비워두면 소유 적의 자식에서 찾습니다.")]
        [SerializeField] private SpriteRenderer[] sprites;

        private EnemyBase _enemy;
        private Rigidbody2D _body;
        private EnemySkillModule _skills;
        private EnemyAttackWarningModule _attackWarning;
        private bool _knockingBack;
        private float _knockbackEnd;
        private float _knockbackLength;
        private float _initialVelocityX;
        private bool _blinking;
        private float _blinkStart;
        private float _blinkEnd;
        private Color[] _originalColors;
        private Material[] _originalMaterials;

        public bool IsKnockedBack => Ready && enableKnockback && _knockingBack && Time.time < _knockbackEnd;
        public bool IsBlinking => _blinking;
        private bool Ready => isActiveAndEnabled && _enemy != null && _enemy.isActiveAndEnabled && !_enemy.IsDead;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            _body = owner.GetComponent<Rigidbody2D>();
            _skills = owner.GetModule<EnemySkillModule>();
            _attackWarning = owner.GetModule<EnemyAttackWarningModule>();
            if (sprites == null || sprites.Length == 0)
                sprites = owner.GetComponentsInChildren<SpriteRenderer>(true);
            _originalColors = new Color[sprites.Length];
            _originalMaterials = new Material[sprites.Length];
        }

        internal void React(DamageData damage, Vector2 hitDirection)
        {
            if (!Ready) return;

            if (cancelAttackOnHit && _skills != null &&
                (_skills.ActiveSkill is EnemyMeleeSkill ||
                 _skills.ActiveSkill is EnemyAttackSkill { IsPreparing: true } ||
                 _skills.ActiveSkill is IChargeSkill { IsCharging: true }))
                _skills.StopAttack();

            if (enableKnockback && knockbackDuration > 0f && knockbackSpeed > 0f && _body != null && _body.simulated)
            {
                _enemy.StopHorizontalMovement(); // 기존 발판 우회 계획도 취소합니다.
                _knockingBack = true;
                _knockbackLength = knockbackDuration;
                _knockbackEnd = Time.time + _knockbackLength;
                _initialVelocityX = KnockbackDirection(damage, hitDirection) * knockbackSpeed;
                _body.linearVelocityX = _initialVelocityX;
            }

            if (enableBlink && blinkDuration > 0f && hitMaterial != null)
            {
                if (_attackWarning != null) _attackWarning.SuspendForHit();
                // 연타 중에는 피격 색을 원본으로 덮어쓰지 않습니다.
                if (!_blinking)
                    for (int i = 0; i < sprites.Length; i++)
                    {
                        if (sprites[i] == null) continue;
                        _originalColors[i] = sprites[i].color;
                        _originalMaterials[i] = sprites[i].sharedMaterial;
                    }
                _blinking = true;
                _blinkStart = Time.time;
                _blinkEnd = Time.time + blinkDuration;
                SetBlinkColor(true);
            }
        }

        private float KnockbackDirection(DamageData damage, Vector2 hitDirection)
        {
            // 현재 플레이어 평타의 전달 방향과 무관하게, 가해자로부터 멀어지는 쪽을 우선합니다.
            float x = damage.Dealer != null ? _enemy.transform.position.x - damage.Dealer.transform.position.x : 0f;
            if (HasDirection(x)) return Mathf.Sign(x);
            if (HasDirection(damage.DirectedKBForce.x)) return Mathf.Sign(damage.DirectedKBForce.x);
            if (HasDirection(hitDirection.x)) return Mathf.Sign(hitDirection.x);
            x = _enemy.Target != null ? _enemy.transform.position.x - _enemy.Target.position.x : 0f;
            return HasDirection(x) ? Mathf.Sign(x) : 1f;
        }

        private static bool HasDirection(float value) => !float.IsNaN(value) && !float.IsInfinity(value) && Mathf.Abs(value) > 0.001f;

        // EnemyBase.FixedUpdate가 FSM보다 먼저 호출합니다. 중력/지형 충돌은 그대로 사용합니다.
        internal bool TickKnockback()
        {
            if (!_knockingBack) return false;
            if (!IsKnockedBack || _body == null || !_body.simulated)
            {
                EndKnockback();
                return false;
            }
            float remaining = Mathf.Clamp01((_knockbackEnd - Time.time) / _knockbackLength);
            _body.linearVelocityX = _initialVelocityX * remaining;
            return true;
        }

        private void LateUpdate()
        {
            if (!_blinking) return;
            if (!Ready || !enableBlink || hitMaterial == null || Time.time >= _blinkEnd)
            {
                RestoreColor();
                return;
            }
            bool flash = Mathf.FloorToInt((Time.time - _blinkStart) / Mathf.Max(0.01f, blinkInterval)) % 2 == 0;
            SetBlinkColor(flash);
        }

        private void SetBlinkColor(bool flash)
        {
            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] == null) continue;
                Color color = flash ? hitColor : _originalColors[i];
                color.a = _originalColors[i].a;
                // 공유 에셋 내용은 수정하지 않고 참조만 교체합니다. 매번 material 복제도 하지 않습니다.
                sprites[i].sharedMaterial = flash ? hitMaterial : _originalMaterials[i];
                sprites[i].color = color;
            }
        }

        private void RestoreColor()
        {
            if (!_blinking) return;
            SetBlinkColor(false);
            _blinking = false;
        }

        private void EndKnockback()
        {
            if (_knockingBack && _body != null) _body.linearVelocityX = 0f;
            _knockingBack = false;
        }

        internal void ClearReaction()
        {
            EndKnockback();
            RestoreColor();
        }

        private void OnDisable() => ClearReaction();
    }
}
