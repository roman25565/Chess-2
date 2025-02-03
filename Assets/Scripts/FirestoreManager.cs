using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
using Google;
using UnityEngine.Networking;

public class FirestoreManager
{
    private const string IDKey = "ID";
    private const string NameKey = "Name";
    private const string EloKey = "Elo";
    private const string IconURLKey = "IconURL";
    private const string EmailKey = "Email";
    
    private const string CollectionName = "Players";
    
    private FirebaseFirestore db;
    public FirebasePlayerData PlayerData;

    public async void Init()
    {
        try
        {
            var dependencyStatus = await FirebaseApp.CheckAndFixDependenciesAsync();
            if (dependencyStatus == DependencyStatus.Available)
            {
                db = FirebaseFirestore.DefaultInstance;
#if UNITY_EDITOR
                db.Settings.PersistenceEnabled = false;
#endif
                Debug.Log("Firebase initialized successfully.");
                // Call the method to add or fetch player data
                // AddOrFetchPlayerData("player123", "JohnDoe", 1200);
                
                // var id = "004";
                // var playerData = await GetPlayerData(id);
                // Debug.Log(playerData?.Elo);
                // if (playerData == null)
                // {
                //     SingUp(FirebasePlayerData.CreateFirebasePlayerData(id));
                // }
            }
            else
            {
                Debug.LogError("Could not resolve all Firebase dependencies: " + dependencyStatus.ToString());
            }
        }
        catch (Exception e)
        {
            Debug.LogError("error" + e);
            Console.WriteLine(e);
            throw;
        }
    }

    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);
    public async Task<FirebasePlayerData> GetPlayerData(string playerId,GetPlayerDataCallBack callback)
    {
        FirebasePlayerData result = null;
        try
        {
            var docRef = db.Collection(CollectionName).Document(playerId);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var existingName = snapshot.GetValue<string>(NameKey);
                var existingElo = snapshot.GetValue<int>(EloKey);
                var imageURL = snapshot.GetValue<string>(IconURLKey);
                var email = snapshot.GetValue<string>(EmailKey);
                Debug.Log("Load From DB");
                var ico = await GlobalTools.LoadSprite(new Uri(imageURL));
                result = new FirebasePlayerData(playerId, existingName, existingElo, ico, email);
            }
            else
            {
                result = null;
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to fetch document for playerId '{playerId}': {ex.Message}");
        }

        callback(result);
        return result;
    }

    public void SingUp(string testId)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, testId },
            { NameKey, "BUGAGAGA" },
            { EloKey, 500 },
            { IconURLKey, "https://lh3.googleusercontent.com/a/ACg8ocKRgsvyDUJoW7yokTHMnHLrXSxy0hZdemCbQynpgBlST-xLnA=s288-c-no" },
            { EmailKey, "test@gmail.com" },
        };

        SingUp(player, testId);
    }

    public void SingUp(GoogleSignInUser user)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, user.UserId },
            { NameKey, user.DisplayName },
            { EloKey, 500 },
            { IconURLKey, user.ImageUrl.ToString() },
            { EmailKey, user.Email },
        };
        SingUp(player, user.UserId);
    }

    private void SingUp(Dictionary<string, object> playerData, string playerId)
    {
        DocumentReference docRef = db.Collection(CollectionName).Document(playerId);
        docRef.SetAsync(playerData).ContinueWithOnMainThread(async setTask =>
        {
            if (setTask.IsFaulted)
            {
                Debug.LogError("Failed to add player: " + setTask.Exception);
            }
            else
            {
                Debug.Log("Player added successfully.");
                var icon = await GlobalTools.LoadSprite(new Uri(playerData[IconURLKey].ToString()));
                PlayerData = new FirebasePlayerData
                (
                    id: playerData[IDKey].ToString(), 
                    name: playerData[NameKey].ToString(),
                    elo: int.Parse(playerData[EloKey].ToString()),
                    icon: icon,
                    email: playerData[EmailKey].ToString()
                );
            }
        });
    }

    public void SetElo(string playerId, int newElo)
    {
        DocumentReference docRef = db.Collection(CollectionName).Document(playerId);
        
        Dictionary<string, object> updates = new Dictionary<string, object>
        {
            { EloKey, newElo }
        };
        
        docRef.UpdateAsync(updates).ContinueWith(task => {
            if (task.IsCompleted) {
                Debug.Log("Document updated successfully.");
            } else if (task.IsFaulted) {
                Debug.LogError("Error updating document: " + task.Exception);
            }
        });
    }
}