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
        
        [SerializeReference]
        public List<Balancer> Balancers = new List<Balancer>();

        public override void OnSubmanagersInitialized()
        {
            base.OnSubmanagersInitialized();
            BalanceAll();
        }
        
        [Button("Balance")]
        public void BalanceAll()
        {
            if (Balancers.IsNullOrEmpty()) return;
            foreach (var balancer in Balancers)
            {
                balancer.Balance();
            }
        }
    }
}