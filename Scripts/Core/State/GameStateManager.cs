using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using Kuantech.Core;
using UnityEngine;

public class GameStateManager : SubManager
{
    [SerializeField] private float SaveCheckFrequency = 1f;
    [SerializeField] private bool AutoSave = true;
    [SerializeField] private string DefaultBinaryProviderId = "local";

    [SerializeReference] private List<DataStorageProvider> _storageProviders = new();

    private readonly Dictionary<string, DataStorageProvider> _providerById = new();
    private float _lastCheckTime;

    public override async UniTask Initialize(GameManager gameManager)
    {
        await base.Initialize(gameManager);

        _providerById.Clear();
        foreach (var provider in _storageProviders)
        {
            if (provider == null) continue;
            _providerById[provider.Id] = provider;
            provider.LoadData();
        }
    }

    protected virtual void LateUpdate()
    {
        if (!AutoSave) return;
        if (Time.time - _lastCheckTime < SaveCheckFrequency) return;
        _lastCheckTime = Time.time;
        FlushProviders();
    }

    private void OnApplicationQuit()
    {
        FlushProviders();
    }

    private void FlushProviders()
    {
        foreach (var provider in _storageProviders)
        {
            if (provider != null && provider.HasUnsavedChanges)
                provider.SaveChanges();
        }
    }

    // --- Provider registry ---

    public static T GetProvider<T>(string id) where T : DataStorageProvider
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null || string.IsNullOrEmpty(id)) return default;
        ctx._providerById.TryGetValue(id, out var provider);
        return provider as T;
    }

    public static DataStorageProvider GetProvider(string id)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null || string.IsNullOrEmpty(id)) return null;
        ctx._providerById.TryGetValue(id, out var provider);
        return provider;
    }

    // --- Binary convenience statics (used by SubManager.SaveState / LoadState) ---

    public static void UpdateSaveData(ISaveable saveable, string providerId = null)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return;
        var provider = GetProvider<BinaryStorageProvider>(providerId ?? ctx.DefaultBinaryProviderId);
        if (provider == null) return;
        provider.SaveData(saveable);
    }

    public static bool LoadData(ISaveable saveable, string providerId = null)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return false;
        var provider = GetProvider<BinaryStorageProvider>(providerId ?? ctx.DefaultBinaryProviderId);
        if (provider == null) return false;
        return provider.LoadData(saveable);
    }

    public static void ClearSaveData(ISaveable saveable, string providerId = null)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return;
        GetProvider<BinaryStorageProvider>(providerId ?? ctx.DefaultBinaryProviderId)?.ClearData(saveable);
    }

    [ConsoleMethod("clearState", "Clears all data in all providers")]
    public static void ClearStateSS()
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return;

        // 1. Clear all provider caches
        foreach (var provider in ctx._storageProviders)
            provider?.Clear();

        // 2. Flush immediately so disk is written before any SubManager re-saves
        ctx.FlushProviders();

        // 3. Reset in-memory state of all SubManagers
        GameManager.Instance.ResetAllSubManagerStates();
    }

    // --- POCO helpers (raw key-value binary storage) ---

    public static void SaveObject<T>(string key, T data, string providerId = null)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return;
        string id = providerId ?? ctx.DefaultBinaryProviderId;
        byte[] bytes = SaveUtility.SerializePoco(data);
        GetProvider<BinaryStorageProvider>(id)?.SaveRaw(key, bytes);
    }

    public static bool TryLoadObject<T>(string key, out T data, string providerId = null)
    {
        data = default;
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return false;
        string id = providerId ?? ctx.DefaultBinaryProviderId;
        var provider = GetProvider<BinaryStorageProvider>(id);
        if (provider == null || !provider.TryLoadRaw(key, out var bytes)) return false;
        data = SaveUtility.DeserializePoco<T>(bytes);
        return true;
    }

    public static void ClearObject(string key, string providerId = null)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null) return;
        string id = providerId ?? ctx.DefaultBinaryProviderId;
        GetProvider<BinaryStorageProvider>(id)?.ClearData(key);
    }
}
