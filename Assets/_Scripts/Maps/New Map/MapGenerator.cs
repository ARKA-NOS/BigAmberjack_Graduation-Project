using System.Collections;
using _Scripts._Core._Debug;
using _Scripts.Loading;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private MapGenerateSetting mapGenerateSetting;
           
        #region MapGenerate Start

        private void Awake()
        {
            GenerateMap();
        }
        
        public void GenerateMap()
        {
            if (mapGenerateSetting == null)
            {
                FDebug.LogError("[MapGenerator] MapGenerateSetting is null");
                return;
            }
            
            StartCoroutine(GenerateMapCoroutine());
        }

        #endregion
        
        #region MapGenerate

        // LoadingEvents.RaiseSetLoadingGauge("SettingMapData", 50f, 100f);
        private async UniTaskVoid GenerateMapCoroutine()
        {
            Init();

            SettingMapData();

            InstantiatePrefabs();

            End();
        }

        private void Init()
        {
            
            
        }

        private void SettingMapData()
        {
            
        }
        
        //전부 생성하면 instantiateLoop을 false로 만들기
        private void InstantiatePrefabs()
        {
            
            
        }

        private void End()
        {
            
        }

        #endregion
        
    }
}