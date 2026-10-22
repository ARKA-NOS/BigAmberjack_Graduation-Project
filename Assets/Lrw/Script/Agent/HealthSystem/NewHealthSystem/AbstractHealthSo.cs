using DevLib.ModuleSystem;
using Lrw.Script.Agent.StatSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    //[CreateAssetMenu(fileName = "??? Health SO", menuName = "Health/??? Health SO")]
    public abstract class AbstractHealthSo : ScriptableObject
    {
        [field: Header("표시")]
        [field: SerializeField] public string HealthName { get; private set; }
        [field: SerializeField] public Color HealthColor { get; private set; } = Color.white;
        [field: SerializeField] public Sprite Icon { get; private set; }
        [field: SerializeField] public HealthTag Tags { get; private set; }

        [field: Header("클수록 데미지를 먼저 받는다 (기본 체력은 항상 마지막)")]
        [field: SerializeField] public int Priority { get; private set; }

        [field: Header("최대치 (MaxStat이 있으면 스탯 값, 없으면 FixedMax)")]
        [field: SerializeField] public StatData MaxStat { get; private set; }
        [field: SerializeField, Min(0f)] public float FixedMax { get; private set; } = 100f;
        [field: Tooltip("최대치가 늘어나면 늘어난 만큼 회복한다")]
        [field: SerializeField] public bool HealOnMaxIncrease { get; private set; } = true;
        [field: Tooltip("생성될 때 최대치의 몇 배로 시작할지")]
        [field: SerializeField, Range(0f, 1f)] public float StartRatio { get; private set; } = 1f;

        [field: Header("회복 / 수명")]
        [field: Tooltip("이 출처의 회복만 받는다")]
        [field: SerializeField] public HealSourceType HealSources { get; private set; } = HealSourceType.All;
        [field: Tooltip("비면 제거한다 (기본 체력에는 적용 안 됨)")]
        [field: SerializeField] public bool RemoveWhenEmpty { get; private set; }
        [field: Tooltip("0이면 무한 (기본 체력에는 적용 안 됨)")]
        [field: SerializeField, Min(0f)] public float Duration { get; private set; }

        public abstract AbstractHealth CreateHealth(ModuleOwner owner);
    }
}
