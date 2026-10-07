using UnityEngine;

namespace Lrw.Script.Currency
{
    [CreateAssetMenu(fileName = "Currency SO", menuName = "Currency/Currency SO", order = 0)]
    public class CurrencySO : ScriptableObject
    {
        [field:SerializeField] public string CurrencyName {get; private set;}
        [field:SerializeField] public Sprite CurrencyIcon { get; private set; }
        [field: SerializeField] public bool IsPermanent { get; private set; } = false;
        [field: SerializeField] public int baseCount = 0;
    }
}