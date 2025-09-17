using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Board;
using Board.Piece;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.RealtimeDatabase;
using Game.Scripts.Board;
using Game.Scripts.Firebase.Firestore;
using Google;
using Statistics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class MyData
{
    public readonly string ID;
    public Sprite Icon;
    public readonly string Name;
    public UnityEvent OnIconLoaded = new ();

    public void GetPlayerRanking(UnityAction<PlayerRankingData> callback)
    {
        _backendManager.LoadPlayerRanking(ID, (arg0, ranking) =>
        {
            callback.Invoke(ranking);
        });
    }
    
    
    private readonly BackendManager _backendManager; 
    public MyData(string id, string name, Sprite icon,BackendManager backendManager)
    {
        ID = id;
        Name = name;
        Icon = icon;
        _backendManager = backendManager;
    }

    public bool FriendIdsContains(string playerDataID)
    {
       var myData = _backendManager.GetSavedPlayer(ID);
       return myData.PlayerData.Data.FriendIds.Contains(playerDataID);
    }

    public void SendFriendRequest(string recipientId)
    {
        _ = _backendManager.RealtimeDatabase.FriendRequestsManager.SendFriendRequest(recipientId, Name);
    }

    public void SendMatchRequest(string recipientId)
    {
        _ = _backendManager.RealtimeDatabase.MatchRequestsManager.SendMatchRequest(recipientId, Name);
    }
}
public class BackendManager
{
    private const string PlayersDataCollectionName = "Players";
    private const string NameKey = "Name";
    
    private FirebaseFirestore _db;
    private AdvancedMatchmaking _advancedMatchmaking;
    
    public MyData MyData;
    
    public StatisticManager StatisticManager;
    public HistoryManager HistoryManager;
    public PlayerDataManager PlayerDataManager;
    public PlayerRankingManager PlayerRankingManager;
    public RealtimeDatabase RealtimeDatabase;
    public RemoteConfigManager RemoteConfigManager;
    public readonly UnityEvent OnLogin = new UnityEvent();
    public readonly UnityEvent OnSignOut = new UnityEvent();
    public BackendManager(AdvancedMatchmaking advancedMatchmaking)
    {
        _advancedMatchmaking = advancedMatchmaking;
    }


    public async Task Init()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                _db = FirebaseFirestore.DefaultInstance;
#if UNITY_EDITOR
                if (_db.Settings.PersistenceEnabled) _db.Settings.PersistenceEnabled = false;
#endif
                Debug.Log("Firebase initialized successfully.");
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus);
            }

            await Task.Yield();
        }
        catch (Exception e)
        {
            Debug.LogError("error" + e);
            Console.WriteLine(e);
            throw;
        }

        StatisticManager = new StatisticManager(_db, this);
        HistoryManager = new HistoryManager(_db, this);
        PlayerDataManager = new PlayerDataManager(_db, this, PlayersDataCollectionName, NameKey);
        PlayerRankingManager = new PlayerRankingManager(_db, this);
        RemoteConfigManager = new RemoteConfigManager(_db, this);
    }

    private readonly Dictionary<string, SavedPlayerData> _savedPlayersData = new();

    public SavedPlayerData GetSavedPlayer(string targetPlayerId)
    {
        if (_savedPlayersData.TryGetValue(targetPlayerId, out var player)) return player;
        
        var newSavedPlayer = new SavedPlayerData(
            StatisticManager.LoadPlayerStatistic,
            PlayerDataManager.GetPlayerData,
            HistoryManager.LoadHistory,
            PlayerRankingManager.LoadRankings);
        
        _savedPlayersData.Add(targetPlayerId, newSavedPlayer);
        return newSavedPlayer;
    }

    public void LoadHistory(string targetPlayerId, UnityAction<string, List<HistoryMatchData>> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);

        player.History.Load(targetPlayerId, callback);
    }

    public void LoadOneHistory(string targetPlayerId, string historyID, UnityAction<HistoryMatchData> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);
        
        HistoryManager.LoadHistory(historyID, callback);
    }

    public void LoadPlayerData(string targetPlayerId, UnityAction<string, FirebasePlayerData> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);
        
        player.PlayerData.Load(targetPlayerId, callback);
    }

    public void LoadStatistic(string targetPlayerId, UnityAction<string, PlayerStatistic> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);
        
        player.Statistic.Load(targetPlayerId, callback);
    }
    
    public void LoadPlayerRanking(string targetPlayerId, UnityAction<string, PlayerRankingData> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);
        
        player.Ranking.Load(targetPlayerId, callback);
    }

    public void SaveMatchHistory(string winnerID,
        string player1ID, int player1Elo, ArrangementEntry[] arrangement1,
        string player2ID, int player2Elo, ArrangementEntry[] arrangement2,
        List<Move> moveHistory, UnityAction<string> historyDocId)
    {
        HistoryManager.SaveMatchHistory(winnerID, player1ID, player1Elo, arrangement1, player2ID, player2Elo, arrangement2, moveHistory,
            (string id) =>
            {
                PlayerDataManager.BdAddHistoryId(player1ID,id);
                PlayerDataManager.BdAddHistoryId(player2ID,id);
                historyDocId.Invoke(id);
            });
    }
    
    public async Task<string> GetPlayerName(string playerId)
    {
        string result = null;
        try
        {
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();
            if (snapshot.Exists) result = snapshot.GetValue<string>(NameKey);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch document for playerId '{playerId}': {ex.Message}");
        }

        return result;
    }
    
    public void Login(FirebasePlayerData playerData)
    {
        MyData = new MyData(playerData.ID, playerData.Name, playerData.Icon, this);
        var myID = MyData.ID;
        LoadHistory(myID, (arg0, arg1) => { });;
        LoadStatistic(myID, (arg0, arg1) => { });
        LoadPlayerData(myID, (arg0, firebasePlayerData) =>
        {
            MyData.Icon = firebasePlayerData.Icon;
        });
        RealtimeDatabase = new RealtimeDatabase(playerData.ID, PlayerDataManager.AddFriend, PlayerDataManager.RemoveFriend, _advancedMatchmaking);
        OnLogin?.Invoke();
        Debug.Log("Login success");
    }
}