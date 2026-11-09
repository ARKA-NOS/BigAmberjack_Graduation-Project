using System.Collections.Generic;
using _Scripts.Agent.Enemies;
using _Scripts.UI.BindSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI.HitFeedback
{
    [DisallowMultipleComponent, RequireComponent(typeof(EnemyDamageIndicatorModule))]
    public sealed class EnemyDamageIndicatorBinder : AbstractUIBinder<DamageIndicatorData>
    {
        [SerializeField] private EnemyDamageIndicatorPopup _popupPrefab;
        [SerializeField, Min(1)] private int _maxVisiblePerEnemy = 16;

        private EnemyDamageIndicatorModule _source;
        private readonly List<EnemyDamageIndicatorPopup> _active = new List<EnemyDamageIndicatorPopup>();

        protected override void Awake()
        {
            _source = GetComponent<EnemyDamageIndicatorModule>();
            base.Awake();
        }

        protected override void OnDataChanged()
        {
            // 같은 SO를 여러 적이 사용해도 자신의 요청만 즉시 복사해 소비합니다.
            // Update에서 마지막 값만 읽지 않으므로 같은 프레임의 연타도 누락되지 않습니다.
            DamageIndicatorData data = CurrentValue;
            if (!isActiveAndEnabled || _source == null || !_source.isActiveAndEnabled ||
                data.SourceId != _source.GetInstanceID() || _popupPrefab == null) return;

            _active.RemoveAll(popup => popup == null);
            while (_active.Count >= Mathf.Max(1, _maxVisiblePerEnemy))
            {
                if (_active[0] != null) Destroy(_active[0].gameObject);
                _active.RemoveAt(0);
            }
            EnemyDamageIndicatorPopup popup = Instantiate(_popupPrefab, data.Position, Quaternion.identity);
            SceneManager.MoveGameObjectToScene(popup.gameObject, gameObject.scene);
            popup.Show(data);
            _active.Add(popup);
        }

        protected override void DataUpdate() { }

        // 생성된 숫자는 적과 부모 관계가 없으며, 적이 사라져도 자신의 연출을 끝냅니다.
        // base.OnDestroy가 ViewModel 이벤트 구독을 해제합니다.
    }
}
