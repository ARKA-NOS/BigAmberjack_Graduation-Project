using System.Collections.Generic;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapSettingAsset
{
    [CreateAssetMenu(fileName = "Map Create Setting", menuName = "MapSystem/Map Create Setting", order = 0)]
    public class MapGenerateSetting : ScriptableObject
    {
        [field: SerializeField] public int Seed { get; private set; }

        [field:Header("Room Settings")]
        [field:SerializeField] public RoomSO DefaultRoom { get; private set; }
        [field:SerializeField] public List<RoomSO> Rooms { get; private set; }

        [field: Header("Map Settings")]
        [field: SerializeField] public int MapCount { get; private set; } = 10;
        [field: SerializeField] public Vector2Int BranchRange { get; private set; }
        [field: SerializeField] public int BranchCount { get; private set; } = 1;
        
    }
}