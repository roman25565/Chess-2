using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Board;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.RealtimeDatabase;
using Google;
using UnityEngine;
using UnityEngine.Events;

namespace Game.Scripts.Firebase.Firestore
{
public class PlayerDataManager
{

    private const string IDKey = "ID";
    private const string EloKey = "Elo";
    private const string IconURLKey = "IconURL";
    private const string EmailKey = "Email";
    private const string HistoryIDsKey = "HistoryIDs";
    private const string FriendIdsKey = "FriendIds";

    private string PlayersDataCollectionName;
    private string NameKey;
    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);

    
    private FirebaseFirestore _db;
    private FirestoreManager _firestoreManager;
    
    public PlayerDataManager(FirebaseFirestore db, FirestoreManager firestoreManager ,string playersDataCollectionName, string nameKey)
    {
        _db = db;
        _firestoreManager = firestoreManager;
        PlayersDataCollectionName = playersDataCollectionName;
        NameKey = nameKey;
    }
    
    public void AddFriend(string friendId)
    {
        Debug.Log("TryAdFriendId: " + friendId + "myId" + _firestoreManager.MyData.ID);
        var docRef = _db.Collection(PlayersDataCollectionName).Document(_firestoreManager.MyData.ID);
        
        docRef.UpdateAsync(FriendIdsKey, FieldValue.ArrayUnion(friendId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("Friend Added successfully.");
                else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
            });
    }
    
    public void BdAddHistoryId(string playerId, string historyId)
    {
        Debug.Log("playerId: " + playerId);
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
        
        docRef.UpdateAsync(HistoryIDsKey, FieldValue.ArrayUnion(historyId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("HistoryMatchIDs updated successfully.");
                else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
            });
    }

    public void GetPlayerData(string playerId, UnityAction<string, FirebasePlayerData> callback)
    {
        _ = GetPlayerData(playerId, (result =>
        {
            callback.Invoke(playerId, result);
        }));
    }
    public async Task<FirebasePlayerData> GetPlayerData(string playerId, GetPlayerDataCallBack callback)
    {
        FirebasePlayerData result = null;
        try
        {
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                Debug.Log("snapshot.Exists: ");
                var existingName = snapshot.GetValue<string>(NameKey);
                var existingElo = snapshot.GetValue<int>(EloKey);
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                var historyIds = snapshot.GetValue<List<string>>(HistoryIDsKey);
                var friendIds = snapshot.GetValue<List<string>>(FriendIdsKey);
                await Task.Yield(); //Optimization

                Debug.Log("Load From DB");
                GlobalTools.LoadSprite(new Uri(imageURL), sprite =>
                {
                    result = new FirebasePlayerData(playerId, existingName, existingElo, sprite, historyIds, friendIds);
                    callback(result);
                });
            }
            else
            {
                Debug.Log($"snapshot NOT.Exists: {PlayersDataCollectionName}, {playerId}");
                callback(null);
                return null;
            }
        }
        catch (Exception e)
        {
            Debug.LogError(playerId + e);
            throw;
        }
        return result;
    }
    
    public async void GetIcon(string playerId, Action<Sprite> action)
    {
        try
        {
            Debug.Log("Get Icon");
            var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
            var snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                GlobalTools.LoadSprite(new Uri(imageURL), action.Invoke);
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
    }

    public void OnSignInAnonymously(string imageUrl, UnityAction<string> callback)
    {
        var player = new Dictionary<string, object>
        {
            { IDKey, "<ID>" },
            { NameKey, "<ID>" },
            { EloKey, 500 },
            { IconURLKey, imageUrl },
            { EmailKey, "<EMAIL>" },
            {HistoryIDsKey, new object[]{} },
            {FriendIdsKey, new object[]{} }
        };

        _db.Collection(PlayersDataCollectionName).AddAsync(new Dictionary<string, object>()).ContinueWithOnMainThread((task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to SingUp player: " + task.Exception);
            }
            else
            {
                var id = task.Result.Id;
                var maxLength = id.Length / 2;
                var name = id.Substring(0, maxLength);
                
                player[IDKey] = id;
                player[NameKey] = name;
                var docRef = _db.Collection(PlayersDataCollectionName).Document(id);
                docRef.SetAsync(player).ContinueWithOnMainThread(updateTask =>
                {
                    if (updateTask.IsFaulted)
                    {
                        Debug.LogError("Failed to SingUp player: " + updateTask.Exception);
                    }
                    else
                    {
                        _ = _firestoreManager.StatisticManager.CreatePlayerStatistics(id);
                        callback.Invoke(id);
                    }

                    return Task.CompletedTask;
                });
                
            }
        }));
    }


    public void CreatePlayerData(GoogleSignInUser user)
    {
        Debug.Log($"CreatePlayerData user.DisplayName: {user.DisplayName}, user.Email: {user.Email}, user.UserId: {user.UserId}, user.ImageUrl: {user.ImageUrl}");
        
        var player = new Dictionary<string, object>
        {
            { IDKey, user.UserId },
            { NameKey, user.DisplayName },
            { EloKey, 500 },
            { IconURLKey, user.ImageUrl.ToString() },
            { EmailKey, user.Email },
            { HistoryIDsKey, new object[] { } },
            { FriendIdsKey, new object[] { } }
        };
        CreatePlayerData(player, user.UserId);
    }

    private void CreatePlayerData(Dictionary<string, object> playerData, string playerId)
    {
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);
        docRef.SetAsync(playerData).ContinueWithOnMainThread(setTask =>
        {
            if (setTask.IsFaulted)
            {
                Debug.LogError("Failed to SingUp player: " + setTask.Exception);
            }
            else
            {
                OnPlayerDataCreated(playerData);
            }

            return Task.CompletedTask;
        });
    }

    private void OnPlayerDataCreated(Dictionary<string, object> playerData, UnityAction<FirebasePlayerData> callback = null)
    {
        GlobalTools.LoadSprite(new Uri(playerData[IconURLKey].ToString()), sprite =>
        {
            Debug.Log("Player SingUp successfully.");
            var firebasePlayerData = new FirebasePlayerData
            (
                playerData[IDKey].ToString(),
                playerData[NameKey].ToString(),
                int.Parse(playerData[EloKey].ToString()),
                sprite,
                new List<string>(),
                new List<string>()
            );
            callback?.Invoke(firebasePlayerData);
            _firestoreManager.Login(firebasePlayerData);
        });
    }

    public void BdSetMyElo(string playerId, int newElo)
    {
        if (playerId != _firestoreManager.MyData.ID)
        {
            Debug.LogError("Invalid attempt to update statistics, user unavailable for this action");
            return;
        }
        
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);

        var updates = new Dictionary<string, object>
        {
            { EloKey, newElo }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
            {
                Debug.Log("Document updated successfully.");
                _firestoreManager.GetSavedPlayer(playerId).PlayerData.IsOutdated = true;
            }
            else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
        });
    }
}
}