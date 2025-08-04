using System;
using System.Collections.Generic;
using Game.Scripts.Firebase.Firestore;
using UnityEngine.Events;

namespace Statistics
{
public class SavedPlayerData
{
    public PlayerHistoryLoader History;
    public PlayerStatisticLoader Statistic;
    public PlayerDataLoader PlayerData;
    
    
    public SavedPlayerData(
        Action<string, UnityAction<string, PlayerStatistic>> getPlayerStatistic,
        Action<string, UnityAction<string, FirebasePlayerData>> getPlayerData,
        Action<string, UnityAction<HistoryMatchData>> getHistory)
    {
        Statistic = new PlayerStatisticLoader(getPlayerStatistic);
        PlayerData = new PlayerDataLoader(getPlayerData);
        History = new PlayerHistoryLoader(this,getHistory);
    }
}

public abstract class SavedPlayerDataLoader<T>
{
    private readonly Action<string, UnityAction<string, T>> _loadMethod;
    public bool IsLoading { get; protected set; }
    public UnityEvent<string, T> OnLoaded { get; } = new();
    public T Data { get; protected set; }
    
    protected SavedPlayerDataLoader(Action<string, UnityAction<string, T>> loadMethod)
    {
        _loadMethod = loadMethod;
    }

    protected bool TryGet(string playerId, UnityAction<string, T> callback)
    {
        if (Data != null)
        {
            callback?.Invoke(playerId, Data);
            return true;
        }
        return false;
    }

    public virtual void Load(string playerId, UnityAction<string, T> callback)
    {
        if (TryGet(playerId, callback)) return;

        OnLoaded.AddListener((id, history) =>
        {
            callback?.Invoke(id, history);
            OnLoaded.RemoveListener(callback);
        });
        if (IsLoading) return;
        
        IsLoading = true;
        _loadMethod(playerId, (id, result) =>
        {
            Data = result;
            IsLoading = false;
            OnLoaded?.Invoke(id, result);
        });
    }
}

public class PlayerStatisticLoader : SavedPlayerDataLoader<PlayerStatistic>
{
    public PlayerStatisticLoader(Action<string, UnityAction<string, PlayerStatistic>> loadMethod) 
        : base(loadMethod) { }
}

public class PlayerHistoryLoader : SavedPlayerDataLoader<List<HistoryMatchData>>
{
    private SavedPlayerData _thisData;
    private Action<string, UnityAction<HistoryMatchData>> _loadHistoryMethod;
    public PlayerHistoryLoader(
        SavedPlayerData thisData,
        Action<string, UnityAction<HistoryMatchData>> loadHistoryMethod)
        : base(null)
    {
        _thisData = thisData;
        _loadHistoryMethod = loadHistoryMethod;
    }
    public override void Load(string playerId, UnityAction<string, List<HistoryMatchData>> callback)
    {
        if (TryGet(playerId, callback)) return;

        OnLoaded.AddListener((id, history) =>
        {
            callback?.Invoke(id, history);
        });
        if (IsLoading) return;

        IsLoading = true;
        _thisData.PlayerData.Load(playerId, (id, playerData) =>
        {
            foreach (var historyMatchID in playerData.HistoryMatchIDs)
            {
                List<HistoryMatchData> history = new();
                _loadHistoryMethod(historyMatchID, (data =>
                {
                    history.Add(data);
                    Data = history;
                    OnLoaded?.Invoke(id, history);
                    if (history.Count == playerData.HistoryMatchIDs.Count)
                    {
                        IsLoading = false;
                        OnLoaded?.RemoveListener(callback);
                    }
                }));
            }
        });
    }
}

public class PlayerDataLoader : SavedPlayerDataLoader<FirebasePlayerData>
{
    public PlayerDataLoader(Action<string, UnityAction<string, FirebasePlayerData>> loadMethod)
        : base(loadMethod) { }
}
}