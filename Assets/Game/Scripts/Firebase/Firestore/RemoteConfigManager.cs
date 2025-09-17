using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
using Firebase.RemoteConfig;
using JetBrains.Annotations;
using UnityEngine;

namespace Game.Scripts.Firebase.Firestore
{
public class RemoteConfigManager
{
    public readonly string PiceCostEasyBotKey = "PiceCostEasyBot";
    public readonly string PiceCostNormalBotKey = "PiceCostNormalBot";
    public readonly string PiceCostHardBotKey = "PiceCostHardBot";
    public readonly string PiceCostExpertBotKey = "PiceCostExpertBot";
    public readonly string MaxEloDifferenceKey = "MaxEloDifference";
    
    private FirebaseFirestore _db;
    
    private BackendManager _backendManager;
    private Dictionary<string, object> _data = new Dictionary<string, object>();
    public RemoteConfigManager(FirebaseFirestore db, BackendManager backendManager)
    {
        _db = db;
        _backendManager = backendManager;
        
        Init();
    }

    private async void Init()
    {
        Dictionary<string, object> defaults =
            new Dictionary<string, object>();

// These are the values that are used if we haven't fetched data from the
// server
// yet, or if we ask for values that the server doesn't have:
        defaults.Add(PiceCostEasyBotKey, 20);
        defaults.Add(PiceCostNormalBotKey, 30);
        defaults.Add(PiceCostHardBotKey, 40);
        defaults.Add(PiceCostExpertBotKey, 50);
        defaults.Add(MaxEloDifferenceKey, 500);

        await global::Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.SetDefaultsAsync(defaults);
        await FetchDataAsync().ContinueWithOnMainThread(FetchComplete);

    }

    private Task FetchDataAsync() {
        Debug.Log("Fetching data...");
        Task fetchTask =
            global::Firebase.RemoteConfig.FirebaseRemoteConfig.DefaultInstance.FetchAsync(
                TimeSpan.Zero);
        return fetchTask;
    }
    
    private void FetchComplete(Task fetchTask) {
        if (!fetchTask.IsCompleted) {
            Debug.LogError("Retrieval hasn't finished.");
            return;
        }

        var remoteConfig = FirebaseRemoteConfig.DefaultInstance;
        var info = remoteConfig.Info;
        if(info.LastFetchStatus != LastFetchStatus.Success) {
            Debug.LogError($"{nameof(FetchComplete)} was unsuccessful\n{nameof(info.LastFetchStatus)}: {info.LastFetchStatus}");
            return;
        }

        // Fetch successful. Parameter values must be activated to use.
        remoteConfig.ActivateAsync()
            .ContinueWithOnMainThread(
                task => {
                    Debug.Log($"Remote data loaded and ready for use. Last fetch time {info.FetchTime}.");
                    
                    _data[PiceCostEasyBotKey] = remoteConfig.GetValue(PiceCostEasyBotKey).LongValue;
                    _data[PiceCostNormalBotKey] = remoteConfig.GetValue(PiceCostNormalBotKey).LongValue;
                    _data[PiceCostHardBotKey] = remoteConfig.GetValue(PiceCostHardBotKey).LongValue;
                    _data[PiceCostExpertBotKey] = remoteConfig.GetValue(PiceCostExpertBotKey).LongValue;
                    _data[MaxEloDifferenceKey] = remoteConfig.GetValue(MaxEloDifferenceKey).LongValue;
                });
    }

    public int GetValue(string key)
    {
        return int.Parse(_data[key].ToString());
    }
}
}