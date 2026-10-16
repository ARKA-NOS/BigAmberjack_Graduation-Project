using Lrw.Script.Agent.EffectSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI
{
    [CreateAssetMenu(fileName = "Skill View Module", menuName = "Skill View Module", order = 0)]
    public class EffectViewModule : ScriptableObject
    {
        private AbstractEffectSO _skill;
        
        public static EffectViewModule CreateViewModule()
        {
            EffectViewModule viewModule = CreateInstance<EffectViewModule>();
            
            return viewModule;
        }
    }
}