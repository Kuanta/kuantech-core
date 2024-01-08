using System.Collections.Generic;
using Kuantech.ArcadeIdle;
using UnityEngine;

/// <summary>
/// Resource vault stores resources that are currency.
/// </summary>
public class ResourceWallet : MonoBehaviour
{
    private Dictionary<ResourceData, int> _resources;

    public void AddResource(ResourceData data, int amount)
    {
        if(_resources == null) _resources = new Dictionary<ResourceData, int>();

        if(_resources.ContainsKey(data))
        {
            _resources[data] += amount;
        }else{
            _resources[data] = amount;
        }
    }

    /// <summary>
    /// Checks if resource vault
    /// </summary>
    /// <param name="data"></param>
    /// <param name="amount"></param>
    /// <returns></returns>
    public bool HasAmount(ResourceData data, int amount)
    {
        if(_resources == null || !_resources.ContainsKey(data)) return false;
        if(_resources[data] >= amount) return true;
        return false;
    }
}