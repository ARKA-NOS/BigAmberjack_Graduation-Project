using System;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.PlayerUI.HUD_UI.Effect_UI_System.Effect_UI
{
    public class PlayerEffectUI : MonoBehaviour
    {
        [SerializeField] private Image effectImage;
        [SerializeField] private Image normalTimeImage;
        
        private EffectViewData effectData;

        private void Awake()
        {
            UpdateUI();
        }

        public void SetViewData(EffectViewData viewData)
        {
            effectData = viewData;
            UpdateUI();
        }

        private void UpdateUI()
        {
            
        }
        
        
    }
}