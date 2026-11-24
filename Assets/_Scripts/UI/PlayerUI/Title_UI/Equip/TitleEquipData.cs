using System;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.Title_UI.Equip
{
    // 목록 필터.
    public enum TitleEquipTab
    {
        All = 0,      // 전체
        Equipped = 1, // 장착 중
        Held = 2,     // 보관 중 (이번 런에 달성했지만 장착하지 않음)
        Missing = 3,  // 미획득 (이번 런에 달성하지 못함. 이전 런 기록이 있어도 포함)
    }

    // 칭호 한 줄의 해금 상태.
    public enum TitleEquipState
    {
        Owned,    // 이번 런에 달성. 장착할 수 있다.
        Recorded, // 이전 런에서만 달성. 이름과 설명은 보이지만 장착할 수 없다.
        Locked,   // 한 번도 달성하지 못함. 이름과 설명을 ???로 가린다.
    }

    // 필터 탭에 보여 줄 개수.
    public readonly struct TitleEquipTabCounts
    {
        public readonly int All;
        public readonly int Equipped;
        public readonly int Held;
        public readonly int Missing;

        public TitleEquipTabCounts(int all, int equipped, int held, int missing)
        {
            All = all;
            Equipped = equipped;
            Held = held;
            Missing = missing;
        }

        public int Get(TitleEquipTab tab) => tab switch
        {
            TitleEquipTab.Equipped => Equipped,
            TitleEquipTab.Held => Held,
            TitleEquipTab.Missing => Missing,
            _ => All,
        };
    }

    // 목록 위에 보여 줄 요약(이번 런 달성 / 이전 런 기록만 / 전체).
    public readonly struct TitleEquipSummary
    {
        public readonly int Owned;
        public readonly int Recorded;
        public readonly int Total;

        public TitleEquipSummary(int owned, int recorded, int total)
        {
            Owned = owned;
            Recorded = recorded;
            Total = total;
        }
    }

    // 목록의 칭호 한 줄. OnSelect를 부르면 이 칭호가 선택된다.
    public readonly struct TitleEquipRowData
    {
        public readonly string Key;
        public readonly string Number;
        public readonly string Name;
        public readonly TitleEquipState State;
        public readonly bool IsEquipped;
        public readonly bool IsSelected;
        public readonly Action OnSelect;

        public TitleEquipRowData(string key, string number, string name, TitleEquipState state, bool isEquipped,
            bool isSelected, Action onSelect)
        {
            Key = key;
            Number = number;
            Name = name;
            State = state;
            IsEquipped = isEquipped;
            IsSelected = isSelected;
            OnSelect = onSelect;
        }
    }

    // 선택한 칭호의 상세. 해금하지 않은 칭호(Recorded, Locked)는 CanToggle이 false다.
    public sealed class TitleEquipDetailData
    {
        public string Key { get; }
        public string Name { get; }
        public string Meta { get; }
        public string Condition { get; }
        public string Effect { get; }
        public Sprite Icon { get; }
        public TitleEquipState State { get; }
        public bool IsEquipped { get; }
        public string ButtonLabel { get; }
        public bool CanToggle { get; }
        public Action OnToggle { get; }

        public TitleEquipDetailData(string key, string name, string meta, string condition, string effect, Sprite icon,
            TitleEquipState state, bool isEquipped, string buttonLabel, bool canToggle, Action onToggle)
        {
            Key = key;
            Name = name;
            Meta = meta;
            Condition = condition;
            Effect = effect;
            Icon = icon;
            State = state;
            IsEquipped = isEquipped;
            ButtonLabel = buttonLabel;
            CanToggle = canToggle;
            OnToggle = onToggle;
        }
    }
}
