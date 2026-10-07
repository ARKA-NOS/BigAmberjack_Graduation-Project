using System.Collections.Generic;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._SaveSystem;
using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Currency
{
    public class CurrencyManager : AbstractManager, ICurrencyManager
    {
        [SerializeField] private CurrencySO[] currencies;
        
        private Dictionary<CurrencySO,CurrencyData> _currencyData;
        
        public override void Initialize()
        {
            _currencyData = new();
            InitCurrency();
            ServiceLocator.Register<ICurrencyManager>(this);
        }

        public override void GameEnd()
        {
            base.GameEnd();
            ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
            SaveCurrencies();
        }

        private void InitCurrency()
        {
            foreach (CurrencySO currency in currencies)
            {
                if (_currencyData.ContainsKey(currency))
                {
                    FDebug.LogWarning("[CurrencyManager] SO 중복");
                    continue;
                }
                
                _currencyData.Add(currency,GetSaveCurrency(currency));
            }
        }

        private CurrencyData GetSaveCurrency(CurrencySO currencySo)
        {
            CurrencyData baseData = new CurrencyData(currencySo, currencySo.baseCount);
            
            if(!currencySo.IsPermanent) return baseData;
            return SaveSystem.Load(currencySo.CurrencyName, baseData);
        }

        private void SaveCurrencies()
        {
            foreach (CurrencyData currencyData in _currencyData.Values)
            {
                if(!currencyData.currencyType.IsPermanent) continue;
                
                string currencyName = currencyData.currencyType.CurrencyName;
                
                bool saveComplete =
                    SaveSystem.Save(currencyName, currencyData);

                if (!saveComplete)
                {
                    FDebug.LogError($"Save Fail : {currencyName}");
                }
            }
        }
        
        public CurrencyData GetCurrency(CurrencySO currencySo)
            => _currencyData[currencySo];
        
    }
}