using System.Collections.Generic;
using System.Linq;

namespace Lrw.Script.UI.PlayerUI.Title_UI
{
    public struct TitleViewData
    {
        public readonly string TitleName;
        public readonly string TitleDescription;
        public readonly string UpgradeDescription;
        public readonly List<QuestViewData> TitleGauge;
        public readonly bool IsCompleted;

        // 달성한 칭호는 Title.Clear()로 퀘스트 목록이 비워지므로 진행도를 1로 본다.
        public float Progress => IsCompleted ? 1f : GetQuestsNormalize();

        public TitleViewData(string titleName, string titleDescription, string upgradeDescription, List<QuestViewData> titleGauge, bool isCompleted)
        {
            TitleName = titleName;
            TitleDescription = titleDescription;
            UpgradeDescription = upgradeDescription;
            TitleGauge = titleGauge;
            IsCompleted = isCompleted;
        }

        private float GetQuestsNormalize()
            => TitleGauge == null || TitleGauge.Count == 0 ? 0f : TitleGauge.Sum(x => x.QuestGauge.Normalized) / TitleGauge.Count;
    }
}