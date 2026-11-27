using _Scripts._Core._Debug;
using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.HealthSystem.NewHealthSystem;
using _Scripts.Agent.Players;
using _Scripts.Agent.Players.PlayerPackSystem;
using _Scripts.Agent.Players.TitleSystem;
using _Scripts.DevLib.ModuleSystem;
using _Scripts.Inventory;
using _Scripts.Maps;
using UnityEngine;

namespace _Scripts.UI.RunEnd
{
    // 탐험이 끝나 다른 씬으로 넘어가기 직전에, 씬을 넘어 남는 상태를 정리한다.
    // 플레이어는 사망했을 때만 파괴해서 다음 씬에서 새로 생성되게 하고, 보스 클리어 시에는 유지하되 인벤토리·칭호를 비우고 체력을 채운다.
    public static class RunResetter
    {
        public static void ResetRun(RunEndReason reason)
        {
            if (reason == RunEndReason.PlayerDead)
                ResetPlayer();
            else
            {
                // 플레이어를 유지하므로 장착 아이템과 그 효과가 다음 런에 남지 않게 인벤토리를 비운다.
                ClearInventory();
                ResetTitles();
                RestoreHealth();
                RestorePlayerInput();
            }

            DestroyGameManager();
            Portal.ResetTransitionState();
        }

        // PlayerPack은 DontDestroyOnLoad라 죽은 상태 그대로 다음 씬에 남는다.
        // Destroy는 프레임 끝에 처리되므로, 다음 씬의 PlayerSpawner/LinearStageGenerator가 실행될 때는
        // NullPlayerPack이 등록되어 있어 CharacterManager가 새 캐릭터를 만든다.
        private static void ResetPlayer()
        {
            ServiceLocator.Get<IPlayerPack>().Destroy();
        }

        private static void ClearInventory()
        {
            ModuleOwner player = ServiceLocator.Get<IPlayerController>() as ModuleOwner;
            if (player != null && player.TryGetModule(out IInventoryModule inventory))
                inventory.Clear();
        }

        private static void ResetTitles()
        {
            ModuleOwner player = ServiceLocator.Get<IPlayerController>() as ModuleOwner;
            if (player != null && player.TryGetModule(out ITitleModule titleModule))
                titleModule.ResetTitles();
            else
                FDebug.LogWarning("[RunResetter] 플레이어의 TitleModule을 찾지 못해 칭호를 초기화하지 못했습니다.");
        }

        private static void RestoreHealth()
        {
            ModuleOwner player = ServiceLocator.Get<IPlayerController>() as ModuleOwner;
            if (player != null && player.TryGetModule(out INewHealthModule health))
            {
                health.ApplyHeal(health.MaxHealth);
                FDebug.Log($"[RunResetter] 체력 회복 {health.CurrentHealth}/{health.MaxHealth}");
            }
            else
                FDebug.LogWarning("[RunResetter] 플레이어의 체력 모듈을 찾지 못해 체력을 회복하지 못했습니다.");
        }

        // 결과 화면을 띄울 때 입력을 껐으므로, 플레이어를 유지하는 경우 다시 켜 둔다.
        // (PlayerSpawner는 위치만 옮기고 입력을 켜지 않는다)
        private static void RestorePlayerInput()
        {
            IPlayerController player = ServiceLocator.Get<IPlayerController>();
            if (player is Object playerObject && playerObject != null)
                player.SetInputActive(true);
        }

        // GameManager는 DontDestroyOnLoad라 다른 씬(또는 같은 씬)을 불러오면 그 씬의 GameManager와 중복된다.
        // 기존 것을 먼저 파괴해서(파괴 시 각 매니저의 GameEnd가 호출됨) 새 씬의 GameManager만 남게 한다.
        private static void DestroyGameManager()
        {
            GameManager gameManager = Object.FindAnyObjectByType<GameManager>();
            if (gameManager != null)
                Object.Destroy(gameManager.gameObject);
        }
    }
}
