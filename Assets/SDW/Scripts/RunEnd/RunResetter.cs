using Lrw.Script._Core._Manager;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players;
using Lrw.Script.Agent.Players.PlayerPackSystem;
using SDW.Scripts.Maps;
using UnityEngine;

namespace SDW.Scripts.RunEnd
{
    // 탐험이 끝나 다른 씬으로 넘어가기 직전에, 씬을 넘어 남는 상태를 정리한다.
    // 플레이어는 사망했을 때만 파괴해서 다음 씬에서 새로 생성되게 하고, 보스 클리어 시에는 그대로 유지한다.
    public static class RunResetter
    {
        public static void ResetRun(RunEndReason reason)
        {
            if (reason == RunEndReason.PlayerDead)
                ResetPlayer();
            else
                RestorePlayerInput();

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
