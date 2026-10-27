namespace Lrw.Script.Agent.Players.QuestSystem
{
    public interface IQuestModule
    {
        Quest AddQuest(QuestSo questSo);
        void AddQuestAmount(QuestKey key, int amount);
    }
}