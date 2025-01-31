using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Firebase;
using Firebase.Extensions;
using Firebase.Firestore;

public class FirestoreManager
{
    private const string IDKey = "ID";
    private const string NameKey = "Name";
    private const string EloKey = "Elo";
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
            Console.WriteLine(e);
            throw;
        }
    }

    public delegate void GetPlayerDataCallBack(FirebasePlayerData result);
    public async Task GetPlayerData(string playerId,GetPlayerDataCallBack callback)
    {
        var docRef = db.Collection(CollectionName).Document(playerId);
        FirebasePlayerData result = null;
        
        try
        {
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();

            if (snapshot.Exists)
            {
                var existingName = snapshot.GetValue<string>(NameKey);
                var existingElo = snapshot.GetValue<int>(EloKey);
                result = new FirebasePlayerData(playerId, existingName, existingElo);
                PlayerData = result;
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
    }

    public void SingUp(FirebasePlayerData playerData)
    {
        Dictionary<string, object> player = new Dictionary<string, object>
        {
            { IDKey, playerData.ID },
            { NameKey, playerData.Name },
            { EloKey, playerData.Elo }
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