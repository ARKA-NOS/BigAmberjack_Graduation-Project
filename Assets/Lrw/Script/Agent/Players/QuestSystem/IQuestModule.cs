namespace Lrw.Script.Agent.Players.QuestSystem
{
    public interface IQuestModule
    {
        Quest AddQuest(QuestSo questSo);
        void AddQuestAmount(QuestKey key, float amount);
        void SetQuestAmount(QuestKey key, float amount);
    }
}