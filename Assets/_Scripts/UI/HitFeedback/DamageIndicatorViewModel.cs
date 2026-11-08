using _Scripts.UI.BindSystem;
using UnityEngine;

namespace _Scripts.UI.HitFeedback
{
    // 상태를 매 프레임 읽는 체력 바와 달리, Bind 순간마다 복사해서 소비하는 표시 요청입니다.
    public readonly struct DamageIndicatorData
    {
        public readonly int SourceId;
        public readonly float Damage;
        public readonly bool IsCritical;
        public readonly Vector3 Position;

        public DamageIndicatorData(int sourceId, float damage, bool isCritical, Vector3 position)
        {
            SourceId = sourceId;
            Damage = damage;
            IsCritical = isCritical;
            Position = position;
        }
    }

    [CreateAssetMenu(fileName = "DamageIndicatorViewModel", menuName = "UI/Enemy/Damage Indicator View Model")]
    public sealed class DamageIndicatorViewModel : AbstractUIViewModel<DamageIndicatorData> { }
}
