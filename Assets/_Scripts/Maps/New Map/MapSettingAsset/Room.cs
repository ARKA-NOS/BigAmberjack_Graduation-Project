using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    public class Room : MonoBehaviour
    {
        [SerializeField] private Tilemap tilemap;
        [SerializeField] private RoomLink[] links;
        
        public Tilemap GetTileMap()
            => tilemap;
        
        public RoomLink[] GetRoomLink()
            => links;

    }
}