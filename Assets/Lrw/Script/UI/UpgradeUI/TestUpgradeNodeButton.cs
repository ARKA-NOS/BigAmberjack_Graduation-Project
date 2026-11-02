using SDW.Scripts.UI.UpgradeUI;
using System;
using Lrw.Script._Core._Debug;
using Lrw.Script.Currency;
using Lrw.Script.Upgrade;
using Lrw.Script.Upgrade.SO;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

namespace Lrw.Script.UI.UpgradeUI
{
    [RequireComponent(typeof(CanvasGroup))]
    public class TestUpgradeNodeButton : MonoBehaviour, IPointerEnterHandler
    {
        private const float DimAlpha = 0.5f;
        private static readonly Color LockedColor = new Color(0.35f, 0.35f, 0.35f, 1f);

        [SerializeField] private UpgradeNodeSO upgradeNode;

        private Button _button;
        private CanvasGroup _canvasGroup;
        private UpgradeTree treeData;
        private TestUpgradeInfoPanel infoPanel;

        public event Action Purchased;

        public UpgradeNodeSO Node => upgradeNode;

        private void Awake()
        {
            _canvasGroup = GetComponent<CanvasGroup>();
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
            Refresh();
        }

        public void SetInfoPanel(TestUpgradeInfoPanel panel)
        {
            infoPanel = panel;
        }

        public void Refresh()
        {
            if (treeData == null || upgradeNode == null) return;

            bool purchased = treeData.IsUpgrade(upgradeNode);
            bool locked = !purchased && !treeData.CanUpgrade(upgradeNode);

            _canvasGroup.alpha = purchased ? 1f : DimAlpha;
            _button.targetGraphic.color = locked ? LockedColor : Color.white;
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            ShowInfo();
        }

        private void ShowInfo()
        {
            if (infoPanel != null) infoPanel.Show(upgradeNode);
        }

        private void ButtonClick()
        {
            if (upgradeNode == null) return;
            ShowInfo();
            if (!treeData.CanUpgrade(upgradeNode))
            {
                FDebug.Log($"[UpgradeUI] {upgradeNode.NodeName} 구매 불가 (이미 구매했거나 선행 노드 미충족 / 3택1 선택됨)");
                return;
            }

            foreach (Cost cost in upgradeNode.UpgradeCost)
            {
                if (!CurrencySystem.GetCurrencyData(cost.Type).Has(cost.Amount))
                {
                    FDebug.Log($"[UpgradeUI] {upgradeNode.NodeName} 구매 불가 (재화 부족: {cost.Type} {cost.Amount})");
                    return;
                }
            }

            foreach (Cost cost in upgradeNode.UpgradeCost)
            {
                CurrencySystem.GetCurrencyData(cost.Type).Add(-cost.Amount);
            }

            treeData.SetUpgrade(upgradeNode,true);
            FDebug.Log($"[UpgradeUI] {upgradeNode.NodeName} 구매 완료");
            Purchased?.Invoke();
        }
    }
}
