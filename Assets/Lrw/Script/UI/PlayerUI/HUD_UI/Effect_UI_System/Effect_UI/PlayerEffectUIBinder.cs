using System.Collections.Generic;
using Lrw.Script._Core._Debug;
using Lrw.Script.UI.BindSystem;
using UnityEngine;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System.Effect_UI
{
    public class PlayerEffectUIBinder : AbstractUIBinder<EffectList>
    {
        [SerializeField] private GameObject playerEffectUIPrefab;
        
        private List<PlayerEffectUI> playerEffectUIs;
        
        protected override void Awake()
        {
            base.Awake();
            
        }
        
        
        protected override void DataUpdate()
        {
            List<EffectViewData> effectList = CurrentValue.ViewDataList;

            SecuringEffectUI(effectList.Count);
        }

        private void SecuringEffectUI(int count)
        {
            for (int i = playerEffectUIs.Count; i < count; i++)
            {
                GameObject go = Instantiate(playerEffectUIPrefab, transform);
                FDebug.Assert(go.TryGetComponent(out PlayerEffectUI playerEffectUI),
                    "[PlayerEffectUIBinder] playerEffectUIPrefab is not have PlayerEffectUI");
                playerEffectUIs.Add(playerEffectUI);
            }
        }
        
    }
}