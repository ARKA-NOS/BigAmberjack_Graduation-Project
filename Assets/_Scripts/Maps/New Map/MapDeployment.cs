using System.Collections.Generic;
using System.Linq;
using System.Threading;
using _Scripts.DataStructure;
using _Scripts.Loading;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using Cysharp.Threading.Tasks;
using UnityEngine;
using Random = System.Random;

namespace _Scripts.Maps.New_Map
{
    public partial class MapGenerator
    {
        private volatile int deploymentCount;
        
        private async UniTask<List<RoomDeploymentData>> MapDeployment(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            if(mapSetting == null) return null;

            var work = UniTask.RunOnThreadPool(() => MapDeploymentInThread(mapSetting), cancellationToken: ct);
            
            while (work.Status == UniTaskStatus.Pending)
            {
                SetLoadingGauge(deploymentCount, mapSetting.MapCount);
                await UniTask.Yield(ct);
            }
            
            List<RoomDeploymentData> list = await work;
            SetLoadingGauge(mapSetting.MapCount, mapSetting.MapCount);
            return list;
        }

        private List<RoomDeploymentData> MapDeploymentInThread(MapGenerateSetting mapSetting)
        {
            List<RoomDeploymentData> result = new List<RoomDeploymentData>();
            
            Vector2 currentPos = Vector2.zero;
            
            RoomDeploymentData currentRoom = new RoomDeploymentData(mapSetting.DefaultRoom,currentPos);
            
            result.Add(currentRoom);
            
            List<RectInt> currentMapRects = new();
            currentMapRects.AddRange(currentRoom.RoomPrefab.GetLinks(Vector2Int.zero));
            
            List<RoomLinkData> linkDataList = new();
            linkDataList.AddRange(currentRoom.RoomPrefab.GetLinks(currentPos));

            Random random = new(mapSetting.Seed);
            
            for (int i = 0; i < mapSetting.MapCount; i++)
            {
                int linkIndex = random.Next(0, linkDataList.Count);
                List<RoomSO> canLinkRooms = GetCanLinkRooms(currentMapRects, linkDataList[linkIndex], mapSetting.Rooms);
                int index = random.Next(0, canLinkRooms.Count);
                RoomSO room = canLinkRooms[index];
                currentRoom = SetRectList(currentMapRects, linkDataList, room);
                result.Add(currentRoom);
            }
            
            return result;
        }
        
        //연결 가능한 거만 전부 찾기
        private List<RoomSO> GetCanLinkRooms(List<RectInt> rectList, RoomLinkData roomLinkDataList,
            List<RoomSO> rooms)
        {
            
            
            
        }

        private RoomDeploymentData SetRectList(List<RectInt> rectList, List<RoomLinkData> roomLinkDataList, RoomSO setRoom)
        {
            
            
            
        }
        
        #region Helper
        
        private static void SetLoadingGauge(int current, int max)
            => LoadingEvents.RaiseSetLoadingGauge("Map Deployment", new Gauge(current,max));
        
        private bool RectListOverlaps(List<RectInt> rect1, List<RectInt> rect2)
            => rect1.Any(i => rect2.Any(i.Overlaps));
        
        #endregion
        
    }
}