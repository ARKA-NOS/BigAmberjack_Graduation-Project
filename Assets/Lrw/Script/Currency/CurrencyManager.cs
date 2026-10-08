using System.Collections.Generic;
using Lrw.Script._Core._Debug;
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
                if(currency == null) continue;
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
            CurrencySaveData baseLoadData = new CurrencySaveData(baseData);
            return SaveSystem.Load(currencySo.CurrencyName, baseLoadData).ChangeCurrency();
        }

        private void SaveCurrencies()
        {
            foreach (CurrencyData currencyData in _currencyData.Values)
            {
                if(!currencyData.CurrencyType.IsPermanent) continue;
                
                string currencyName = currencyData.CurrencyType.CurrencyName;

                CurrencySaveData saveData = new CurrencySaveData(currencyData);
                
                bool saveComplete =
                    SaveSystem.Save(currencyName, saveData);

                if (!saveComplete)
                {
                    FDebug.LogError($"Save Fail : {currencyName}");
                }
            }
        }

        [ContextMenu("Debug Currency")]
        private void DebugCurrency()
        {
            if (_currencyData == null)
            {
                FDebug.LogWarning("CurrencyManager is not Initialized");
                return;
            }
            
            foreach (CurrencyData currencyData in _currencyData.Values)
            {
                FDebug.Log($"{currencyData.CurrencyType.CurrencyName} : {currencyData.Count}");
            }
        }

        [ContextMenu("Cheat")]
        private void Cheat()
        {
            if (_currencyData == null)
            {
                FDebug.LogWarning("CurrencyManager is not Initialized");
                return;
            }
            
            foreach (CurrencyData currencyData in _currencyData.Values)
            {
                currencyData.Add(100);
            }
        }
        
        public CurrencyData GetCurrency(CurrencySO currencySo)
            => _currencyData.GetValueOrDefault(currencySo);
        
    }
}