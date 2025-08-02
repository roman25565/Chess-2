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
    
    private string _myId;
    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);

    
    private FirebaseFirestore _db;
    private FirestoreManager _firestoreManager;
    
    public PlayerDataManager(FirebaseFirestore db, FirestoreManager firestoreManager ,string playersDataCollectionName, string nameKey, string myId)
    {
        _db = db;
        _firestoreManager = firestoreManager;
        PlayersDataCollectionName = playersDataCollectionName;
        NameKey = nameKey;
        _myId = myId;
    }
    
    public void AddFriend(string friendId)
    {
        var docRef = _db.Collection(PlayersDataCollectionName).Document(_myId);
        
        docRef.UpdateAsync(FriendIdsKey, FieldValue.ArrayUnion(friendId))
            .ContinueWithOnMainThread(task =>
            {
                if (task.IsCompleted)
                    Debug.Log("HistoryMatchIDs updated successfully.");
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
                await Task.Yield(); //Optimization
                var email = snapshot.GetValue<string>(EmailKey);
                var historyIds = snapshot.GetValue<List<string>>(HistoryIDsKey);
                var friendIds = snapshot.GetValue<List<string>>(FriendIdsKey);

                Debug.Log("Load From DB");
                GlobalTools.LoadSprite(new Uri(imageURL), sprite =>
                {
                    result = new FirebasePlayerData(playerId, existingName, existingElo, sprite, email, historyIds, friendIds);
                    callback(result);
                });
            }
            else
            {
                Debug.Log("snapshot NOT.Exists:");
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

    public void SingUp(string testId)
    {
        var player = new Dictionary<string, object>
        {
            { IDKey, testId },
            { NameKey, "BUGAGAGA" },
            { EloKey, 500 },
            {
                IconURLKey,
                "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no"
            },
            { EmailKey, "test@gmail.com" },
            {HistoryIDsKey, new object[]{} },
            {FriendIdsKey, new object[]{} }
        };

        SingUp(player, testId);
    }


    public void SingUp(GoogleSignInUser user)
    {
        Debug.LogError($"user.DisplayName: {user.DisplayName}, user.Email: {user.Email}, user.UserId: {user.UserId}, user.ImageUrl: {user.ImageUrl}");

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
        SingUp(player, user.UserId);
    }

    private void SingUp(Dictionary<string, object> playerData, string playerId)
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
                GlobalTools.LoadSprite(new Uri(playerData[IconURLKey].ToString()), sprite =>
                {
                    Debug.Log("Player SingUp successfully.");
                    var firebasePlayerData = new FirebasePlayerData
                    (
                        playerData[IDKey].ToString(),
                        playerData[NameKey].ToString(),
                        int.Parse(playerData[EloKey].ToString()),
                        sprite,
                        playerData[EmailKey].ToString(),
                        new List<string>(),
                        new List<string>()
                    );
                    _firestoreManager.Login(firebasePlayerData);
                });
            }

            return Task.CompletedTask;
        });
    }

    public void BdSetElo(string playerId, int newElo)
    {
        var docRef = _db.Collection(PlayersDataCollectionName).Document(playerId);

        var updates = new Dictionary<string, object>
        {
            { EloKey, newElo }
        };

        docRef.UpdateAsync(updates).ContinueWithOnMainThread(task =>
        {
            if (task.IsCompleted)
                Debug.Log("Document updated successfully.");
            else if (task.IsFaulted) Debug.LogError("Error updating document: " + task.Exception);
        });
    }
}
}