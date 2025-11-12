using System;
using Cysharp.Threading.Tasks;
using IngameDebugConsole;
using Kuantech.Core;
using UnityEngine;

public class GameStateManager : SubManager
{
    public GameState GameState { get; private set; }

    [SerializeField] private float SaveCheckFrequency = 1f;
    [SerializeField] private bool SaveData = true;
    private float _lastCheckTime;

    public override async UniTask Initialize(GameManager gameManager)
    {
        await base.Initialize(gameManager);
        GameState = new GameState();
        await GameState.LoadData();
    }

    protected virtual void LateUpdate()
    {
        if (GameState == null || !GameState.Dirtied) return;
        if (Time.time - _lastCheckTime < SaveCheckFrequency) return;
        if (SaveData)
        {
            GameState.SaveData();
            GameState.Dirtied = false;
        }
        _lastCheckTime = Time.time;
    }

    private void OnApplicationQuit()
    {
        if (SaveData && GameState != null) GameState.SaveData();
    }


    public static void UpdateSaveData(ISaveable saveable)
    {
        var ctx = GameStateManager.GetContext<GameStateManager>();
        if (ctx == null || ctx.GameState == null || saveable == null) return;

        string id = GetSaveableId(saveable);
        var data = SaveUtility.Serialize(saveable);
        ctx.GameState.UpdateData(id, data);
    }
    
    /// <summary>
    /// Tries to load state. Returns true if there exist any data to load
    /// </summary>
    /// <param name="saveable"></param>
    /// <returns></returns>
    public static bool LoadData(ISaveable saveable)
    {
        try
        {
            var ctx = GameStateManager.GetContext<GameStateManager>();
            if (ctx == null)
            {
                Debug.LogError("Game State Manager is null");
                return false;
            }
            if (ctx.GameState == null || saveable == null) return false;
            string id = GetSaveableId(saveable);
            byte[] data = ctx.GameState.GetData(id);
            if (data == null) return false;
            SaveUtility.Deserialize(data,saveable);
            return true;
        }
        catch (Exception e)
        {
            return false;
        }
    }

    public override void ClearState()
    {
        var ctx = GetContext<GameStateManager>();
        ctx.GameState.ClearAllData();
    }

    [ConsoleMethod("clearState", "Clears game state")]
    public static void ClearStateSS()
    {
        var ctx = GameStateManager.GetContext<GameStateManager>();
        if (ctx == null) return;
        ctx.ClearState();
    }
    
    public static void ClearSaveData(ISaveable saveable)
    {
        string id = GetSaveableId(saveable);
        ClearSaveData(id);
    }
    public static void ClearSaveData(string id)
    {
        var ctx = GetContext<GameStateManager>();
        ctx.GameState.ClearData(id);
    }
    
    private static string GetSaveableId(ISaveable module)
    {
        return module.GetType().FullName;
    }
    
    #region POCO
    public static void SaveObject<T>(string id, T data)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null || ctx.GameState == null) return;
        byte[] bytes = SaveUtility.SerializePoco(data);
        ctx.GameState.UpdateData(id, bytes); // Dirtied otomatik true olmalı (senin GameState içinde)
    }

    public static bool TryLoadObject<T>(string id, out T data)
    {
        data = default;
        var ctx = GetContext<GameStateManager>();
        if (ctx == null || ctx.GameState == null) return false;

        byte[] bytes = ctx.GameState.GetData(id);
        if (bytes == null) return false;

        data = SaveUtility.DeserializePoco<T>(bytes);
        return true;
    }

    public static void ClearObject(string id)
    {
        var ctx = GetContext<GameStateManager>();
        if (ctx == null || ctx.GameState == null) return;
        ctx.GameState.ClearData(id);
    }
    #endregion
}