using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        // 빈 칸이 있으면 이 방은 배치에서 제외된다.
        [SerializeField] private RoomLink[] links;

        [Header("Spawn Points (시작지점이 아니면 설정할 필요 없음)")]
        [SerializeField] private Transform playerSpawnPoint;
        
        public Tilemap GetTileMap()
            => tilemap;
        
        public RoomLink[] GetRoomLink()
            => links;
        
        public Vector3 GetPlayerSpawnPoint()
            => playerSpawnPoint != null ?  playerSpawnPoint.position : Vector3.zero;
    }
}
