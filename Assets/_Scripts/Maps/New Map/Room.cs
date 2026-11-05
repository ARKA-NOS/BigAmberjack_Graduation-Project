using UnityEngine;
using UnityEngine.Tilemaps;

namespace _Scripts.Maps.New_Map
{
    [RequireComponent(typeof(Tilemap))]
    public class Room : MonoBehaviour
    {
        private Tilemap _tilemap;
        private RoomLink[] _links;

        private void Awake()
        {
            _tilemap = GetTileMap();
            _links = GetRoomLink();
        }

        public Tilemap GetTileMap()
        {
            if (_tilemap == null) 
                _tilemap = GetComponent<Tilemap>();
            return _tilemap;
        }

        public RoomLink[] GetRoomLink()
            => _links ??= GetComponentsInChildren<RoomLink>(true);
        
    }
}