using _Scripts._Core._EventSystem;
using _Scripts._Core._Manager;
using UnityEngine;

namespace _Scripts.Maps.New_Map
{
    public class MapManager : AbstractManager
    {
        [SerializeField] private uint bossMapCycle = 5;
        
        private int _currentCount;
        
        public override void Initialize()
        {
            _currentCount = 0;
            EventBus<MapMoveEvent>.Subscribe(HandleMapMove);
        }
        
        public override void GameEnd()
        {
            EventBus<MapMoveEvent>.Subscribe(HandleMapMove);
        }
        
        private void HandleMapMove(MapMoveEvent evt)
        {
            _currentCount++;
            if (_currentCount != bossMapCycle)
            {
                EventBus<MapGeneratedEvent>.Raise(new MapGeneratedEvent());
            }
            else
            {
                EventBus<BossMapCreate>.Raise(new BossMapCreate());
                _currentCount = 0;
            }
        }
        
    }
}