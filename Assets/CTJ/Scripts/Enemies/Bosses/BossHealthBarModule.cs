using CTJ.UI;
using DevLib.ModuleSystem;
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
        private bool _started;

        public override void Initialize(ModuleOwner owner)
        {
            base.Initialize(owner);
            _boss = owner as BossBase;
        }

        private void Start()
        {
            // NewHealthModule의 AfterInit까지 끝난 뒤 체력에 연결합니다.
            _started = true;
            CreateView();
        }

        private void OnEnable()
        {
            if (_started) CreateView();
        }

        private void CreateView()
        {
            if (_view != null || _boss == null || _boss.IsDead || !isActiveAndEnabled) return;
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

        private void ReleaseView()
        {
            if (_view == null) return;
            _view.gameObject.SetActive(false);
            Destroy(_view.gameObject);
            _view = null;
        }

        private void OnDisable() => ReleaseView();
        private void OnDestroy() => ReleaseView();
    }
}
