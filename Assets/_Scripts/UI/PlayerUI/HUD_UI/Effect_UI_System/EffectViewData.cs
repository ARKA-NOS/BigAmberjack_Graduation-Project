using System;
using _Scripts.DataStructure;
using UnityEngine;

namespace _Scripts.UI.PlayerUI.HUD_UI.Effect_UI_System
{
    // 효과 아이콘 하나를 그리는 데 필요한 값. EffectViewModel이 효과 데이터마다 하나씩 만든다.
    // ObservableList가 바뀐 칸만 알리도록 값 비교(IEquatable)를 구현한다.
    public struct EffectViewData : IEquatable<EffectViewData>
    {
        public Sprite EffectIcon;
        // 가장 먼저 끝나는 효과의 남은 시간 / 지속 시간. 시간 제한이 없으면 0 / 0.
        public Gauge EffectGauge;
        public int EffectStack;
        public string EffectName;
        public string EffectDescription;

        public bool Equals(EffectViewData other)
            => EffectIcon == other.EffectIcon
               && EffectGauge.CurrentValue.Equals(other.EffectGauge.CurrentValue)
               && EffectGauge.MaxValue.Equals(other.EffectGauge.MaxValue)
               && EffectStack == other.EffectStack
               && EffectName == other.EffectName
               && EffectDescription == other.EffectDescription;

        public override bool Equals(object obj) => obj is EffectViewData other && Equals(other);

        public override int GetHashCode()
            => HashCode.Combine(EffectIcon, EffectGauge.CurrentValue, EffectGauge.MaxValue, EffectStack, EffectName, EffectDescription);
    }
}
