using System;
using System.Collections.Generic;
using Kuantech.Utils;
using Sirenix.OdinInspector;
using UnityEngine;

namespace Kuantech.Core.Database
{
    public class BalanceManager : SubManager
    {
        [SerializeField] private KtDatabaseManager DatabaseManager;
        
        [Serializable]
        public struct BalanceEntry
        {
            public string DatabaseName;
            public string TableName;
            [SerializeReference] public Balancer Balancer;
        }
        
        public List<BalanceEntry> BalanceEntries = new List<BalanceEntry>();

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            BalanceAll();
        }
        
        [Button("Balance")]
        public void BalanceAll()
        {
            if (BalanceEntries.IsNullOrEmpty()) return;
            foreach (var entry in BalanceEntries)
            {
                KtDatabase db = DatabaseManager.GetDatabaseNonStatic(entry.DatabaseName);
                if (db == null || entry.Balancer == null) continue;
                entry.Balancer.Balance(db, entry.TableName);
            }
        }
    }
}