using System;
using System.Collections.Generic;
using Game.Scripts.Firebase.Firestore;
using UnityEngine;
using UnityEngine.Events;

namespace Statistics
{
public class SavedPlayerData
{
    public PlayerHistoryLoader History;
    public PlayerStatisticLoader Statistic;
    public PlayerDataLoader PlayerData;
    public PlayerRankingLoader Ranking;
    
    
    public SavedPlayerData(
        Action<string, UnityAction<string, PlayerStatistic>> getPlayerStatistic,
        Action<string, UnityAction<string, FirebasePlayerData>> getPlayerData,
        Action<string, UnityAction<HistoryMatchData>> getHistory,
        Action<string, UnityAction<string, PlayerRankingData>> getRanking)
    {
        Statistic = new PlayerStatisticLoader(getPlayerStatistic);
        PlayerData = new PlayerDataLoader(getPlayerData);
        History = new PlayerHistoryLoader(this,getHistory);
        Ranking = new PlayerRankingLoader(getRanking);
    }
}

public abstract class SavedPlayerDataLoader<T>
{
    private readonly Action<string, UnityAction<string, T>> _loadMethod;
    public bool IsLoading { get; protected set; }
    public UnityEvent<string, T> OnLoaded { get; } = new();
    public T Data { get; protected set; }

    public bool IsOutdated;
    
    protected SavedPlayerDataLoader(Action<string, UnityAction<string, T>> loadMethod)
    {
        _loadMethod = loadMethod;
    }

    protected bool TryGet(string playerId, UnityAction<string, T> callback)
    {
        if (IsOutdated) return false;
        if (Data == null) return false;
        
        callback?.Invoke(playerId, Data);
        return true;
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
            IsOutdated = false;
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
            List<HistoryMatchData> history = new();
            foreach (var historyMatchID in playerData.HistoryMatchIDs)
            {
                _loadHistoryMethod(historyMatchID, (data =>
                {
                    history.Add(data);
                    Data = history;
                    OnLoaded?.Invoke(id, history);
                    Debug.Log("History loaded" + history.Count + " / " + playerData.HistoryMatchIDs.Count);
                    if (history.Count == playerData.HistoryMatchIDs.Count)
                    {
                        Debug.Log("History loaded Finish");
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

public class PlayerRankingLoader : SavedPlayerDataLoader<PlayerRankingData>
{
    public PlayerRankingLoader(Action<string, UnityAction<string, PlayerRankingData>> loadMethod)
        : base(loadMethod) { }
}
}