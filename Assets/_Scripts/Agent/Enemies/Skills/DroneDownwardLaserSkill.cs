using System.Collections.Generic;
using _Scripts.Agent.Enemies.Bosses;
using _Scripts.DevLib.ModuleSystem;
using UnityEngine;

namespace _Scripts.Agent.Enemies.Skills
{
    public sealed class DroneDownwardLaserSkill : EnemyAttackSkill
    {
        [SerializeField] private Transform muzzle;
        [SerializeField] private DroneVisualModule visual;
        [SerializeField] private SpriteRenderer beam;
        [Tooltip("레이저가 이 렌더러와 같은 정렬 레이어에서 한 단계 뒤에 그려집니다.")]
        [SerializeField] private SpriteRenderer bodyRenderer;
        [SerializeField] private BossLaserShake cameraShake;
        [Tooltip("대각선 끝의 최대 거리입니다. 바닥이나 지형이 더 가까우면 그곳에서 끝납니다.")]
        [SerializeField, Min(0.1f)] private float length = 7.5f;
        [Tooltip("월드 유닛 기준 전체 두께입니다. 빔 표시, 경고, 피해 판정에 함께 적용됩니다.")]
        [SerializeField, Min(0.1f)] private float width = 0.8f;
        [Tooltip("빔 표시만 원점 뒤로 연장해 몸체 안에 겹치는 거리입니다. 월드 유닛이며 피해 판정과 끝점은 유지합니다.")]
        [SerializeField, Min(0f)] private float bodyOverlapDistance = 0.35f;
        [SerializeField, Min(0f)] private float damage = 24f;
        [SerializeField, Min(0f)] private float lastStandDamage = 28f;
        [SerializeField, Min(0.1f)] private float lastStandSpeedMultiplier = 1.2f;
        [Tooltip("발사 시작 시 회전 속도입니다. 도/초 단위이며 Min Speed까지 부드럽게 감속합니다.")]
        [SerializeField, Min(0.01f)] private float maxSpeed = 600f;
        [Tooltip("회전이 끝날 때의 속도입니다. 도/초 단위이며 전체 회전 시간은 두 속도로 계산합니다.")]
        [SerializeField, Min(0.01f)] private float minSpeed = 50f;
        [Tooltip("회전을 끝낸 뒤 빔이 사라지는 시간입니다. 이 구간에는 추가 피해를 주지 않습니다.")]
        [SerializeField, Min(0f)] private float fadeOutDuration = 0.14f;
        [Tooltip("강화 레이저 첫 발사 후 쉬는 시간입니다. 두 번째 발사는 재경고 없이 진행합니다.")]
        [SerializeField, Min(0f)] private float repeatPause = 0.25f;
        [Tooltip("수직 아래에서 좌우로 각각 회전할 각도입니다. 안전 폭을 지키도록 필요하면 줄입니다.")]
        [SerializeField, Range(0f, 85f)] private float sweepHalfAngle = 60f;
        [Tooltip("아레나 양 끝에 남겨 둘 안전 폭입니다. 레이저 두께까지 포함해 보호합니다.")]
        [SerializeField, Min(0f)] private float safeEdgeWidth = 1.25f;
        [Tooltip("레이저 끝과 바닥 부근을 투명하게 만드는 거리입니다. 월드 유닛 기준입니다.")]
        [SerializeField, Min(0.01f)] private float endFadeDistance = 1f;

        private static readonly int BeamOriginId = Shader.PropertyToID("_BeamOrigin");
        private static readonly int BeamDirectionId = Shader.PropertyToID("_BeamDirection");
        private static readonly int BeamLengthId = Shader.PropertyToID("_BeamLength");
        private static readonly int EndFadeId = Shader.PropertyToID("_EndFadeDistance");
        private static readonly int FloorId = Shader.PropertyToID("_FloorY");
        private const int WarningSegments = 48;

        private readonly List<Collider2D> _overlaps = new();
        private readonly RaycastHit2D[] _groundHit = new RaycastHit2D[1];
        private readonly List<Vector2> _warningCloud = new();
        private readonly List<Vector2> _warningPolygon = new();
        private readonly List<Vector2> _clipBuffer = new();
        private MaterialPropertyBlock _beamProperties;
        private ContactFilter2D _terrainFilter;
        private Vector2 _origin;
        private float _floorY, _safeLeft, _safeRight, _halfAngle, _radius, _beamWidth, _bodyOverlap;
        private Bounds _warningBounds;
        private bool _didHit;
        private float _beamStartedAt, _previousAngle;
        private float _startSpeed, _endSpeed, _sweepDuration;
        private int _sweepCount = 1;
        private int _sweepLeg;
        private bool _isFadingOut;
        private bool _waitingForRepeat;
        private float _currentDamage;
        private float _repeatAt;

        public bool DidFire { get; private set; }

        public override void InitSkill(ModuleOwner owner)
        {
            base.InitSkill(owner);
            _beamProperties = new MaterialPropertyBlock();
            SetBeamSorting();

            if (beam != null) beam.enabled = false;
        }

        public bool CanPlanFrom(Vector2 bossPosition)
        {
            if (Enemy is not ProductionControlDrone drone || drone.Arena == null || muzzle == null) return false;

            Vector2 origin = bossPosition + (Vector2)(muzzle.position - Enemy.transform.position);

            return origin.y > drone.FloorY &&
                Mathf.Min(origin.x - drone.Arena.LaserLeft.x, drone.Arena.LaserRight.x - origin.x) >
                Mathf.Max(0f, safeEdgeWidth) + Mathf.Max(0.1f, width) * 0.5f;
        }

        protected override bool CanUseSkillCondition() =>
            base.CanUseSkillCondition() && muzzle != null && beam != null && beam.sprite != null &&
            CanPlanFrom(Enemy.transform.position);

        protected override void OnUseSkill()
        {
            float previousCooldown = CoolTimeStartTime;

            base.OnUseSkill();
            CoolTimeStartTime = previousCooldown;
        }

        protected override void PrepareAttack()
        {
            var drone = (ProductionControlDrone)Enemy;

            _origin = muzzle.position;
            _floorY = drone.FloorY;
            _safeLeft = drone.Arena.LaserLeft.x + Mathf.Max(0f, safeEdgeWidth);
            _safeRight = drone.Arena.LaserRight.x - Mathf.Max(0f, safeEdgeWidth);
            _radius = Mathf.Max(0.1f, length);
            _beamWidth = Mathf.Max(0.1f, width);
            _bodyOverlap = Mathf.Max(0f, bodyOverlapDistance);
            float reach = Mathf.Max(0f, Mathf.Min(_origin.x - _safeLeft, _safeRight - _origin.x) - _beamWidth * 0.5f);
            float safeAngle = Mathf.Asin(Mathf.Clamp01(reach / _radius)) * Mathf.Rad2Deg;

            _halfAngle = Mathf.Min(Mathf.Clamp(sweepHalfAngle, 0f, 85f), safeAngle);
            _endSpeed = Mathf.Max(0.01f, Mathf.Min(minSpeed, maxSpeed));
            _startSpeed = Mathf.Max(_endSpeed, Mathf.Max(minSpeed, maxSpeed));
            bool enhanced = drone.UseLastStandPatterns;

            _sweepCount = enhanced ? 2 : 1;
            _currentDamage = enhanced ? lastStandDamage : damage;

            if (enhanced) { _startSpeed *= lastStandSpeedMultiplier; _endSpeed *= lastStandSpeedMultiplier; }

            _sweepDuration = 2f * _halfAngle / ((_startSpeed + _endSpeed) * 0.5f);
            _sweepLeg = 0;
            _waitingForRepeat = false;
            _terrainFilter = new ContactFilter2D { useTriggers = false };
            _terrainFilter.SetLayerMask(drone.TerrainLayers);
            _didHit = false;
            DidFire = false;
            _isFadingOut = false;
            BuildWarning();
            visual.Play("LaserCharge");
        }

        protected override void UpdateTelegraph() => Telegraph.SetPolygon(_warningBounds, _warningPolygon);

        protected override void OnReleaseAttack()
        {
            CoolTimeStartTime = _beamStartedAt = Time.time;
            DidFire = true;
            Telegraph?.Hide();
            visual.Play("Laser");
            SetBeamSorting();
            beam.enabled = true;
            _previousAngle = _sweepLeg % 2 == 0 ? -_halfAngle : _halfAngle;
            UpdateBeam(_previousAngle, 0f);
            cameraShake?.StartShake(_warningBounds);
            NotifyAttackPerformed(_origin);
            TryHit(_previousAngle);
        }

        public override void UpdateSkill()
        {
            base.UpdateSkill();

            if (!IsUsing || !HasReleased) return;

            float elapsed = Time.time - _beamStartedAt;

            if (!beam.enabled)
            {
                if (_waitingForRepeat)
                {
                    if (Time.time >= _repeatAt)
                    {
                        _waitingForRepeat = false;
                        _sweepLeg++;
                        _didHit = false;
                        _isFadingOut = false;
                        OnReleaseAttack();
                    }
                }
                else if (visual.HasFinished("Laser"))
                {
                    if (_sweepLeg + 1 >= _sweepCount) EndSkill();
                    else
                    {
                        _waitingForRepeat = true;
                        _repeatAt = Time.time + Mathf.Max(0f, repeatPause);
                        visual.Play("Idle");
                    }
                }

                return;
            }

            float angle = EvaluateSweepAngle(elapsed);

            UpdateBeam(angle, elapsed);

            if (!_isFadingOut)
            {
                int steps = Mathf.Clamp(Mathf.CeilToInt(Mathf.Abs(angle - _previousAngle) * Mathf.Deg2Rad * _radius /
                    Mathf.Max(0.05f, _beamWidth * 0.5f)), 1, 128);

                for (int i = 1; i <= steps && !_didHit; i++) TryHit(Mathf.Lerp(_previousAngle, angle, i / (float)steps));

                _previousAngle = angle;

                if (elapsed >= _sweepDuration)
                {
                    _isFadingOut = true;
                    cameraShake?.FadeOut();
                }
            }

            if (elapsed >= _sweepDuration + Mathf.Max(0f, fadeOutDuration))
            {
                beam.enabled = false;
                cameraShake?.StopShake();
            }
        }

        private float EvaluateSweepAngle(float elapsed)
        {
            if (_sweepDuration <= 0f) return _sweepLeg % 2 == 0 ? _halfAngle : -_halfAngle;

            float t = Mathf.Clamp01(elapsed / _sweepDuration);
            // SmoothStep으로 감속하는 속도를 적분합니다. 프레임 간격과 무관하게 같은 각도를 얻습니다.
            float integratedSmoothStep = t * t * t * (1f - 0.5f * t);
            float distance = _sweepDuration * (_startSpeed * t + (_endSpeed - _startSpeed) * integratedSmoothStep);

            float angle = Mathf.Clamp(-_halfAngle + distance, -_halfAngle, _halfAngle);

            return _sweepLeg % 2 == 0 ? angle : -angle;
        }

        private void SetBeamSorting()
        {
            if (beam == null || bodyRenderer == null) return;

            beam.sortingLayerID = bodyRenderer.sortingLayerID;
            beam.sortingOrder = bodyRenderer.sortingOrder - 1;
        }

        private static Vector2 Direction(float angle)
        {
            float radians = angle * Mathf.Deg2Rad;

            return new Vector2(Mathf.Sin(radians), -Mathf.Cos(radians));
        }

        private float BeamLength(Vector2 direction)
        {
            float distance = Mathf.Min(_radius, (_origin.y - _floorY) / Mathf.Max(0.001f, -direction.y));
            RaycastHit2D ground = EnemyLobProjectile.FindSurface(_origin, direction, distance, _terrainFilter, _groundHit);

            return ground.collider != null ? ground.distance : distance;
        }

        private void UpdateBeam(float angle, float elapsed)
        {
            Vector2 direction = Direction(angle);
            float distance = BeamLength(direction);

            // 시작부만 몸체 안으로 연장합니다. 회전 원점과 바닥 쪽 끝점은 그대로 둡니다.
            beam.transform.position = _origin + direction * ((distance - _bodyOverlap) * 0.5f);
            beam.transform.rotation = Quaternion.Euler(0f, 0f, angle);
            Vector3 parentScale = beam.transform.parent != null ? beam.transform.parent.lossyScale : Vector3.one;

            beam.transform.localScale = new Vector3(_beamWidth / beam.sprite.bounds.size.x / Mathf.Max(0.0001f, Mathf.Abs(parentScale.x)),
                (distance + _bodyOverlap) / beam.sprite.bounds.size.y / Mathf.Max(0.0001f, Mathf.Abs(parentScale.y)), 1f);
            Color tint = beam.color;
            float fade = elapsed <= _sweepDuration ? 1f : fadeOutDuration <= 0f ? 0f :
                1f - Mathf.SmoothStep(0f, 1f, (elapsed - _sweepDuration) / fadeOutDuration);

            tint.a = Mathf.Lerp(0.45f, 1f, Mathf.Clamp01(elapsed / 0.055f)) * fade;
            beam.color = tint;
            beam.GetPropertyBlock(_beamProperties);
            _beamProperties.SetVector(BeamOriginId, new Vector4(_origin.x, _origin.y, 0f, 0f));
            _beamProperties.SetVector(BeamDirectionId, new Vector4(direction.x, direction.y, 0f, 0f));
            _beamProperties.SetFloat(BeamLengthId, distance);
            _beamProperties.SetFloat(EndFadeId, Mathf.Max(0.01f, endFadeDistance));
            _beamProperties.SetFloat(FloorId, _floorY);
            beam.SetPropertyBlock(_beamProperties);
        }

        private void TryHit(float angle)
        {
            if (_didHit) return;

            Vector2 direction = Direction(angle);
            float distance = BeamLength(direction);
            Vector2 center = _origin + direction * (distance * 0.5f);

            Physics2D.OverlapBox(center, new Vector2(_beamWidth, distance), angle,
                new ContactFilter2D { useTriggers = true }, _overlaps);

            foreach (var hit in _overlaps)
            {
                if (!EnemyDamageUtility.IsTargetCollider(hit, CastTarget) || hit.bounds.max.y <= _floorY) continue;
                if (!EnemyDamageUtility.TryApply(hit, CastTarget, Enemy, _currentDamage, hit.ClosestPoint(center), direction)) continue;

                _didHit = true;
                break;
            }
        }

        private void BuildWarning()
        {
            _warningCloud.Clear();

            for (int i = 0; i <= WarningSegments; i++)
            {
                Vector2 direction = Direction(Mathf.Lerp(-_halfAngle, _halfAngle, i / (float)WarningSegments));
                Vector2 side = new Vector2(-direction.y, direction.x) * (_beamWidth * 0.5f);
                float distance = BeamLength(direction);
                Vector2 end = _origin + direction * distance;

                _warningCloud.Add(_origin + side); _warningCloud.Add(_origin - side);
                _warningCloud.Add(end + side); _warningCloud.Add(end - side);
            }

            _warningCloud.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
            _warningPolygon.Clear();

            foreach (Vector2 point in _warningCloud) AddHullPoint(point, 0);

            int lower = _warningPolygon.Count;

            for (int i = _warningCloud.Count - 2; i >= 0; i--) AddHullPoint(_warningCloud[i], lower - 1);

            if (_warningPolygon.Count > 1) _warningPolygon.RemoveAt(_warningPolygon.Count - 1);

            ClipWarning(Vector2.up, _floorY);
            ClipWarning(Vector2.right, _safeLeft);
            ClipWarning(Vector2.left, -_safeRight);
            _warningBounds = new Bounds(_origin, Vector3.zero);

            foreach (Vector2 point in _warningPolygon) _warningBounds.Encapsulate(point);
        }

        private void AddHullPoint(Vector2 point, int start)
        {
            while (_warningPolygon.Count >= start + 2)
            {
                Vector2 a = _warningPolygon[_warningPolygon.Count - 1] - _warningPolygon[_warningPolygon.Count - 2];
                Vector2 b = point - _warningPolygon[_warningPolygon.Count - 1];
                if (a.x * b.y - a.y * b.x > 0f) break;

                _warningPolygon.RemoveAt(_warningPolygon.Count - 1);
            }

            _warningPolygon.Add(point);
        }

        private void ClipWarning(Vector2 normal, float offset)
        {
            if (_warningPolygon.Count == 0) return;

            _clipBuffer.Clear();
            Vector2 previous = _warningPolygon[_warningPolygon.Count - 1];
            float previousSide = Vector2.Dot(previous, normal) - offset;

            foreach (Vector2 point in _warningPolygon)
            {
                float side = Vector2.Dot(point, normal) - offset;

                if ((side >= 0f) != (previousSide >= 0f))
                    _clipBuffer.Add(Vector2.Lerp(previous, point, previousSide / (previousSide - side)));

                if (side >= 0f) _clipBuffer.Add(point);

                previous = point; previousSide = side;
            }

            _warningPolygon.Clear();
            _warningPolygon.AddRange(_clipBuffer);
        }

        public override void OnAttackFinished() { }

        protected override void OnEndSkill()
        {
            base.OnEndSkill();
            _waitingForRepeat = false;

            if (beam != null) beam.enabled = false;

            cameraShake?.StopShake();
        }

        protected override void OnStopSkill()
        {
            base.OnStopSkill();
            _waitingForRepeat = false;

            if (beam != null) beam.enabled = false;

            cameraShake?.StopShake();
        }
    }
}
