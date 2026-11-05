using CTJ.UI;
using DevLib.ModuleSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script.Agent.HealthSystem.NewHealthSystem;
using SDW.Scripts.RunEnd;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace CTJ.Enemies.Bosses
{
    // 보스의 자식으로 두는 선택 모듈. 화면 UI의 생성/제거만 맡습니다.
    [DisallowMultipleComponent]
    public sealed class BossHealthBarModule : Module
    {
        [SerializeField] private BossHealthBarUI healthBarPrefab;
        private BossBase _boss;
        private BossHealthBarUI _view;
        private INewHealthModule _health;
        private bool _started;
        private bool _closed;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _boss = owner as BossBase;
        }

        private void Start()
        {
            // NewHealthModule의 AfterInit까지 끝난 뒤 체력에 연결합니다.
            _started = true;
            SubscribeHealth();
            CreateView();
        }

        private void OnEnable()
        {
            EventBus<RunEndedEvent>.Subscribe(HandleRunEnded);
            if (_started)
            {
                SubscribeHealth();
                CreateView();
            }
        }

        private void SubscribeHealth()
        {
            UnsubscribeHealth();
            if (_boss == null) return;
            _health = _boss.GetModule<INewHealthModule>();
            if (_health == null) return;
            _health.OnDead += CloseView;
            if (_health.IsDead || _health.CurrentHealth <= 0f) CloseView();
        }

        private void UnsubscribeHealth()
        {
            if (_health != null) _health.OnDead -= CloseView;
            _health = null;
        }

        private void CreateView()
        {
            if (_closed || _view != null || _boss == null || _boss.IsDead || !isActiveAndEnabled) return;
            if (healthBarPrefab == null)
            {
                Debug.LogWarning($"{name}: Boss Health Bar Prefab을 연결하세요.", this);
                return;
            }
            // Overlay Canvas는 씬 루트에 두어 보스의 이동/스케일을 물려받지 않게 합니다.
            _view = Instantiate(healthBarPrefab);
            _view.name = _boss.name + " HealthBar";
            SceneManager.MoveGameObjectToScene(_view.gameObject, _boss.gameObject.scene);
            _view.Bind(_boss);
        }

        private void HandleRunEnded(RunEndedEvent _) => CloseView();

        private void CloseView()
        {
            // OnDead 콜백 중에는 공용 HealthModule.IsDead가 아직 false일 수 있습니다.
            // 이벤트 자체로 종료를 확정해 재활성화/LateUpdate에서 다시 표시되지 않게 합니다.
            _closed = true;
            ReleaseView();
        }

        private void ReleaseView()
        {
            if (_view == null) return;
            _view.gameObject.SetActive(false);
            Destroy(_view.gameObject);
            _view = null;
        }

        private void OnDisable()
        {
            EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);
            UnsubscribeHealth();
            ReleaseView();
        }

        private void OnDestroy()
        {
            EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);
            UnsubscribeHealth();
            ReleaseView();
        }
    }
}
