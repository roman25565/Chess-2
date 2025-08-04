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
using Game.Scripts.Firebase.Firestore;
using Google;
using Statistics;
using Unity.Mathematics;
using UnityEngine;
using UnityEngine.Events;

public class FirestoreManager
{
    private const string PlayersDataCollectionName = "Players";
    private const string NameKey = "Name";
    
    private FirebaseFirestore _db;
    private AdvancedMatchmaking _advancedMatchmaking;

    public FirebasePlayerData PlayerData;
    public StatisticManager StatisticManager;
    public HistoryManager HistoryManager;
    public PlayerDataManager PlayerDataManager;
    public RealtimeDatabase RealtimeDatabase;
    public readonly UnityEvent OnLogin = new UnityEvent();
    public FirestoreManager(AdvancedMatchmaking advancedMatchmaking)
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

        StatisticManager = new StatisticManager(_db);
        HistoryManager = new HistoryManager(_db, this);
        PlayerDataManager = new PlayerDataManager(_db, this, PlayersDataCollectionName, NameKey);

    }


    private Dictionary<string, SavedPlayerData> _savedPlayers = new();

    private SavedPlayerData GetSavedPlayer(string targetPlayerId)
    {
        if (_savedPlayers.TryGetValue(targetPlayerId, out var player)) return player;
        
        var newSavedPlayer = new SavedPlayerData(StatisticManager.GetPlayerStatistic, PlayerDataManager.GetPlayerData, HistoryManager.LoadHistory);
        _savedPlayers.Add(targetPlayerId, newSavedPlayer);
        return newSavedPlayer;
    }

    public void LoadHistory(string targetPlayerId, UnityAction<string, List<HistoryMatchData>> callback)
    {
        var player = GetSavedPlayer(targetPlayerId);

        player.History.Load(targetPlayerId, callback);
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

    public void SaveMatchHistory(string winnerID,
        string player1ID, int player1Elo, ArrangementEntry[] arrangement1,
        string player2ID, int player2Elo, ArrangementEntry[] arrangement2,
        List<Move> moveHistory)
    {
        HistoryManager.SaveMatchHistory(winnerID, player1ID, player1Elo, arrangement1, player2ID, player2Elo, arrangement2, moveHistory,
            (string id) =>
            {
                PlayerDataManager.BdAddHistoryId(player1ID,id);
                PlayerDataManager.BdAddHistoryId(player2ID,id);
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
        PlayerData = playerData;
        RealtimeDatabase = new RealtimeDatabase(playerData.ID, PlayerDataManager.AddFriend, _advancedMatchmaking);
        PlayerDataManager.SetPlayerDataID(PlayerData.ID);
        OnLogin?.Invoke();
    }
}