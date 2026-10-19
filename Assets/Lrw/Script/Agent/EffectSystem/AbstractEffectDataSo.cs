using DevLib.ModuleSystem;
using UnityEngine;

namespace Lrw.Script.Agent.EffectSystem
{
    //[CreateAssetMenu(fileName = "??? Effect", menuName = "Effect/??? Effect")]
    public abstract class AbstractEffectDataSo : ScriptableObject
    {
        [field:SerializeField] public EffectType Type { get; private set; }
        [field:SerializeField] public Sprite Sprite { get; private set; }
        
        [field:SerializeField] public string EffectName { get; private set; }
        [field:SerializeField,TextArea] public string EffectDescription { get; private set; }
        
        [field: SerializeField] public bool IsStackable { get; private set; } = false;
        
        public abstract IEffect CreateEffect(ModuleOwner owner);
    }
}