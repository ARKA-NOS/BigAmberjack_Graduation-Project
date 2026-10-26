namespace Lrw.Script.Agent.Players.QuestSystem
{
    public interface IQuestModule
    {
        void AddQuestStack(QuestSo questType, float value);
        Quest GetQuest(QuestSo questType);
    }
}