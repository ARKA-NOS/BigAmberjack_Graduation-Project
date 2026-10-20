using System.Collections.Generic;
using UnityEngine;

namespace SDW.Scripts.Maps
{
    // Room Prefab의 루트에 붙여서, 포탈 생성 위치와 플레이어 스폰 위치를 미리 지정해두는 컴포넌트.
    public class RoomDefinition : MonoBehaviour
    {
        [Header("Portal Points")]
        [SerializeField] private Transform leftPortalPoint;
        [SerializeField] private Transform rightPortalPoint;

        [Header("Player Spawn")]
        [Tooltip("시작 방일 때 플레이어가 생성될 위치. 비워두면 방의 위치를 사용한다.")]
        [SerializeField] private Transform playerSpawnPoint;
        
        [Header("NPC Spawn")]
        [Tooltip("NPC가 생성될 위치. 비워두면 방 원점 + 오프셋으로 생성")]
        [SerializeField] private Transform npcSpawnPoint;

        [Header("Chest Spawn")]
        [Tooltip("보물상자가 생성될 위치. 비워두면 방 원점 + 오프셋으로 생성")]
        [SerializeField] private Transform chestSpawnPoint;

        [Header("Reward Chest Spawn")]
        [Tooltip("보상방에서 상자가 생성될 위치들. 포인트 하나마다 상자 하나가 생성된다.")]
        [SerializeField] private List<Transform> rewardChestSpawnPoints = new();

        public Transform PlayerSpawnPoint => playerSpawnPoint;
        public Transform NPCSpawnPoint => npcSpawnPoint;
        public Transform ChestSpawnPoint => chestSpawnPoint;
        public IReadOnlyList<Transform> RewardChestSpawnPoints => rewardChestSpawnPoints;

        public Transform GetPortalPoint(RoomDirection direction)
        {
            return direction == RoomDirection.Left ? leftPortalPoint : rightPortalPoint;
        }
    }
}
