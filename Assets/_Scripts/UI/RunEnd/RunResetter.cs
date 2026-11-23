using _Scripts._Core._Debug;
using _Scripts._Core._Manager;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.PlayerPackSystem;
using _Scripts.Maps;
using UnityEngine;

namespace _Scripts.UI.RunEnd
{
    // 탐험이 끝나 다른 씬으로 넘어가기 직전에, 씬을 넘어 남는 상태를 정리한다.
    public static class RunResetter
    {
        public static void ResetRun(RunEndReason reason)
        {
            ResetPlayer();
            DestroyGameManager();
            Portal.ResetTransitionState();
            FDebug.Log($"[RunResetter] 런 종료({reason}) - 플레이어/GameManager 초기화");
        }

        // PlayerPack은 DontDestroyOnLoad라 죽은 상태 그대로 다음 씬에 남는다.
        // Destroy는 프레임 끝에 처리되므로, 다음 씬의 PlayerSpawner/LinearStageGenerator가 실행될 때는
        // NullPlayerPack이 등록되어 있어 CharacterManager가 새 캐릭터를 만든다.
        private static void ResetPlayer()
        {
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();
            if (!playerPack.IsExist())
            {
                FDebug.LogWarning("[RunResetter] 파괴할 PlayerPack이 없습니다.");
                return;
            }

            playerPack.Destroy();
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
