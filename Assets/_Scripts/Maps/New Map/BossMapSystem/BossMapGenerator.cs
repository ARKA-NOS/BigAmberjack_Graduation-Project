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

            BossMap map = Instantiate(prefabs[index]);

            MapGenerateEvents.RaiseMapGenerated(index, null, map.StartPos, false);
        }
        
        private void ClearGeneratedMap()
        {
            Transform trm = gameObject.transform;
            while (trm.childCount > 0) 
                Destroy(trm.GetChild(0).gameObject);
        }
        
        
    }
}