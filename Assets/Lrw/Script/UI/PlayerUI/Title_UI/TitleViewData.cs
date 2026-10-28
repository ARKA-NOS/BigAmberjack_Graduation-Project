using System.Collections.Generic;

namespace Lrw.Script.UI.PlayerUI.Title_UI
{
    public class TitleViewData
    {
        public readonly string TitleName;
        public readonly string TitleDescription;
        public readonly string UpgradeDescription;
        public readonly List<QuestViewData> TitleGauge;
        
        public TitleViewData(string titleName, string titleDescription, string upgradeDescription, List<QuestViewData> titleGauge)
        {
            TitleName = titleName;
            TitleDescription = titleDescription;
            UpgradeDescription = upgradeDescription;
            TitleGauge = titleGauge;
        }
        
        
    }
}