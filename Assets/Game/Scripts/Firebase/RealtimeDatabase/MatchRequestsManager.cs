#if !UNITY_SERVER
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.RealtimeDatabase.Data;
using UnityEngine;
using UnityEngine.Events;

namespace Firebase.RealtimeDatabase
{
public class MatchRequestsManager
{
    private ClientMatchmaker _clientMatchmaker;
    
    private const long WeekInMs = 7 * 24 * 60 * 60 * 1000; // 7 day in ms
        
    private readonly List<MatchRequestData> _requests = new ();
    private readonly UnityEvent _onChangedRequests;
    private readonly DatabaseReference _database;
    private readonly string _currentUserId;

    public IReadOnlyList<AbstractRequestData> GetRequestData()
    {
        return _requests;
    }

    public MatchRequestsManager(DatabaseReference database, string currentUserId, UnityEvent onChangedRequests, ClientMatchmaker clientMatchmaker)
    {
        _database = database;
        _currentUserId = currentUserId;
        Debug.Log($"Current user id: {_currentUserId}");
        _onChangedRequests = onChangedRequests;
        _clientMatchmaker = clientMatchmaker;

        SubscribeToStatusUpdates();
        ListenReceivedRequests();

        // SendMatchRequest("002", "Alpha");
    }

    public async Task SendMatchRequest(string recipientId, string senderName)
    {
        try
        {
            var requestData = new MatchRequestData
                (recipientId, senderName, _currentUserId).ToDictionary();

            var requestRef = _database.Child(MatchRequestData.CollectionName)
                .Push();

            await requestRef.SetValueAsync(requestData);

            Debug.Log("Match request sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending match request: {e.Message}");
        }
    }

    public async Task AcceptMatchRequest(AbstractRequestData request)
    {
        try
        {
            var requestRef = _database.Child(MatchRequestData.CollectionName).Child(request.RequestId);
        
            var snapshot = await requestRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogWarning($"Request {request.RequestId} not found");
                return;
            }
            
            var matchRequest = new MatchRequestData(snapshot);
            matchRequest.Status = AbstractRequestData.StatusKeys.AcceptedKey;;

            _ = _clientMatchmaker.StartFriendMatch((ip, port) =>
            {
                matchRequest.SetConnectionInfo(ip,port);
                        
                var requestRef = _database.Child(MatchRequestData.CollectionName).Child(matchRequest.RequestId);
                requestRef.UpdateChildrenAsync(matchRequest.ToDictionary());
            });
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting match request from {request.RequestId}: {e.Message}");
            throw;
        }
    }

    public async Task DeclineMatchRequest(AbstractRequestData request)
    {
        try
        {
            var requestRef = _database.Child(MatchRequestData.CollectionName).Child(request.RequestId);
        
            var snapshot = await requestRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogWarning($"Request {request.RequestId} not found");
                return;
            }


            var matchRequest = new MatchRequestData(snapshot);
            matchRequest.Status = AbstractRequestData.StatusKeys.RejectedKey;

            await requestRef.UpdateChildrenAsync(matchRequest.ToDictionary());
            Debug.Log($"Request {request.RequestId} status updated to rejected");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting match request from {request.RequestId}: {e.Message}");
            throw;
        }
    }
    
    private void GetReceivedMatchRequests()
    {
        var query = _database.Child(MatchRequestData.CollectionName)
            .OrderByChild(AbstractRequestData.RecipientIdKey)
            .EqualTo(_currentUserId);

        query.GetValueAsync().ContinueWith(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Error getting sent requests: " + task.Exception);
                return;
            }

            var snapshot = task.Result;
           
            if (snapshot.Exists)
            {
                foreach (var request in snapshot.Children)
                {
                    _requests.Add(new MatchRequestData(request));
                }
            }

            _onChangedRequests?.Invoke();
        });
    }

    private IDisposable ListenReceivedRequests()
    {
        var query = _database.Child(MatchRequestData.CollectionName)
            .OrderByChild(AbstractRequestData.RecipientIdKey)
            .EqualTo(_currentUserId);

        query.ChildAdded += Handler;

        return new RealtimeDatabase.FirebaseEventDisposable(() => { query.ChildAdded -= Handler; });

        void Handler(object sender, ChildChangedEventArgs args)
        {
            if (args.DatabaseError != null)
            {
                Debug.LogError(args.DatabaseError.Message);
                return;
            }
            var status = args.Snapshot.Child(AbstractRequestData.StatusKey)?.Value as string;
            if (status != AbstractRequestData.StatusKeys.PendingKey)
                return;
            
            _requests.Add(new MatchRequestData(args.Snapshot));
            _onChangedRequests?.Invoke();
        }
    }
    
    private IDisposable SubscribeToStatusUpdates()
    {
        var query = _database.Child(MatchRequestData.CollectionName)
            .OrderByChild(AbstractRequestData.SenderIdKey)
            .EqualTo(_currentUserId);

        query.GetValueAsync().ContinueWith(task => 
            Debug.Log(task.Result.Children.First()));
        query.ChildChanged += HandleSentStatusChange;

        return new RealtimeDatabase.FirebaseEventDisposable(() => 
        {
            query.ChildChanged -= HandleSentStatusChange;
        });

    }
    
    private void HandleSentStatusChange(object sender, ChildChangedEventArgs args)
    {
        if (args.DatabaseError != null)
        {
            Debug.LogError(args.DatabaseError.Message);
            return;
        }
        var newStatus = args.Snapshot.Child(AbstractRequestData.StatusKey)?.Value as string;
        var recipientId = args.Snapshot.Child(AbstractRequestData.RecipientIdKey)?.Value as string;
        
        if (!string.IsNullOrEmpty(newStatus))
        {
            Debug.Log($"Request to {recipientId} changed status to: {newStatus}");

            switch (newStatus)
            {
                case "accepted":
                    var matchRequest = new MatchRequestData(args.Snapshot);
                    if (!string.IsNullOrEmpty(matchRequest.Ip) && matchRequest.Port > 0)
                    {
                        _clientMatchmaker.JoinFriendMatch(matchRequest.Ip, matchRequest.Port);
                    }
                    break;
                
                case "rejected":
                    try 
                    {
                        var requestId = args.Snapshot.Key;
                        _database.Child(MatchRequestData.CollectionName)
                            .Child(requestId)
                            .RemoveValueAsync();
                    }
                    catch (Exception ex)
                    {
                        Debug.LogError($"Failed to remove request: {ex.Message}");
                    }
                    break;
            }
        }
    }
}
}
#endif