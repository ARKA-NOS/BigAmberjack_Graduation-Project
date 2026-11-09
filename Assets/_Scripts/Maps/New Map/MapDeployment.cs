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
                SetLoadingGauge("Map Deployment", deploymentCount, mapSetting.MapCount);
                await UniTask.Yield(ct);
            }
            
            List<RoomDeploymentData> list = await work;
            SetLoadingGauge("Map Deployment", mapSetting.MapCount, mapSetting.MapCount);
            return list;
        }
        
        private List<RoomDeploymentData> MapDeploymentInThread(MapGenerateSetting mapSetting)
        {
            List<RoomDeploymentData> result = new List<RoomDeploymentData>();
            
            Random random = new Random(mapSetting.Seed);
            
            
            
            
            
            
             
            
            return result;
        }
        
        
        #region Helper
        
        private static void SetLoadingGauge(string text, int current, int max)
            => LoadingEvents.RaiseSetLoadingGauge(text, new Gauge(current,max));
        
        private bool RectListOverlaps(List<RectInt> rect1, List<RectInt> rect2)
            => rect1.Any(i => rect2.Any(i.Overlaps));
        
        #endregion
        
    }
}