#if !UNITY_SERVER
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RealtimeDatabase.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Firebase.RealtimeDatabase
{
public class ReConnectRequestsManager
{
    private AdvancedMatchmaking _advancedMatchmaking;
        
    private readonly DatabaseReference _database;
    private readonly string _currentUserId;


    public ReConnectRequestsManager(DatabaseReference database, string currentUserId, AdvancedMatchmaking advancedMatchmaking)
    {
        _database = database;
        _currentUserId = currentUserId;
        _advancedMatchmaking = advancedMatchmaking;
    }

    public async Task SendReConnectRequest(string recipientId,string relayJoinCode)
    {
        try
        {
            Debug.Log($"SendReConnectRequest: {recipientId}, {relayJoinCode}");
            var requestData = new ReConnectRequestData(recipientId, relayJoinCode).ToDictionary();

            var requestRef = _database.Child(ReConnectRequestData.CollectionName)
                .Push();

            await requestRef.SetValueAsync(requestData);

            Debug.Log("ReConnect request sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending ReConnect request: {e.Message}");
        }
    }
    
    public void FetchReConnectRequests()
    {
        try
        {
            Debug.Log("FetchReConnectRequests");
            var query = _database.Child(ReConnectRequestData.CollectionName)
                .OrderByChild(AbstractRequestData.RecipientIdKey)
                .EqualTo(_currentUserId);

            query.GetValueAsync().ContinueWithOnMainThread(async task =>
            {
                Debug.Log("FetchRequests task");
                if (task.IsFaulted)
                {
                    Debug.LogError("Error getting sent requests: " + task.Exception);
                    return;
                }

                var snapshot = task.Result;

                if (!snapshot.Exists) return;
                Debug.Log("FetchRequests snapshot");
                Debug.Log(snapshot.Children.First().Key);
                foreach (var request in snapshot.Children)
                {
                    var data = new ReConnectRequestData(request);
                    Debug.Log("FetchRequests data" + data.Timestamp + " " + data.RelayJoinCode);
                    var currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                    var timeDifferenceInMinutes = (currentTime - data.Timestamp) / (1000 * 60);
                    Debug.Log("FetchRequests timeDifferenceInMinutes" + timeDifferenceInMinutes);

                    if (timeDifferenceInMinutes > 2)
                    {
                        Debug.Log("DeleteRequest");
                        request.Reference.RemoveValueAsync().ContinueWith(removeTask =>
                        {
                            if (removeTask.IsFaulted)
                            {
                                Debug.LogError($"Error removing request: {removeTask.Exception}");
                            }
                        });
                    }
                    else
                    {
                        Debug.Log("ReConnect");
                        try
                        {
                            await _advancedMatchmaking.ReConnectToMatch(data.RelayJoinCode);
                            await request.Reference.RemoveValueAsync();

                        }
                        catch (Exception d)
                        {
                            Debug.LogError(d);
                            Console.WriteLine(d);
                            throw;
                        }
                    }
                }
            });
        }
        catch (Exception e)
        {
            Debug.LogError("error: " + e);
            Console.WriteLine(e);
            throw;
        }
    }

    
}
}
#endif