using Lrw.Script.Upgrade.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace SDW.Scripts.UI.UpgradeUI
{
    public class TestUpgradeInfoPanel : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text descriptionText;

        public void Show(UpgradeNodeSO node)
        {
            if (node == null)
            {
                Clear();
                return;
            }

            iconImage.sprite = node.NodeIcon;
            descriptionText.text = node.NodeDescription;
        }

        public void Clear()
        {
            iconImage.sprite = null;
            descriptionText.text = string.Empty;
        }
    }
}
