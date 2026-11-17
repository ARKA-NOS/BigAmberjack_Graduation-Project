using System.Collections.Generic;
using _Scripts._Core._ServiceLocator;
using _Scripts.Currency;
using _Scripts.UI.MVVM.Connectors;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace _Scripts.UI.CurrencyUI
{
    // 재화 UI에 붙어, 등록된 재화 매니저(ICurrencyManager)를 재화 ViewModel들에 연결한다.
    // 재화 매니저는 GameManager 아래에 있어서 씬을 옮기면(런 종료 등) 새 것으로 바뀌므로, 씬이 바뀔 때마다 다시 연결한다.
    public class CurrencyConnector : ConnectorBase
    {
        [Tooltip("이 UI에 보여줄 재화들의 ViewModel. 각 CurrencyView에 연결한 에셋과 같은 것을 넣는다.")]
        [SerializeField] private List<CurrencyViewModel> viewModels = new();

        protected override void OnEnable()
        {
            base.OnEnable();
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        protected override void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            base.OnDisable();
        }

        // sceneLoaded는 새 씬의 Awake 뒤에 불리므로, 새 GameManager가 등록한 재화 매니저로 다시 연결된다.
        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single) return;

            Reconnect();
        }

        protected override void Connect()
        {
            ICurrencyManager manager = ServiceLocator.Get<ICurrencyManager>();
            if (manager == null)
            {
                Debug.LogWarning($"[{name}] ICurrencyManager가 등록되지 않았습니다.", this);
                return;
            }

            foreach (CurrencyViewModel viewModel in viewModels)
            {
                if (viewModel != null)
                    viewModel.Connect(manager);
            }
        }

        protected override void Disconnect()
        {
            foreach (CurrencyViewModel viewModel in viewModels)
            {
                if (viewModel != null)
                    viewModel.Disconnect();
            }
        }
    }
}
