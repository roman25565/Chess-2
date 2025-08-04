#if !UNITY_SERVER
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RealtimeDatabase.Data;
using Unity.Networking.Transport;
using UnityEngine;
using UnityEngine.Events;

namespace Firebase.RealtimeDatabase
{
public class FriendRequestsManager
{
    private const long WeekInMs = 7 * 24 * 60 * 60 * 1000; // 7 day in ms
        
    private readonly List<FriendRequestData> _requests = new ();
    private readonly UnityEvent _onChangedRequests;
    private readonly DatabaseReference _database;
    private readonly string _currentUserId;
    private UnityAction<string> _addFriend;

    public IReadOnlyList<AbstractRequestData> GetRequestData()
    {
        return _requests;
    }

    public FriendRequestsManager(DatabaseReference database, string currentUserId, UnityEvent onChangedRequests, UnityAction<string> addFriend)
    {
        _database = database;
        _currentUserId = currentUserId;
        _onChangedRequests = onChangedRequests;
        _addFriend = addFriend;

        SubscribeToStatusUpdates();
        ListenReceivedRequests();
        // GetReceivedFriendRequests();
    }

    public async Task SendFriendRequest(string recipientId, string senderName)
    {
        try
        {
            var requestData = new FriendRequestData
                (recipientId, senderName, _currentUserId).ToDictionary();

            var requestRef = _database.Child(FriendRequestData.CollectionName)
                .Push();

            await requestRef.SetValueAsync(requestData);

            Debug.Log("Friend request sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending friend request: {e.Message}");
        }
    }

    public async Task AcceptFriendRequest(AbstractRequestData request)
    {
        try
        {
            var requestRef = _database.Child(FriendRequestData.CollectionName).Child(request.RequestId);
        
            var snapshot = await requestRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogWarning($"Request {request.RequestId} not found");
                return;
            }
            
            var friendRequest = new FriendRequestData(snapshot);
            friendRequest.Status = AbstractRequestData.StatusKeys.AcceptedKey;;

            await requestRef.UpdateChildrenAsync(friendRequest.ToDictionary());
            _addFriend?.Invoke(friendRequest.SenderId);
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting friend request from {request.RequestId}: {e.Message}");
            throw;
        }
    }

    public async Task DeclineFriendRequest(AbstractRequestData request)
    {
        try
        {
            var requestRef = _database.Child(FriendRequestData.CollectionName).Child(request.RequestId);
        
            var snapshot = await requestRef.GetValueAsync();
            if (!snapshot.Exists)
            {
                Debug.LogWarning($"Request {request.RequestId} not found");
                return;
            }


            var friendRequest = new FriendRequestData(snapshot);
            friendRequest.Status = AbstractRequestData.StatusKeys.RejectedKey;

            await requestRef.UpdateChildrenAsync(friendRequest.ToDictionary());
            Debug.Log($"Request {request.RequestId} status updated to rejected");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting friend request from {request.RequestId}: {e.Message}");
            throw;
        }
    }

    public async void DeleteFriendRequest(string myId, string friendId)
    {
        
    }
    
    private void GetReceivedFriendRequests()
    {
        var query = _database.Child(FriendRequestData.CollectionName)
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
                    _requests.Add(new FriendRequestData(request));
                }
            }

            _onChangedRequests?.Invoke();
        });
    }

    private IDisposable ListenReceivedRequests()
    {
        var query = _database.Child(FriendRequestData.CollectionName)
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
            
            _requests.Add(new FriendRequestData(args.Snapshot));
            _onChangedRequests?.Invoke();
        }
    }
    
    private IDisposable SubscribeToStatusUpdates()
    {
        var query = _database.Child(FriendRequestData.CollectionName)
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
                    _addFriend?.Invoke(recipientId);
                    break;
                
                case "rejected":
                    try 
                    {
                        var requestId = args.Snapshot.Key;
                        _database.Child(FriendRequestData.CollectionName)
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