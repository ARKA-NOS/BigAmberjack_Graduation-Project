using _Scripts.DataStructure;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    public struct QuestViewData
    {
        public string QuestName;
        public string QuestDescription;
        public Gauge QuestGauge;
        public bool IsCompleted;

        public QuestViewData(string questName, string questDescription, Gauge questGauge, bool isCompleted)
        {
            QuestName =  questName;
            QuestDescription = questDescription;
            QuestGauge = questGauge;
            IsCompleted = isCompleted;
        }
        
    }
}