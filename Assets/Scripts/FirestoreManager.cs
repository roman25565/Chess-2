using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;
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

    public void SingUp(FirebasePlayerData playerData)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, playerData.ID },
            { NameKey, playerData.Name },
            { EloKey, playerData.Elo },
            { IconURLKey, playerData.Icon },
            { EmailKey, playerData.Email },
        };
        
        DocumentReference docRef = db.Collection(CollectionName).Document(playerData.ID);
        docRef.SetAsync(player).ContinueWithOnMainThread(setTask =>
        {
            if (setTask.IsFaulted)
            {
                Debug.LogError("Failed to add player: " + setTask.Exception);
            }
            else
            {
                Debug.Log("Player added successfully.");
                PlayerData = playerData;
            }
        });
    }
}