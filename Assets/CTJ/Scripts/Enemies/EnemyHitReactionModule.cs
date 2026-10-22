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
        [SerializeField, Min(0f)] private float knockbackSpeed = 6.4f;
        [SerializeField, Min(0f)] private float knockbackDuration = 0.18f;
        [Tooltip("넉백 없이도 행동을 제한하는 피격 경직 시간. 0이면 기존 넉백 시간만 사용합니다.")]
        [SerializeField, Min(0f)] private float hitStunDuration;

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

        [Header("Scale Pulse")]
        [Tooltip("피격마다 가로/세로를 약하게 눌렀다가 복원합니다. 보스 등에서는 별도로 켤 수 있습니다.")]
        [SerializeField] private bool enableScalePulse;
        [Tooltip("그림만 가진 자식 Transform. 방향 전환 루트, 콜라이더, 발사 위치가 포함된 부모는 지정하지 않습니다.")]
        [SerializeField] private Transform[] scaleTargets;
        [Tooltip("최대 변형 비율. 실제 변형은 이 값의 50~100%이며 가로/세로 방향은 매번 무작위입니다.")]
        [SerializeField, Range(0f, 0.2f)] private float scalePulseAmount = 0.08f;
        [SerializeField, Min(0f)] private float scalePulseDuration = 0.16f;

        private EnemyBase _enemy;
        private Rigidbody2D _body;
        private EnemySkillModule _skills;
        private EnemyAttackWarningModule _attackWarning;
        private bool _knockingBack;
        private float _knockbackEnd;
        private float _knockbackLength;
        private float _initialVelocityX;
        private float _stunEnd;
        private bool _blinking;
        private float _blinkStart;
        private float _blinkEnd;
        private Color[] _originalColors;
        private Material[] _originalMaterials;
        private Transform[] _scaleTargets;
        private Vector3[] _originalScales;
        private bool _scaling;
        private float _scaleStart;
        private float _scaleLength;
        private float _scaleOffset;

        public bool IsKnockedBack => Ready && enableKnockback && _knockingBack && Time.time < _knockbackEnd;
        public bool IsStunned => Ready && Time.time < _stunEnd;
        public bool IsReacting => IsKnockedBack || IsStunned;
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
            // 피격마다 배열을 만들지 않고, 초기화 때 시각 전용 대상을 확보합니다.
            _scaleTargets = scaleTargets != null ? (Transform[])scaleTargets.Clone() : new Transform[0];
            _originalScales = new Vector3[_scaleTargets.Length];
            for (int i = 0; i < _scaleTargets.Length; i++)
            {
                Transform target = _scaleTargets[i];
                if (target == null || target == owner.transform || !target.IsChildOf(owner.transform) ||
                    target.GetComponentInChildren<Collider2D>(true) != null ||
                    target.GetComponentInChildren<Rigidbody2D>(true) != null)
                    _scaleTargets[i] = null;
            }
        }

        internal void React(DamageData damage, Vector2 hitDirection)
        {
            if (!Ready) return;
            if (hitStunDuration > 0f) _stunEnd = Time.time + hitStunDuration;

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
            StartScalePulse();
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

        // EnemyBase.FixedUpdate가 FSM보다 먼저 호출합니다. 경직과 실제 밀림은 독립적입니다.
        internal bool TickReaction()
        {
            if (TickKnockback()) return true;
            if (!IsStunned) return false;
            _enemy.StopHorizontalMovement();
            return true;
        }

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
            TickScalePulse(); // 점멸을 꺼도 스케일 연출은 독립적으로 재생/복원됩니다.
            if (!_blinking) return;
            if (!Ready || !enableBlink || hitMaterial == null || Time.time >= _blinkEnd)
            {
                RestoreColor();
                return;
            }
            bool flash = Mathf.FloorToInt((Time.time - _blinkStart) / Mathf.Max(0.01f, blinkInterval)) % 2 == 0;
            SetBlinkColor(flash);
        }

        private void StartScalePulse()
        {
            // 연타 시 이전 변형을 원본으로 다시 저장하지 않아 크기가 누적되지 않습니다.
            RestoreScale();
            if (!enableScalePulse || scalePulseAmount <= 0f || scalePulseDuration <= 0f ||
                _scaleTargets == null || _scaleTargets.Length == 0) return;

            _scaleOffset = Random.Range(0.5f, 1f) * Mathf.Clamp(scalePulseAmount, 0f, 0.2f);
            if (Random.value < 0.5f) _scaleOffset = -_scaleOffset;
            _scaleStart = Time.time;
            _scaleLength = scalePulseDuration;
            for (int i = 0; i < _scaleTargets.Length; i++)
                if (_scaleTargets[i] != null) _originalScales[i] = _scaleTargets[i].localScale;
            _scaling = true;
            ApplyScale(1f);
        }

        private void TickScalePulse()
        {
            if (!_scaling) return;
            float elapsed = Time.time - _scaleStart;
            if (!Ready || !enableScalePulse || elapsed >= _scaleLength)
            {
                RestoreScale();
                return;
            }
            ApplyScale(1f - Mathf.SmoothStep(0f, 1f, elapsed / _scaleLength));
        }

        private void ApplyScale(float weight)
        {
            float offset = _scaleOffset * weight;
            Vector3 multiplier = new Vector3(1f + offset, 1f - offset, 1f);
            for (int i = 0; i < _scaleTargets.Length; i++)
                if (_scaleTargets[i] != null)
                    _scaleTargets[i].localScale = Vector3.Scale(_originalScales[i], multiplier);
        }

        private void RestoreScale()
        {
            if (!_scaling) return;
            for (int i = 0; i < _scaleTargets.Length; i++)
                if (_scaleTargets[i] != null) _scaleTargets[i].localScale = _originalScales[i];
            _scaling = false;
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
            _stunEnd = 0f;
            EndKnockback();
            RestoreColor();
            RestoreScale();
        }

        private void OnDisable() => ClearReaction();
    }
}
