#if !UNITY_SERVER
using System;
using System.Threading.Tasks;
using Firebase.Firestore;
using Statistics;
using UnityEngine;
using UnityEngine.Events;

public class StatisticManager
{
    private FirebaseFirestore _db;
    private const string CollectionName = "Statistics";
    public StatisticManager(FirebaseFirestore db)
    {
        _db = db;
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

    public async Task<bool> UpdatePlayerStatistics(string playerId, PlayerStatistic updatedStats)
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