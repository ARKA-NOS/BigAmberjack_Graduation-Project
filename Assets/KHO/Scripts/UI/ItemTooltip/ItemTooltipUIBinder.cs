using System.Linq;
using KHO.Scripts.Eunms;
using KHO.Scripts.ItemSystem;
using Lrw.Script.UI.BindSystem;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace KHO.Scripts.UI.ItemTooltip
{
    // 상호작용 대상인 바닥 아이템 위에 아이콘·이름·등급/태그·설명·키 안내를 띄운다.
    // Screen Space - Overlay 캔버스 아래에 두고, 아이템의 월드 좌표를 화면 좌표로 바꿔 따라다닌다.
    public class ItemTooltipUIBinder : AbstractUIBinder<IItemTooltipSource>
    {
        [SerializeField] private RectTransform tooltip;
        [SerializeField] private Vector2 screenOffset = new(0f, 30f);

        [Header("Content")]
        [SerializeField] private Image iconImage;
        [SerializeField] private TextMeshProUGUI nameText;
        [SerializeField] private TextMeshProUGUI infoText;
        [SerializeField] private TextMeshProUGUI descriptionText;
        [SerializeField] private Image destroyHoldFill;

        private RectTransform _area;

        protected override void Awake()
        {
            base.Awake();
            _area = (RectTransform)tooltip.parent;
            tooltip.gameObject.SetActive(false);
        }

        private void Start()
        {
            OnDataChanged();
        }

        protected override void DataUpdate()
        {
            IItemTooltipSource source = CurrentValue;
            if (source == null || !tooltip.gameObject.activeSelf) return;

            FollowTarget(source.TooltipPosition);
            destroyHoldFill.fillAmount = source.DestroyProgress;
        }

        protected override void OnDataChanged()
        {
            ItemSO item = CurrentValue?.Item;
            tooltip.gameObject.SetActive(item != null);
            if (item == null) return;

            iconImage.enabled = item.Icon != null;
            iconImage.sprite = item.Icon;
            nameText.text = item.ItemName;
            nameText.color = GetGradeColor(item.Grade);
            infoText.text = BuildInfo(item);
            descriptionText.text = item.Description;
            destroyHoldFill.fillAmount = 0f;

            FollowTarget(CurrentValue.TooltipPosition);
        }

        private void FollowTarget(Vector2 worldPosition)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Vector2 screenPoint = cam.WorldToScreenPoint(worldPosition);
            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(_area, screenPoint, null, out Vector2 local)) return;

            tooltip.anchoredPosition = ClampInsideArea(local + screenOffset);
        }

        // 툴팁이 화면 밖으로 나가지 않게 한다. 툴팁 앵커는 영역 중앙이라 anchoredPosition은 중앙 기준 좌표다.
        private Vector2 ClampInsideArea(Vector2 position)
        {
            Rect area = _area.rect;
            Vector2 size = tooltip.rect.size;
            Vector2 pivot = tooltip.pivot;
            Vector2 center = area.center;

            float minX = area.xMin + size.x * pivot.x - center.x;
            float maxX = area.xMax - size.x * (1f - pivot.x) - center.x;
            float minY = area.yMin + size.y * pivot.y - center.y;
            float maxY = area.yMax - size.y * (1f - pivot.y) - center.y;

            return new Vector2(Mathf.Clamp(position.x, minX, maxX), Mathf.Clamp(position.y, minY, maxY));
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
