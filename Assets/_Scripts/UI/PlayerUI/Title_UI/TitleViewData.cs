using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI
{
    public struct TitleViewData
    {
        public const string LockedName = "???";

        // 칭호를 구분하는 값(칭호 에셋 이름). 잠긴 칭호는 이름이 모두 ???라 펼침 상태 등은 이 값으로 구분한다.
        public readonly string Key;
        public readonly string TitleName;
        public readonly string TitleDescription;
        public readonly string UpgradeDescription;
        public readonly Sprite Icon;
        public readonly List<QuestViewData> TitleGauge;
        // 도감 번호(1부터). 도감에 없는 칭호면 0.
        public readonly int Number;
        public readonly string MapLabel;
        // 달성 기록이 없어 이름·조건을 숨기는 칭호.
        public readonly bool IsLocked;
        public readonly TitleEntryStatus Status;

        // 이번 런 달성이거나 달성 기록이 있으면 강조 스타일로 그린다.
        public bool IsCompleted => Status is TitleEntryStatus.RunCompleted or TitleEntryStatus.Earned;

        // 달성한 칭호는 Title.Clear()로 퀘스트 목록이 비워지므로 진행도를 1로 본다.
        public float Progress => IsCompleted ? 1f : GetQuestsNormalize();

        public TitleViewData(string key, string titleName, string titleDescription, string upgradeDescription, Sprite icon,
            List<QuestViewData> titleGauge, int number, string mapLabel, bool isLocked, TitleEntryStatus status)
        {
            Key = key;
            TitleName = isLocked ? LockedName : titleName;
            TitleDescription = isLocked ? string.Empty : titleDescription;
            UpgradeDescription = isLocked ? string.Empty : upgradeDescription;
            Icon = isLocked ? null : icon;
            TitleGauge = titleGauge;
            Number = number;
            MapLabel = mapLabel;
            IsLocked = isLocked;
            Status = status;
        }

        private float GetQuestsNormalize()
            => TitleGauge == null || TitleGauge.Count == 0 ? 0f : TitleGauge.Sum(x => x.QuestGauge.Normalized) / TitleGauge.Count;
    }
}
