using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Firestore;
using Statistics;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Firebase.Firestore
{
public class PlayerRankingManager
{
    private FirebaseFirestore _db;
    private const string CollectionName = "PlayersRankings";
    
    private FirestoreManager _firestoreManager;
    public PlayerRankingManager(FirebaseFirestore db, FirestoreManager firestoreManager)
    {
        _db = db;
        _firestoreManager = firestoreManager;
    }

    public async void LoadRankings(string playerId, UnityAction<string, PlayerRankingData> callback)
    {
        try
        {
            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                callback?.Invoke(playerId, snapshot.ConvertTo<PlayerRankingData>());
                return;
            }

            Debug.LogWarning($"Statistics not found for player {playerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting player statistics: {e.Message}");
        }
    }
    
    public async void UpdateMyPlayerRanking(string playerId, PlayerRankingData updatedStats)
    {
        if (playerId != _firestoreManager.MyData.ID)
        {
            Debug.LogError("Invalid attempt to update statistics, user unavailable for this action");
            return;
        }
        
        try
        {
            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            await docRef.SetAsync(updatedStats, SetOptions.MergeFields(nameof(PlayerRankingData.Elo)));
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating player statistics: {e.Message}");
        }
    }

    public async void CreateMyPlayerRanking(string playerId)
    {
        // if (playerId != _firestoreManager.MyData.ID) //TODO Firebase Settings
        // {
        //     Debug.LogError("Invalid attempt to create statistics, user unavailable for this action");
        //     return;
        // }
        
        var newData = new PlayerRankingData { Elo = 500, Position = -1 };
        try
        {
            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            await docRef.SetAsync(newData);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating player statistics: {e.Message}");
        }
    }
}
}