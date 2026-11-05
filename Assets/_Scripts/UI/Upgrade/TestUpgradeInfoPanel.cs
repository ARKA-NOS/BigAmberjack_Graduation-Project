using _Scripts.Upgrade.SO;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace _Scripts.UI.Upgrade
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
