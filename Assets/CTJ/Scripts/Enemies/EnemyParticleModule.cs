using System;
using System.Collections.Generic;
using DevLib.BattleSystem;
using DevLib.ModuleSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CTJ.Enemies
{
    // 피격 파티클의 선택/배치만 담당합니다. 경직/넉백/공격 취소와 독립적으로 제거할 수 있습니다.
    [DisallowMultipleComponent]
    public sealed class EnemyParticleModule : Module
    {
        [Serializable]
        public sealed class HitEffect
        {
            public string label = "Impact";
            public bool enabled = true;
            public ParticleSystem prefab;
            [Tooltip("비워두면 Prefab을 사용합니다. 지정하면 피격마다 이 목록에서 하나만 선택하며 직전 종류는 피합니다.")]
            public ParticleSystem[] randomPrefabs;
            [Min(0.01f)] public float scale = 1f;
            [Tooltip("피격 방향을 기준으로 한 로컬 위치 보정입니다.")]
            public Vector2 offset;
            public bool alignToHitDirection = true;
            public float angleOffset;

            [NonSerialized] private ParticleSystem _lastPrefab;

            internal ParticleSystem SelectPrefab()
            {
                int count = 0;
                ParticleSystem fallback = prefab;
                if (randomPrefabs != null)
                    foreach (ParticleSystem candidate in randomPrefabs)
                    {
                        if (candidate == null) continue;
                        fallback = candidate;
                        if (candidate != _lastPrefab) count++;
                    }
                if (count == 0) return _lastPrefab = fallback;
                int choice = UnityEngine.Random.Range(0, count);
                foreach (ParticleSystem candidate in randomPrefabs)
                    if (candidate != null && candidate != _lastPrefab && choice-- == 0)
                        return _lastPrefab = candidate;
                return fallback;
            }
        }

        [Tooltip("피격 좌표가 유효하지 않거나 사용하지 않을 때의 기준점. 비우면 이 모듈 위치입니다.")]
        [SerializeField] private Transform fallbackOrigin;
        [SerializeField] private bool useHitPoint = true;
        [Tooltip("기준점에서 이 거리보다 먼 피격 좌표는 기준점으로 대체합니다. 잘못 전달된 (0,0) 좌표 등을 방지합니다.")]
        [SerializeField, Min(0f)] private float maxHitPointDistance = 3f;
        [SerializeField, Min(0.01f)] private float effectScale = 1f;
        [SerializeField] private List<HitEffect> hitEffects = new List<HitEffect>();
        [Tooltip("적 한 마리의 동시 효과 수. 초과하면 가장 오래된 효과부터 정리합니다.")]
        [SerializeField, Min(1)] private int maxConcurrentEffects = 12;
        [Tooltip("잘못된 루프/긴 수명 프리팹의 정리 상한(초). 일반 효과는 파티클이 끝나면 먼저 제거됩니다.")]
        [SerializeField, Min(0.1f)] private float maxEffectLifetime = 5f;

        private EnemyBase _enemy;
        private bool _subscribed;
        private readonly List<EnemyHitParticle> _activeEffects = new List<EnemyHitParticle>();

        public override void Initialize(ModuleOwner owner)
        {
            Unsubscribe();
            base.Initialize(owner);
            _enemy = owner as EnemyBase;
            if (isActiveAndEnabled) Subscribe();
        }

        private void OnEnable() => Subscribe();

        private void Subscribe()
        {
            if (_subscribed || _enemy == null) return;
            _enemy.DamageReceived += PlayHit;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DamageReceived -= PlayHit;
            _subscribed = false;
        }

        private void PlayHit(DamageData damage, Vector2 point, Vector2 direction, Vector2 normal)
        {
            // 실제 체력 감소가 확정되고 사망 처리에 들어가기 전에 전달받습니다.
            if (!isActiveAndEnabled) return;
            Vector3 origin = fallbackOrigin != null ? fallbackOrigin.position : transform.position;
            Vector3 position = origin;
            if (useHitPoint && IsFinite(point) && Vector2.Distance(point, origin) <= maxHitPointDistance)
                position = new Vector3(point.x, point.y, origin.z);
            if (!HasDirection(direction) && damage.Dealer != null) direction = origin - damage.Dealer.transform.position;
            if (!HasDirection(direction)) direction = -normal;
            if (!HasDirection(direction) && _enemy.Target != null) direction = origin - _enemy.Target.position;
            if (!HasDirection(direction)) direction = Vector2.right;
            float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;

            _activeEffects.RemoveAll(effect => effect == null);
            foreach (HitEffect entry in hitEffects)
            {
                if (entry == null || !entry.enabled || entry.scale <= 0f) continue;
                ParticleSystem selectedPrefab = entry.SelectPrefab();
                if (selectedPrefab == null) continue;
                while (_activeEffects.Count >= Mathf.Max(1, maxConcurrentEffects))
                {
                    EnemyHitParticle oldest = _activeEffects[0];
                    if (oldest != null) { oldest.gameObject.SetActive(false); Destroy(oldest.gameObject); }
                    _activeEffects.RemoveAt(0);
                }
                float effectAngle = angle;
                if (selectedPrefab.TryGetComponent<EnemyBloodParticle>(out _) && damage.Dealer != null)
                {
                    // 전달 방향 규약이 서로 다른 공격도, 혈흔은 가해자로부터 멀어지는 쪽으로 뿜습니다.
                    Vector2 away = origin - damage.Dealer.transform.position;
                    if (HasDirection(away)) effectAngle = Mathf.Atan2(away.y, away.x) * Mathf.Rad2Deg;
                }
                Quaternion rotation = Quaternion.Euler(0f, 0f, entry.angleOffset + (entry.alignToHitDirection ? effectAngle : 0f));
                ParticleSystem particle = Instantiate(selectedPrefab, position + rotation * (Vector3)entry.offset, rotation);
                // 적의 부모/반전/펀치 스케일과 분리하되, 같은 씬에 두어 씬 언로드 때 함께 정리합니다.
                SceneManager.MoveGameObjectToScene(particle.gameObject, gameObject.scene);
                particle.transform.localScale *= Mathf.Max(0.01f, effectScale) * entry.scale;
                particle.gameObject.SetActive(true);
                EnemyHitParticle lifetime = particle.GetComponent<EnemyHitParticle>();
                if (lifetime == null) lifetime = particle.gameObject.AddComponent<EnemyHitParticle>();
                lifetime.Play(particle, maxEffectLifetime);
                _activeEffects.Add(lifetime);
            }
        }

        private static bool IsFinite(Vector2 value) => !float.IsNaN(value.x) && !float.IsInfinity(value.x) &&
            !float.IsNaN(value.y) && !float.IsInfinity(value.y);
        private static bool HasDirection(Vector2 value) => IsFinite(value) && value.sqrMagnitude > 0.0001f;

        // 이미 생성된 효과는 적 사망/모듈 제거 후에도 자신의 수명이 끝날 때까지 재생됩니다.
        private void OnDisable() => Unsubscribe();
        private void OnDestroy() => Unsubscribe();
    }
}
