using System.Collections.Generic;
using Lrw.Script._Core._Debug;
using Lrw.Script._Core._Manager;
using Lrw.Script._Core._SaveSystem;
using Lrw.Script._Core._SaveSystem.Data;
using Lrw.Script._Core._ServiceLocator;
using UnityEngine;

namespace Lrw.Script.Currency
{
    public class CurrencyManager : AbstractManager, ICurrencyManager
    {
        [SerializeField] private CurrencySO[] currencies;
        
        private Dictionary<CurrencySO,CurrencyData> _currencyData;

        private const string CurrencySaveName = "CurrencyData";
        
        public override void Initialize()
        {
            _currencyData = new();
            LoadCurrency();
            ServiceLocator.Register<ICurrencyManager>(this);
        }

        public override void GameEnd()
        {
            base.GameEnd();
            ServiceLocator.Register<ICurrencyManager>(new NullCurrencyManager());
            SaveCurrencies();
        }

        private void LoadCurrency()
        {
            Dictionary<string, int> dict = new();
            
            JsonDict<string, int> jsonDict = new(dict);
            jsonDict = SaveSystem.Load(CurrencySaveName, jsonDict);
            
            jsonDict.AddTo(dict);
            
            foreach (CurrencySO currency in currencies)
            {
                if(currency == null) continue;
                if (_currencyData.ContainsKey(currency))
                {
                    FDebug.LogWarning("[CurrencyManager] SO 중복");
                    continue;
                }
                
                _currencyData.Add(currency,GetSaveCurrency(dict,currency));
            }
        }

        private CurrencyData GetSaveCurrency(Dictionary<string,int> dict,CurrencySO currencySo)
        {
            if(!currencySo.IsPermanent) return new CurrencyData(currencySo, currencySo.baseCount);
            
            int saveCount = dict.GetValueOrDefault(currencySo.CurrencyName, currencySo.baseCount);
            return new CurrencyData(currencySo, saveCount);
        }

        private void SaveCurrencies()
        {
            Dictionary<string,int> dict = new();
            
            foreach (CurrencyData currencyData in _currencyData.Values)
            {
                if(!currencyData.CurrencyType.IsPermanent) continue;
                
                string currencyName = currencyData.CurrencyType.CurrencyName;
                int count = currencyData.Count;
                
                dict.Add(currencyName, count);
            }
            
            bool saveComplete
                = SaveSystem.Save(CurrencySaveName, new JsonDict<string,int>(dict));

            if (!saveComplete)
                FDebug.LogError($"[CurrencyManager] SaveFail");
            
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