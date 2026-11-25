using System;
using System.Collections.Generic;
using _Scripts._Core._Debug;
using _Scripts._Core._EventSystem;
using UnityEngine;
using Random = UnityEngine.Random;

namespace _Scripts.Maps.New_Map.BossMapSystem
{
    public class BossMapGenerator : MonoBehaviour
    {
        [SerializeField] private BossMapListSO bossMapListSO;
        
        private void Awake()
        {
            EventBus<BossMapCreateEvent>.Subscribe(HandleBossMapCreate);
        }

        private void OnDestroy()
        {
            EventBus<BossMapCreateEvent>.UnSubscribe(HandleBossMapCreate);
        }

        private BossMap _currentMap;
        
        private void HandleBossMapCreate(BossMapCreateEvent evt)
        {
            ClearGeneratedMap();
            if(!evt.Active)
                return;

            if (bossMapListSO == null)
            {
                FDebug.LogError("bossMapListSO is null", this);
                return;
            }
            
            List<BossMap> prefabs = bossMapListSO.Prefabs;

            if (prefabs == null || prefabs.Count == 0)
            {
                FDebug.LogError("Boss Map Setting Error", bossMapListSO);
                return;
            }
            
            int index = Random.Range(0, prefabs.Count);

            _currentMap = Instantiate(prefabs[index],transform);

            MapGenerateEvents.RaiseMapGenerated(index, null, _currentMap.StartPos, false);
        }
        
        private void ClearGeneratedMap()
        {
            if(_currentMap == null) return;
            Destroy(_currentMap.gameObject);
        }
        
        
    }
}