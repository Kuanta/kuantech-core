using Cysharp.Threading.Tasks;
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
        if (ctx.GameState == null || saveable == null) return;

        string id = GetSaveableId(saveable);
        var data = SaveUtility.Serialize(saveable);
        ctx.GameState.UpdateData(id, data);
    }
    
    public static void LoadData(ISaveable saveable)
    {
        var ctx = GameStateManager.GetContext<GameStateManager>();
        if (ctx == null)
        {
            Debug.LogError("Game State Manager is null");
        }
        if (ctx.GameState == null || saveable == null) return;
        string id = GetSaveableId(saveable);
        byte[] data = ctx.GameState.GetData(id);
        if (data == null) return;
        SaveUtility.Deserialize(data,saveable);
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
}