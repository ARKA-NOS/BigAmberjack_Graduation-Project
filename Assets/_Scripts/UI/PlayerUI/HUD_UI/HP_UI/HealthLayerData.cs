using System;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.HP_UI
{
    // 체력 한 겹을 그리는 데 필요한 값. HealthViewModel이 겹마다 하나씩 만든다.
    // ObservableList가 바뀐 겹만 알리도록 값 비교(IEquatable)를 구현한다.
    public readonly struct HealthLayerData : IEquatable<HealthLayerData>
    {
        public Color BarColor { get; }
        public Color TextColor { get; }
        public float Current { get; }
        public float Max { get; }

        // true면 "현재 / 최대", false면 "+현재"로 표시한다.
        public bool ShowMax { get; }

        // 남은 체력이 있는 겹만 보여준다.
        public bool IsVisible => Current > 0f;

        public HealthLayerData(Color barColor, Color textColor, float current, float max, bool showMax)
        {
            BarColor = barColor;
            TextColor = textColor;
            Current = current;
            Max = max;
            ShowMax = showMax;
        }

        public bool Equals(HealthLayerData other)
            => BarColor == other.BarColor && TextColor == other.TextColor
               && Current.Equals(other.Current) && Max.Equals(other.Max) && ShowMax == other.ShowMax;

        public override bool Equals(object obj) => obj is HealthLayerData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(BarColor, TextColor, Current, Max, ShowMax);
    }
}
