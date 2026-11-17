using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        // 배열 순서가 굽기와 마감의 링크 인덱스다. 빼거나 순서를 바꾸면 두 순서가 어긋난다.
        [SerializeField] private RoomLink[] links;

        [Header("Spawn Points")]
        [SerializeField] private Transform playerSpawnPoint;
        [SerializeField] private Transform[] enemySpawnPoints;
        [SerializeField] private Transform[] rewardSpawnPoints;

        public Tilemap GetTileMap()
            => tilemap;

        public RoomLink[] GetRoomLink()
            => links;

        public Transform GetPlayerSpawnPoint()
            => playerSpawnPoint;

        public IReadOnlyList<Transform> GetEnemySpawnPoints()
            => enemySpawnPoints ?? System.Array.Empty<Transform>();

        public IReadOnlyList<Transform> GetRewardSpawnPoints()
            => rewardSpawnPoints ?? System.Array.Empty<Transform>();

        // connected[i]가 links[i]의 연결 여부다. 연결되지 않은 링크는 막는다.
        public void ApplyLinkState(bool[] connected)
        {
            if (links == null) return;

            for (int i = 0; i < links.Length; i++)
            {
                if (links[i] == null) continue;

                bool isConnected = connected != null && i < connected.Length && connected[i];
                links[i].SetConnected(isConnected);
            }
        }
    }
}
