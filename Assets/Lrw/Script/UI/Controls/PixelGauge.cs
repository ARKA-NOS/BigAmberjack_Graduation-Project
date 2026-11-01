using UnityEngine;
using UnityEngine.UIElements;

namespace Lrw.Script.UI.Controls
{
    /// <summary>
    /// 0~1 비율만큼 채워지는 게이지.
    /// 비율만 UXML 속성(value)이나 코드로 넣고, 색·크기·테두리는 USS로 꾸민다.
    /// 구조: .pixel-gauge(트랙) > .pixel-gauge__fill(채움)
    /// </summary>
    [UxmlElement]
    public partial class PixelGauge : VisualElement
    {
        public const string UssClassName = "pixel-gauge";
        public const string FillUssClassName = UssClassName + "__fill";

        private readonly VisualElement _fill;
        private float _value;

        [UxmlAttribute]
        public float Value
        {
            get => _value;
            set
            {
                _value = Mathf.Clamp01(value);
                _fill.style.width = Length.Percent(_value * 100f);
            }
        }

        public PixelGauge()
        {
            AddToClassList(UssClassName);

            _fill = new VisualElement { pickingMode = PickingMode.Ignore };
            _fill.AddToClassList(FillUssClassName);
            Add(_fill);

            Value = 0f;
        }
    }
}
