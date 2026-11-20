using System;
using _Scripts.Agent.StatSystem;

namespace _Scripts.UI.InventoryUI.StatUI
{
    // 스탯 하나의 표시 값. 연결 전이거나 플레이어에게 없는 스탯이면 HasValue가 false다.
    public readonly struct StatValueData : IEquatable<StatValueData>
    {
        public static readonly StatValueData None = default;

        public bool HasValue { get; }
        public float Value { get; }
        // 기본값에서 바뀐 양(장비·효과 등). 0이면 변화 없음.
        public float Modify { get; }

        public StatValueData(float value, float modify)
        {
            HasValue = true;
            Value = value;
            Modify = modify;
        }

        public static StatValueData From(Stat stat) => new(stat.Value, stat.ModifyValue);

        public bool Equals(StatValueData other)
            => HasValue == other.HasValue && Value.Equals(other.Value) && Modify.Equals(other.Modify);

        public override bool Equals(object obj) => obj is StatValueData other && Equals(other);

        public override int GetHashCode() => HashCode.Combine(HasValue, Value, Modify);
    }
}
