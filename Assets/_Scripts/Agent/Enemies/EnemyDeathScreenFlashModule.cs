using _Scripts.DevLib.ModuleSystem;
using _Scripts.UI.HitFeedback;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.Agent.Enemies
{
    [DisallowMultipleComponent]
    public sealed class EnemyDeathScreenFlashModule : Module
    {
        [SerializeField] private EnemyDeathScreenFlash flashPrefab;
        [SerializeField] private Color flashColor = new Color(1f, 1f, 1f, 0.35f);
        [Tooltip("시간 감속의 영향을 받지 않는 실시간입니다.")]
        [SerializeField, Min(0.01f)] private float duration = 0.35f;

        private EnemyBase _enemy;
        private EnemyDeathScreenFlash _view;
        private bool _subscribed;
        private bool _played;

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

            _enemy.DeathStarted += PlayFlash;
            _subscribed = true;
        }

        private void Unsubscribe()
        {
            if (_subscribed && _enemy != null) _enemy.DeathStarted -= PlayFlash;

            _subscribed = false;
        }

        private void PlayFlash()
        {
            if (_played || !isActiveAndEnabled || flashPrefab == null) return;

            _played = true;
            _view = Instantiate(flashPrefab);
            SceneManager.MoveGameObjectToScene(_view.gameObject, gameObject.scene);
            _view.Show(flashColor, duration);
        }

        private void Release()
        {
            Unsubscribe();

            if (_view == null) return;

            _view.gameObject.SetActive(false);
            Destroy(_view.gameObject);
            _view = null;
        }

        private void OnDisable() => Release();

        private void OnDestroy() => Release();
    }
}
