using System.Collections.Generic;
using System.Threading;
using _Scripts._Core._Debug;
using _Scripts.Maps.New_Map.MapGenerateData;
using _Scripts.Maps.New_Map.MapSettingAsset;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public partial class MapGenerator : MonoBehaviour
    {
        [SerializeField] private MapGenerateSetting mapGenerateSetting;
        
        private MapGenerateSetting prevMapSetting;
        
        private void Awake()
        {
            GenerateMap(mapGenerateSetting);
        }
        
        #region MapGenerate
        
        public void GenerateMap(MapGenerateSetting mapSetting)
        {
            if (mapSetting == null)
            {
                FDebug.LogError("[MapGenerator] MapGenerateSetting is null");
                return;
            }
            
            GenerateMapUniTask(mapSetting).Forget();
        }

        // 
        private async UniTaskVoid GenerateMapUniTask(MapGenerateSetting mapSetting)
        {
            CancellationToken ct = this.GetCancellationTokenOnDestroy();
            
            await UnLoadAssets(ct);
            
            await LoadAssets(mapSetting, ct);
            
            List<RoomDeploymentData> roomDeploymentDataList = await MapDeployment(mapSetting, ct);
            
            await InstantiatePrefabs(roomDeploymentDataList, ct);
            
            End(mapSetting);
        }

        #region Addressable

        private async UniTask UnLoadAssets(CancellationToken ct)
        {
            if(prevMapSetting == null) return;
            
            await UniTask.Yield(ct);
            // 나중에
        }

        private async UniTask LoadAssets(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            await UniTask.Yield(ct);
            // 나중에
        }

        #endregion
        
        private async UniTask InstantiatePrefabs(List<RoomDeploymentData> roomDataList, CancellationToken ct)
        {
            await UniTask.SwitchToMainThread(ct);
            
            List<UniTask> taskList = new List<UniTask>();
            
            foreach (RoomDeploymentData roomDeploymentData in roomDataList)
            {
                var task = InstantiateRoomAsync(roomDeploymentData, ct);
                taskList.Add(task);
            }

            await UniTask.WhenAll(taskList);
        }

        private async UniTask<Room> InstantiateRoomAsync(RoomDeploymentData roomData, CancellationToken ct)
        {
            var op = InstantiateAsync(roomData.RoomPrefab, transform, roomData.Position, Quaternion.identity);
            
            using (ct.Register(op.Cancel))
            {
                await op.ToUniTask(cancellationToken: ct);
            }
            
            return op.Result != null && op.Result.Length > 0 ? op.Result[0] : null;
        }
        
        private void End(MapGenerateSetting mapSetting)
        {
            prevMapSetting = mapSetting;
        }

        #endregion
        
    }
}