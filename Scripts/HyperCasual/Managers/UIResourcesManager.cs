using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace Kuantech.Core.HyperCasual.UI
{
    [Serializable]
    public struct CurrencyIconsData
    {
        public string CurrencyId;
        public Sprite CurrencyIcon;
    }

    public class UIResourcesManager : SubManager
    {
        public List<CurrencyIconsData> CurrencyIconsData;
        private Dictionary<string, Sprite> _currencyIconsMap;

        public async override UniTask Initialize(GameManager parentManager)
        {
            _currencyIconsMap = new Dictionary<string, Sprite>();
            foreach(var data in CurrencyIconsData)
            {
                _currencyIconsMap[data.CurrencyId] = data.CurrencyIcon;
            }
            await base.Initialize(parentManager);
        }    

        public static Sprite GetCurrencyIcon(string currencyId)
        {
            UIResourcesManager context = UIResourcesManager.GetContext<UIResourcesManager>();
            if(context == null) 
            {
                Debug.LogError("Context is null");
                return null;
            }
            if(context._currencyIconsMap == null || !context._currencyIconsMap.ContainsKey(currencyId)) return null;
            return context._currencyIconsMap[currencyId];
        }
    } 
}