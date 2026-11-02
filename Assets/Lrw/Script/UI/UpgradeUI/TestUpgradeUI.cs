using SDW.Scripts.UI.UpgradeUI;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core._ServiceLocator;
using Lrw.Script.Agent.Players;
using Lrw.Script.UI.Template;
using Lrw.Script.Upgrade;
using Lrw.Script.Upgrade.SO;
using UnityEngine;

namespace Lrw.Script.UI.UpgradeUI
{
    public class TestUpgradeUI : FadeWindow
    {
        [SerializeField] private TestUpgradeInfoPanel infoPanel;
        [SerializeField] private TestUpgradeLineDrawer lineDrawer;

        private UpgradeTree tree;
        private TestUpgradeNodeButton[] buttons;

        protected override void Awake()
        {
            base.Awake();
            buttons = GetComponentsInChildren<TestUpgradeNodeButton>();
            foreach (TestUpgradeNodeButton button in buttons)
            {
                button.Purchased += RefreshAll;
            }
        }

        protected virtual void OnDestroy()
        {
            foreach (TestUpgradeNodeButton button in buttons)
            {
                if (button != null) button.Purchased -= RefreshAll;
            }
        }

        public override void Open(object data)
        {
            base.Open(data);

            if (data == null)
            {
                FDebug.LogError("[UpgradeUI] data is null");
                return;
            }

            if (data is not UpgradeTreeSO treeSo)
            {
                FDebug.LogError("[UpgradeUI] data type is not UpgradeTreeSO");
                return;
            }

            ServiceLocator.Get<IPlayerController>().SetInputActive(false);

            tree = ServiceLocator.Get<IUpgradeManager>().GetUpgradeTree(treeSo.TreeName);

            if (infoPanel != null) infoPanel.Clear();

            foreach (TestUpgradeNodeButton button in buttons)
            {
                button.SetInfoPanel(infoPanel);
                button.SetTree(tree);
            }

            if (lineDrawer != null)
            {
                Canvas.ForceUpdateCanvases();
                lineDrawer.Draw(buttons);
            }
            RefreshAll();
        }

        private void RefreshAll()
        {
            foreach (TestUpgradeNodeButton button in buttons)
            {
                button.Refresh();
            }
            if (lineDrawer != null) lineDrawer.Refresh(tree);
        }

        public void RequestClose()
            => UIManagerEvents.OpenCloseWindow(null);

        public override void Close()
        {
            base.Close();
            ServiceLocator.Get<IPlayerController>().SetInputActive(true);
        }
    }
}
