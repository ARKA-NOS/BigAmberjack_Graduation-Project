using System;
using System.Collections.Generic;
using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts._Core.SceneSystem;
using _Scripts.Agent.Enemies;
using _Scripts.Agent.Players;
using _Scripts.Currency;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using _Scripts.Item;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace _Scripts.UI.RunEnd
{
    // 결과창이 쓰는 게임 쪽 기능(종료 이벤트, 재화, 인벤토리, 처치 수, 런 정리와 씬 이동)을 IRunResultSource로 감싼다.
    // RunResultConnector가 인스펙터 설정(표시할 재화, 다음 씬)으로 만든다.
    public class RunResultSource : IRunResultSource
    {
        private readonly CurrencySO _gold;
        private readonly SceneSo _nextScene;
        private Action<RunEndReason> _runEnded;

        public RunResultSource(CurrencySO gold, SceneSo nextScene)
        {
            _gold = gold;
            _nextScene = nextScene;
        }

        // 구독자가 생기면 EventBus를 구독하고, 모두 빠지면 해제한다. EventBus는 정적이라 씬이 바뀌어도 남으므로 해제가 중요하다.
        public event Action<RunEndReason> RunEnded
        {
            add
            {
                if (_runEnded == null) EventBus<RunEndedEvent>.Subscribe(HandleRunEnded);
                _runEnded += value;
            }
            remove
            {
                _runEnded -= value;
                if (_runEnded == null) EventBus<RunEndedEvent>.UnSubscribe(HandleRunEnded);
            }
        }

        public int Gold
        {
            get
            {
                if (_gold == null) return 0;

                CurrencyData data = CurrencySystem.GetCurrencyData(_gold);
                return data != null ? data.Count : 0;
            }
        }

        public Sprite GoldIcon => _gold != null ? _gold.CurrencyIcon : null;

        // CTJ의 EnemyKillCountManager에서 처치 수를 가져온다.
        public int? KillCount
        {
            get
            {
                EnemyKillCountManager killCountManager = ServiceLocator.Get<EnemyKillCountManager>();
                return killCountManager != null ? killCountManager.GetKillCount() : null;
            }
        }

        // 인벤토리를 가져오는 공용 서비스가 아직 없어서, 플레이어(ModuleOwner)의 인벤토리 모듈을 직접 찾는다.
        // 플레이어에 InventoryModule이 없으면 빈 칸으로 채운다.
        public IReadOnlyList<Sprite> GetItemIcons()
        {
            List<Sprite> icons = new();

            ModuleOwner player = ServiceLocator.Get<IPlayerController>() as ModuleOwner;
            if (player == null || !player.TryGetModule(out IInventoryModule inventory))
            {
                for (int i = 0; i < Inventory.Inventory.SlotCount; i++)
                    icons.Add(null);

                return icons;
            }

            foreach (ItemInstance item in inventory.Inventory.Slots)
                icons.Add(item?.Data != null ? item.Data.Icon : null);

            return icons;
        }

        public void LockPlayerInput()
        {
            IPlayerController player = ServiceLocator.Get<IPlayerController>();
            if (player is Object playerObject && playerObject != null)
                player.SetInputActive(false);
        }

        // nextScene(예: Test Home)으로 이동한다. 비어 있으면 현재 씬을 다시 시작한다.
        public void GoNext(RunEndReason reason)
        {
            RunResetter.ResetRun(reason);

            if (_nextScene != null)
                SceneManager.LoadScene(_nextScene);
            else
                RestartScene();
        }

        private void HandleRunEnded(RunEndedEvent evt) => _runEnded?.Invoke(evt.Reason);

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
    }
}
