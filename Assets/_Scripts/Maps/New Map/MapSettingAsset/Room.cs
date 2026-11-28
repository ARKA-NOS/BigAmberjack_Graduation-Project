using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        
        [Header("Spawn Points (시작지점이 아니면 설정할 필요 없음)")]
        [SerializeField] private Transform playerSpawnPoint;
        
        public Tilemap GetTileMap()
            => tilemap;
        
        public RoomLink[] GetRoomLink()
            => GetComponentsInChildren<RoomLink>(true);
        
        public Vector3 GetPlayerSpawnPoint()
            => playerSpawnPoint != null ?  playerSpawnPoint.position : Vector3.zero;
    }
}
