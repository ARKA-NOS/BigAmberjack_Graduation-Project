using System.Threading;
using _Scripts._Core._Debug;
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
            
            await MapDeployment(mapSetting, ct);
            
            await InstantiatePrefabs(mapSetting, ct);
            
            End(mapSetting);
        }

        #region Addressable

        private async UniTask UnLoadAssets(CancellationToken ct)
        {
            if(prevMapSetting == null) return;
            
            // 나중에
        }

        private async UniTask LoadAssets(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            // 나중에
        }

        #endregion
        
        private async UniTask InstantiatePrefabs(MapGenerateSetting mapSetting, CancellationToken ct)
        {
            
            
        }
        
        private void End(MapGenerateSetting mapSetting)
        {
            prevMapSetting = mapSetting;
        }

        #endregion
        
    }
}