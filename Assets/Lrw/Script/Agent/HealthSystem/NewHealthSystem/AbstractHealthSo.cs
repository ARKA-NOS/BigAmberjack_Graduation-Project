using DevLib.BattleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.HealthSystem.NewHealthSystem
{
    [CreateAssetMenu(fileName = "??? Health SO", menuName = "Health/??? Health SO", order = 0)]
    public abstract class AbstractHealthSo : ScriptableObject
    {
        [field: SerializeField] public Color HealthColor { get; private set; } = Color.red;
        
        [Header("설명 (RunTime 사용 X)")]
        [SerializeField,TextArea] private string healthDescription = "Health SO";
        
        public virtual HealthData ApplyDamage(HealthData data)
            => new HealthData(
                Mathf.Max(data.Damage - data.Health, 0f),
                Mathf.Max(data.Health - data.Damage, 0f)
                );
        
    }
}