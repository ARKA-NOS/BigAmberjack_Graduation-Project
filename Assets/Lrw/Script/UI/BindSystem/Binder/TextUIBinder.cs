using TMPro;
using UnityEngine;

namespace Lrw.Script.UI.BindSystem.Binder
{
    [RequireComponent(typeof(TextMeshProUGUI))]
    public class TextUIBinder : AbstractUIBinder<string>
    {
        private TextMeshProUGUI _tmp;
        
        protected override void Awake()
        {
            base.Awake();
            _tmp = GetComponent<TextMeshProUGUI>();
        }

        protected override void DataUpdate()
        {
            _tmp.text = CurrentValue;
        }
        
    }
}