using System.Collections;
using System.Threading.Tasks;
using _Scripts._Core._Debug;
using _Scripts.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public class MapGenerator : MonoBehaviour
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

        // LoadingEvents.RaiseSetLoadingGauge("SettingMapData", 50f, 100f);
        private async UniTaskVoid GenerateMapUniTask(MapGenerateSetting mapSetting)
        {
            await UnLoadAssets();
            
            await LoadAssets(mapSetting);
            
            await SettingMapData(mapSetting);
            
            await InstantiatePrefabs(mapSetting);
            
            await End(mapSetting);
        }

        #region Addressable

        private async UniTask UnLoadAssets()
        {
            if(prevMapSetting == null) return;
            
            // 나중에
        }

        private async UniTask LoadAssets(MapGenerateSetting mapSetting)
        {
            // 나중에
        }

        #endregion

        #region CreateMap
        
        private async UniTask SettingMapData(MapGenerateSetting mapSetting)
        {
            
        }
        
        private async UniTask InstantiatePrefabs(MapGenerateSetting mapSetting)
        {
            
            
        }
        
        #endregion
        
        private async UniTask End(MapGenerateSetting mapSetting)
        {
            prevMapSetting = mapSetting;
        }

        #endregion
        
    }
}