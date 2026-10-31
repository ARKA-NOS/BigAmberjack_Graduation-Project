using DevLib.ModuleSystem;
using Lrw.Script._Core._Debug;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.Players.QuestSystem.QuestBinder
{
    // 스탯 값을 그대로 퀘스트 수치로 쓴다(퀘스트 목표 = 기준값).
    // 비활성 스테이지에서는 진행도가 무시되므로 이벤트 대신 매 프레임 현재 값을 넣는다.
    public class StatThresholdQuestUpdateModule : AbstractQuestUpdateModule
    {
        [SerializeField] private StatData statData;

        private Stat _stat;

        protected override void OnInit(ModuleOwner owner)
        {
            FDebug.Assert(statData != null, $"[StatThresholdQuestUpdateModule : {name}] statData is null");
            FDebug.Assert(owner.TryGetModule(out IStatModule statModule),
                $"[StatThresholdQuestUpdateModule : {owner.name}] IStatModule is not found");

            _stat = statModule.GetStat(statData);
        }

        private void Update()
        {
            if (!IsInitialized) return;
            SetQuestAmount(_stat.Value);
        }
    }
}
