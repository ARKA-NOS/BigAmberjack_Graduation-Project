using System.Linq;
using KHO.Scripts.Eunms;
using KHO.Scripts.ItemSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.ItemTooltip
{
    // 툴팁 패널 하나. 아이콘·이름·등급/태그·설명을 채우고, 부모 영역 밖으로 나가지 않게 위치를 잡는다.
    // 바닥 아이템 툴팁과 인벤토리 칸 툴팁이 같이 쓴다.
    [RequireComponent(typeof(RectTransform))]
    public class ItemTooltipView : MonoBehaviour
    {
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI descriptionText;

        private RectTransform _rect;

        public RectTransform Rect => _rect != null ? _rect : _rect = (RectTransform)transform;
        public RectTransform Area => (RectTransform)Rect.parent;
        public bool IsShown => gameObject.activeSelf;

        public void Show(ItemSO item)
        {
            gameObject.SetActive(item != null);
            if (item == null) return;

            iconImage.enabled = item.Icon != null;
            iconImage.sprite = item.Icon;
            nameText.text = item.ItemName;
            nameText.color = GetGradeColor(item.Grade);
            infoText.text = BuildInfo(item);
            descriptionText.text = item.Description;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        // position은 부모(Area) 기준 로컬 좌표. 툴팁 앵커는 부모 중앙이라 중앙 기준으로 바꿔 넣는다.
        public void MoveTo(Vector2 position)
        {
            Rect area = Area.rect;
            Vector2 size = Rect.rect.size;
            Vector2 pivot = Rect.pivot;
            Vector2 center = area.center;

            float minX = area.xMin + size.x * pivot.x;
            float maxX = area.xMax - size.x * (1f - pivot.x);
            float minY = area.yMin + size.y * pivot.y;
            float maxY = area.yMax - size.y * (1f - pivot.y);

            Vector2 clamped = new(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
            Rect.anchoredPosition = clamped - center;
        }

        private static string BuildInfo(ItemSO item)
        {
            string grade = GetGradeName(item.Grade);
            string[] tags = item.ItemTags.Where(tag => tag != Tags.None).Select(tag => tag.ToString()).ToArray();
            return tags.Length == 0 ? grade : $"{grade}  |  {string.Join(", ", tags)}";
        }

        private static string GetGradeName(Grade grade) => grade switch
        {
            Grade.Rare => "희귀",
            Grade.Unique => "유니크",
            Grade.Legendary => "전설",
            _ => "일반",
        };

        private static Color GetGradeColor(Grade grade) => grade switch
        {
            Grade.Rare => new Color32(0x5A, 0xA9, 0xF0, 0xFF),
            Grade.Unique => new Color32(0xB9, 0x7A, 0xF2, 0xFF),
            Grade.Legendary => new Color32(0xF2, 0xC1, 0x4E, 0xFF),
            _ => new Color32(0xE1, 0xE6, 0xF0, 0xFF),
        };
    }
}
