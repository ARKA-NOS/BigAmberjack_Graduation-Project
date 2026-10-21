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
            playerEffectUIs = new();

            if (playerEffectUIPrefab == null)
            {
                FDebug.LogError("[PlayerEffectUIBinder] playerEffectUIPrefab is null");
            }
        }

        protected override void DataUpdate()
        {
            if (CurrentValue == null)
            {
                FDebug.LogError("[PlayerEffectUIBinder] EffectList is null");
                return;
            }
            
            List<EffectViewData> effectList = CurrentValue.ViewDataList;

            SecuringEffectUI(effectList.Count);

            UpdateUI(effectList);
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

        private void UpdateUI(List<EffectViewData> effectList)
        {
            for(int i = 0; i < playerEffectUIs.Count;i++)
            {
                if (i < playerEffectUIs.Count)
                {
                    playerEffectUIs[i].gameObject.SetActive(true);
                    playerEffectUIs[i].SetViewData(effectList[i]);
                }
                else
                {
                    playerEffectUIs[i].gameObject.SetActive(false);
                }
            }
            
        }
        
    }
}