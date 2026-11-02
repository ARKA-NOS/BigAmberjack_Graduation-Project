using System;
using System.Collections.Generic;
using SDW.Scripts.Maps.Encounter;

namespace SDW.Scripts.Maps.Conditions
{
    // 방 안의 EncounterZone이 모두 끝나면 클리어된다.
    // 플레이어가 트리거를 밟아야 적이 나오는 방(보스 방 등)에서 쓴다.
    // KillAllEnemies는 적이 나오기 전에 "살아 있는 적 없음"으로 바로 클리어되므로 이 조건으로 구역 완료를 기다린다.
    public class ClearAllEncountersCondition : IRoomClearCondition
    {
        private readonly RoomController _room;
        private readonly List<EncounterZone> _pendingZones = new();

        public event Action Cleared;

        public ClearAllEncountersCondition(RoomController room)
        {
            _room = room;
        }

        public void Begin()
        {
            foreach (EncounterZone zone in _room.EncounterZones)
            {
                if (zone == null || zone.CurrentState == EncounterZone.State.Cleared)
                    continue;

                _pendingZones.Add(zone);
                zone.Cleared += HandleZoneCleared;
            }

            // 전투 구역이 없거나 이미 다 끝났으면 바로 클리어한다.
            if (_pendingZones.Count == 0)
                Cleared?.Invoke();
        }

        public void Dispose()
        {
            foreach (EncounterZone zone in _pendingZones)
            {
                if (zone != null)
                    zone.Cleared -= HandleZoneCleared;
            }

            _pendingZones.Clear();
        }

        private void HandleZoneCleared()
        {
            _pendingZones.RemoveAll(zone =>
            {
                if (zone == null || zone.CurrentState != EncounterZone.State.Cleared)
                    return false;

                zone.Cleared -= HandleZoneCleared;
                return true;
            });

            if (_pendingZones.Count == 0)
                Cleared?.Invoke();
        }
    }
}
