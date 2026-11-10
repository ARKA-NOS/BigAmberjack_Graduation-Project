using _Scripts.Maps.New_Map.MapSettingAsset;
using UnityEngine;

namespace _Scripts.Maps.New_Map.MapGenerateData
{
    public struct RoomDeploymentData
    {
        public RoomSO RoomPrefab;
        public Vector2 Position;

        public RoomDeploymentData(RoomSO roomPrefab, Vector2 position)
        {
            RoomPrefab = roomPrefab;
            Position = position;
        }
    }
}