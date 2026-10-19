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
        private UpgradeTree tree;
        private TestUpgradeNodeButton[] buttons;

        protected override void Awake()
        {
            base.Awake();
            buttons = GetComponentsInChildren<TestUpgradeNodeButton>();
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

            foreach (TestUpgradeNodeButton button in buttons)
            {
                button.SetTree(tree);
            }
        }

        public override void Close()
        {
            base.Close();
            ServiceLocator.Get<IPlayerController>().SetInputActive(true);
        }
    }
}