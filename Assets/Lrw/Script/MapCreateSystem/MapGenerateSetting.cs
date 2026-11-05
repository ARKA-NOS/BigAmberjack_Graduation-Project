using System.Collections.Generic;
using UnityEngine;

namespace Lrw.Script.MapCreateSystem
{
    [CreateAssetMenu(fileName = "Map Create Setting", menuName = "MapSystem/Map Create Setting", order = 0)]
    public class MapGenerateSetting : ScriptableObject
    {
        [field:Header("Room Settings")]
        [field:SerializeField] public RoomSO DefaultRoom { get; private set; }
        [field:SerializeField] public List<RoomSO> Rooms { get; private set; }

        [field: Header("Map Settings")]
        [field: SerializeField] public Vector2Int MapSize { get; private set; } = new(100, 100);
        
        
        
    }
}