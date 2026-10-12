using Lrw.Script._Core._Debug;
using Lrw.Script.Currency;
using Lrw.Script.Upgrade;
using Lrw.Script.Upgrade.SO;
using UnityEngine;
using UnityEngine.UI;

namespace Lrw.Script.UI.UpgradeUI
{
    public class TestUpgradeNodeButton : MonoBehaviour
    {
        [SerializeField] private UpgradeNodeSO upgradeNode;

        private Button _button;
        private UpgradeTree treeData;

        private void Awake()
        {
            _button = GetComponent<Button>();
            if (_button == null)
            {
                FDebug.LogError("Button is not found");
                return;
            }
            
            _button.onClick.AddListener(ButtonClick);
        }

        private void OnDestroy()
        {
            _button.onClick.RemoveListener(ButtonClick);
        }

        public void SetTree(UpgradeTree tree)
        {
            treeData = tree;
        }


        private void ButtonClick()
        {
            if (upgradeNode == null) return;
            if (treeData.IsUpgrade(upgradeNode)) return;
            
            foreach (Cost cost in upgradeNode.UpgradeCost)
            {
                if (!CurrencySystem.GetCurrencyData(cost.Type).Has(cost.Amount))
                {
                    return;
                }
            }
            
            foreach (Cost cost in upgradeNode.UpgradeCost)
            {
                CurrencySystem.GetCurrencyData(cost.Type).Add(-cost.Amount);
            }
            
            treeData.SetUpgrade(upgradeNode,true);
        }
        
        
    }
}