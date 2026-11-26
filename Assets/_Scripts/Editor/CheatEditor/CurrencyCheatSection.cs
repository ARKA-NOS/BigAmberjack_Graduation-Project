using System.Linq;
using _Scripts._Core._ServiceLocator;
using _Scripts.Currency;
using UnityEditor;
using UnityEngine;

namespace _Scripts.Editor.CheatEditor
{
    public class CurrencyCheatSection : ICheatSection
    {
        private readonly CurrencySO[] _currencies = AssetDatabase.FindAssets($"t:{nameof(CurrencySO)}")
            .Select(AssetDatabase.GUIDToAssetPath)
            .Select(AssetDatabase.LoadAssetAtPath<CurrencySO>)
            .ToArray();

        private int _amount = 100;

        public string Title => "재화";

        public void Draw()
        {
            ICurrencyManager manager = ServiceLocator.Get<ICurrencyManager>();

            _amount = Mathf.Max(0, EditorGUILayout.IntField("수량", _amount));

            bool hasCurrency = false;
            foreach (CurrencySO currency in _currencies)
            {
                CurrencyData data = manager?.GetCurrency(currency);
                if (data == null) continue;

                hasCurrency = true;
                using (new EditorGUILayout.HorizontalScope())
                {
                    EditorGUILayout.LabelField(currency.CurrencyName, data.Count.ToString());

                    if (GUILayout.Button("지급", GUILayout.Width(50f)))
                        data.Add(_amount);
                    if (GUILayout.Button("차감", GUILayout.Width(50f)))
                        data.Add(-Mathf.Min(_amount, data.Count));
                }
            }

            if (!hasCurrency)
                EditorGUILayout.LabelField("CurrencyManager에 등록된 재화가 없습니다.");
            else
                EditorGUILayout.HelpBox("영구 재화는 플레이를 끝낼 때 저장됩니다.", MessageType.None);
        }

        public void Tick() { }

        public void Release() { }
    }
}
