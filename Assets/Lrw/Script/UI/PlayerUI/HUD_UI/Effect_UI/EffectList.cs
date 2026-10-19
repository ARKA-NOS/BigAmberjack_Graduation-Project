using System;
using System.Collections.Generic;
using System.Linq;
using Lrw.Script.Agent.EffectSystem;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI
{
    public class EffectList
    {
        private readonly List<EffectViewData> viewDataList;
        private Dictionary<AbstractEffectDataSo, List<IEffect>> effects;
        
        public EffectList(Dictionary<AbstractEffectDataSo, List<IEffect>> dict)
        {
            effects = dict;
            viewDataList = new();
            Update();  
        }

        public void Update()
        {
            viewDataList.Clear();
            AbstractEffectDataSo[] effectDataList = effects.Keys.ToArray();
            Array.Sort(effectDataList,CheckPriority);
            foreach (AbstractEffectDataSo effectData in effectDataList)
            {
                
                //다른 값 추가 필요
                EffectViewData viewData = new EffectViewData
                {
                    EffectName = effectData.EffectName,
                    EffectDescription = effectData.EffectDescription,
                };
                viewDataList.Add(viewData);
            }
        }

        private int CheckPriority(AbstractEffectDataSo x, AbstractEffectDataSo y)
            => x.Priority > y.Priority ? x.Priority : y.Priority;
    }
}