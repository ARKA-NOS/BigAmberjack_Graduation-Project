using DevLib.ModuleSystem;

namespace Lrw.Script.Agent.Players.QuestSystem
{
    public static class QuestManager
    {
        public static void AddAmount(ModuleOwner owner, QuestKey questSo, float value)
        {
            if(!owner.TryGetModule(out IQuestModule questModule)) return;
            questModule.AddQuestAmount(questSo, value);
        }
    }
}