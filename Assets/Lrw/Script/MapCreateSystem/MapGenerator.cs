using System.Collections;
using Lrw.Script._Core._Debug;
using Lrw.Script.DataStructure;
using Lrw.Script.Loading;
using Lrw.Script.UI.BindSystem.ViewModel;
using UnityEngine;

namespace Lrw.Script.MapCreateSystem
{
    public class MapGenerator : MonoBehaviour
    {
        [SerializeField] private MapGenerateSetting mapGenerateSetting;
        
        [Header("UI Settings")]
        [SerializeField] private GaugeViewModel mapGenerateSettingViewModel;


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

        private bool instantiateLoop = false;
        private IEnumerator GenerateMapCoroutine()
        {
            Init();
            
            yield return LoadingEvents.RaiseSetLoadingGauge("Init", 25f, 100f);

            SettingMapData();
            
            yield return LoadingEvents.RaiseSetLoadingGauge("SettingMapData", 50f, 100f);

            instantiateLoop = true;
            
            while (instantiateLoop)
            {
                InstantiatePrefabs();
                
                yield return LoadingEvents.RaiseSetLoadingGauge("Prefab Instantiate",10f, 100f);
            }

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