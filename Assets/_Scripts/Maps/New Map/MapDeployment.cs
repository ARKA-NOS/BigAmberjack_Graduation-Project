using System.Collections.Generic;
using System.Threading;
using _Scripts.DataStructure;
using _Scripts.Loading;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using Cysharp.Threading.Tasks;
using UnityEngine;

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
            int nowSettingMap = 0;
            
            RoomDeploymentData currentRoom = new RoomDeploymentData(mapSetting.DefaultRoom,Vector2.zero);
            
            for (int i = 0; i < mapSetting.MapCount; i++)
            {
                
            }
            
            return result;
        }

        private static void SetLoadingGauge(int current, int max)
            => LoadingEvents.RaiseSetLoadingGauge("Map Deployment", new Gauge(current,max));
        
    }
}