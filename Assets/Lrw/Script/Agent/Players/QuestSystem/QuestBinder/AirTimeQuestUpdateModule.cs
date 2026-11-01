using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // minAirTime초 이상 떠 있던 구간만, 착지할 때 그 구간 전체 시간(초)을 더한다.
    public class AirTimeQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField, Min(0f)] private float minAirTime = 1.5f;

        private IGroundChecker _groundChecker;
        private bool _isInAir;
        private float _airStartTime;

        protected override void OnInit(ModuleOwner owner)
        {
            FDebug.Assert(owner.TryGetModule(out _groundChecker),
                $"[AirTimeQuestUpdateModule : {owner.name}] IGroundChecker is not found");
        }

        private void FixedUpdate()
        {
            if (!IsInitialized) return;

            bool isGround = _groundChecker.FixedIsGround;
            if (!isGround && !_isInAir)
            {
                _isInAir = true;
                _airStartTime = Time.time;
            }
            else if (isGround && _isInAir)
            {
                _isInAir = false;
                float airTime = Time.time - _airStartTime;
                if (airTime >= minAirTime) AddQuestAmount(airTime);
            }
        }
    }
}
