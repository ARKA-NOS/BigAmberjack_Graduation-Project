using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using _Scripts._Core._ServiceLocator;
using _Scripts.Agent.Players.CharacterSystem;
using _Scripts.Agent.Players.PlayerPackSystem;
using _Scripts.Maps.New_Map;
using UnityEngine;

namespace _Scripts.Agent.Players
{
    // 맵 생성기(MapGenerator)가 있는 씬에 둔다. 맵 생성이 끝나면 시작방의 플레이어 생성 위치로 플레이어를 만들거나 옮긴다.
    // 플레이어(PlayerPack)는 DontDestroyOnLoad라 이전 씬에서 넘어오므로, PlayerSpawner처럼 없을 때만 새로 만들고 있으면 위치만 옮긴다.
    // 완료 이벤트는 로딩창이 사라지기 전에 오므로, 화면이 밝아질 때는 플레이어가 이미 시작방에 있다.
    // 맵을 다시 생성해도(MapGenerator의 재생성 메뉴) 그때마다 새 시작방으로 옮긴다.
    public class MapPlayerSpawner : MonoBehaviour
    {
        // MapGenerator는 Awake에서 생성을 시작하지만 완료 이벤트는 최소 한 프레임 뒤에 오므로 Awake 구독이면 충분하다.
        private void Awake()
        {
            EventBus<MapGenerateEndEvent>.Subscribe(HandleMapGenerated);
        }

        private void OnDestroy()
        {
            EventBus<MapGenerateEndEvent>.UnSubscribe(HandleMapGenerated);
        }

        private void HandleMapGenerated(MapGenerateEndEvent evt)
        {
            Vector3 spawnPosition = evt.PlayerSpawnPosition;
            IPlayerPack playerPack = ServiceLocator.Get<IPlayerPack>();

            if (!playerPack.IsExist())
            {
                ServiceLocator.Get<ICharacterManager>().CreateCharacter(spawnPosition);
                return;
            }

            playerPack.SetPos(spawnPosition);
            StopPlayer();
        }

        // 로딩 중에 빈 공간으로 떨어지며 쌓인 속도가 남아 있으면 옮긴 직후 바닥을 뚫을 수 있으므로 멈춘다.
        private static void StopPlayer()
        {
            if (ServiceLocator.Get<IPlayerController>() is Component player && player != null
                && player.TryGetComponent(out Rigidbody2D body))
            {
                body.linearVelocity = Vector2.zero;
            }
        }
    }
}
