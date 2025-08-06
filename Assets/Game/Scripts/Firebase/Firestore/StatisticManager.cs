#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Board;
using Firebase.Extensions;
using Firebase.Firestore;
using Google;
using Setting;
using Statistics;
using UnityEngine;
using UnityEngine.Events;

public class StatisticManager
{
    private FirebaseFirestore _db;
    private const string CollectionName = "Statistics";
    
    private FirestoreManager _firestoreManager;
    public StatisticManager(FirebaseFirestore db, FirestoreManager firestoreManager)
    {
        _db = db;
        _firestoreManager = firestoreManager;
    }

    public async void GetPlayerStatistic(string playerId, UnityAction<string, PlayerStatistic> callback = null)
    {
        try
        {
            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                callback?.Invoke(playerId, snapshot.ConvertTo<PlayerStatistic>());
                return;
            }

            Debug.LogWarning($"Statistics not found for player {playerId}");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error getting player statistics: {e.Message}");
        }
    }

    public async Task<bool> CreatePlayerStatistics(string playerId)
    {
        try
        {
            var now = DateTime.UtcNow;
            var initialStats = new PlayerStatistic
            {
                RegistrationDate = Timestamp.FromDateTime(now),
                LastPlayedDate = Timestamp.FromDateTime(now),
                
                CurrentEloRating = 500,
                LowestEloRating = 500,
                PeakEloRating = 500,
                
                // Всі інші поля за замовчуванням 0
            };

            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            await docRef.SetAsync(initialStats);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error creating player statistics: {e.Message}");
            return false;
        }
    }


    public void UpdatePlayerStatistics(string playerId, PlayerData player, List<Move> history, EndGameType endGameType, WonReason wonReason)
    {
        if (_firestoreManager.MyData.ID != playerId)
        {
            Debug.LogError("Invalid attempt to update statistics, user unavailable for this action");
            return;
        }

        _firestoreManager.LoadStatistic(_firestoreManager.MyData.ID, (arg0, oldStatistic) =>
        {
            oldStatistic.UpdateStatistics(player, history, endGameType, wonReason);
            SavePlayerStatistics(playerId, oldStatistic)
                .ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError($"Error updating player statistics: {task.Exception}");
                }

                if (task.IsCompleted && task.Result)
                {
                    Debug.Log("Statistics updated successfully");
                }
            });
        });
    }
    
    private async Task<bool> SavePlayerStatistics(string playerId, PlayerStatistic updatedStats)
    {
        try
        {
            DocumentReference docRef = _db.Collection(CollectionName).Document(playerId);
            await docRef.SetAsync(updatedStats, SetOptions.MergeAll);
            return true;
        }
        catch (Exception e)
        {
            Debug.LogError($"Error updating player statistics: {e.Message}");
            return false;
        }
    }

    public void ReportAnotherPlayer(string id)
    {
        throw new NotImplementedException();
    }
}
#endif