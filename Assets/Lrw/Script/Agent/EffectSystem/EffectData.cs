using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    [CreateAssetMenu(fileName = "Effect Data", menuName = "Effect/Effect Data", order = 0)]
    public class EffectData : ScriptableObject
    {
        [field: SerializeField] public string EffectName { get; private set; }
        [field: SerializeField] public Sprite EffectIcon { get; private set; }
    }
}