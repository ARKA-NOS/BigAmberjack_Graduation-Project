using System.Collections.Generic;
using CTJ.Enemies;
using DevLib.ModuleSystem;
using KHO.Scripts.InventorySystem;
using KHO.Scripts.ItemSystem;
using Lrw.Script._Core._EventSystem;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script._Core.SceneSystem;
using Lrw.Script.Agent.Players;
using Lrw.Script.Currency;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace SDW.Scripts.RunEnd
{
    // 탐험 종료 처리. RunEndedEvent를 받으면 결과 데이터를 모아 RunResultViewModel에 Bind하고,
    // 결과 화면의 "다음"을 누르면 다음 단계로 넘어간다. UI 컴포넌트는 직접 참조하지 않는다.
    public class RunResultPresenter : MonoBehaviour
    {
        [SerializeField] private RunResultViewModel viewModel;
        [Tooltip("소지금으로 표시할 재화")]
        [SerializeField] private CurrencySO gold;
        [Tooltip("\"다음\"을 누르면 이동할 씬. 비워두면 현재 씬을 다시 시작한다.")]
        [SerializeField] private SceneSo nextScene;

        private bool _isEnded;
        private bool _isLeaving;

        private void OnEnable()
        {
            // EventBus 핸들러는 정적이라 씬을 다시 불러와도 남으므로 반드시 해제한다.
            EventBus<RunEndedEvent>.Subscribe(HandleRunEnded);
        }

        private void OnDisable()
        {
            EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);
        }

        private void HandleRunEnded(RunEndedEvent evt)
        {
            if (_isEnded)
                return;

            _isEnded = true;

            IPlayerController player = ServiceLocator.Get<IPlayerController>();
            if (player is Object playerObject && playerObject != null)
                player.SetInputActive(false);

            RunResultData result = new RunResultData(
                evt.Reason,
                GetGold(),
                gold != null ? gold.CurrencyIcon : null,
                GetItemIcons(),
                GetKillCountText(),
                HandleNext);

            BindResult(result);
        }

        private int GetGold()
        {
            if (gold == null)
                return 0;

            CurrencyData data = CurrencySystem.GetCurrencyData(gold);
            return data != null ? data.Count : 0;
        }

        // 인벤토리를 가져오는 공용 서비스가 아직 없어서, 플레이어(ModuleOwner)의 인벤토리 모듈을 직접 찾는다.
        // 플레이어에 InventoryModule이 없으면 빈 칸으로 표시한다.
        private static List<Sprite> GetItemIcons()
        {
            List<Sprite> icons = new();

            ModuleOwner player = ServiceLocator.Get<IPlayerController>() as ModuleOwner;
            if (player == null || !player.TryGetModule(out IInventoryModule inventory))
            {
                for (int i = 0; i < Inventory.SlotCount; i++)
                    icons.Add(null);

                return icons;
            }

            foreach (ItemInstance item in inventory.Inventory.Slots)
                icons.Add(item?.Data != null ? item.Data.Icon : null);

            return icons;
        }

        // CTJ의 EnemyKillCountManager에서 처치 수를 가져온다. 씬에 매니저가 없으면 null("-"로 표시).
        private static int? GetKillCountText()
        {
            EnemyKillCountManager killCountManager = ServiceLocator.Get<EnemyKillCountManager>();
            return killCountManager != null ? killCountManager.GetKillCount() : null;
        }

        // "다음" 버튼. nextScene(예: Test Home)으로 이동한다. 비어 있으면 현재 씬을 다시 시작한다.
        private void HandleNext()
        {
            if (_isLeaving)
                return;

            _isLeaving = true;
            BindResult(null);
            DestroyGameManager();

            if (nextScene != null)
                SceneManager.LoadScene(nextScene);
            else
                RestartScene();
        }

        // GameManager는 DontDestroyOnLoad라 다른 씬(또는 같은 씬)을 불러오면 그 씬의 GameManager와 중복된다.
        // 기존 것을 먼저 파괴해서(파괴 시 각 매니저의 GameEnd가 호출됨) 새 씬의 GameManager만 남게 한다.
        private static void DestroyGameManager()
        {
            GameManager gameManager = FindAnyObjectByType<GameManager>();
            if (gameManager != null)
                Destroy(gameManager.gameObject);
        }

        private static void RestartScene()
        {
            Scene current = SceneManager.GetActiveScene();
#if UNITY_EDITOR
            // 테스트 씬은 빌드 세팅에 없을 수 있으므로, 에디터에서는 경로로 다시 불러온다.
            UnityEditor.SceneManagement.EditorSceneManager.LoadSceneInPlayMode(
                current.path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            SceneManager.LoadScene(current.buildIndex);
#endif
        }

        private void BindResult(RunResultData result)
        {
            if (viewModel != null)
                viewModel.Bind(result);
        }
    }
}
