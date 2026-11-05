using TMPro;
using UnityEngine;

namespace _Scripts.UI.InventoryUI.StatUI
{
    // 스탯 한 줄: 왼쪽 이름, 오른쪽 값
    public class StatRowUI : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI valueText;

        public void Set(string displayName, string value)
        {
            nameText.text = displayName;
            valueText.text = value;
        }
    }
}
