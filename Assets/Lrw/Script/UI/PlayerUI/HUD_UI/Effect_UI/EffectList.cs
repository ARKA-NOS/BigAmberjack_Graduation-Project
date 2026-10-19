using System.Collections.Generic;
using Lrw.Script.Agent.EffectSystem;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI
{
    public class EffectList
    {
        public Dictionary<AbstractEffectDataSo, List<IEffect>> Effects { get; private set; }
        
        public EffectList(Dictionary<AbstractEffectDataSo, List<IEffect>> dict)
        {
            Effects = dict;
        }
        
    }
}