#if !UNITY_SERVER
using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.Firestore;
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
    private readonly UnityAction<string> _addFriend;
    private readonly UnityAction<string> _removeFriend;

    public IReadOnlyList<AbstractRequestData> GetRequestData()
    {
        return _requests;
    }
    
    public void RemoveRequest(AbstractRequestData request)
    {
        _requests.Remove(request as FriendRequestData);
        _onChangedRequests?.Invoke();
    }

    public FriendRequestsManager(DatabaseReference database, string currentUserId, UnityEvent onChangedRequests, UnityAction<string> addFriend, UnityAction<string> removeFriend)
    {
        _database = database;
        _currentUserId = currentUserId;
        _onChangedRequests = onChangedRequests;
        _addFriend = addFriend;
        _removeFriend = removeFriend;

        SubscribeToStatusUpdates();
        ListenReceivedRequests();
        ListenReceivedRemoveFriendRequests();
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
            friendRequest.Status = StatusKeys.AcceptedKey;

            await requestRef.UpdateChildrenAsync(friendRequest.ToDictionary());
            _addFriend.Invoke(friendRequest.SenderId);
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
            friendRequest.Status = StatusKeys.RejectedKey;

            await requestRef.UpdateChildrenAsync(friendRequest.ToDictionary());
            Debug.Log($"Request {request.RequestId} status updated to rejected");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error accepting friend request from {request.RequestId}: {e.Message}");
            throw;
        }
    }

    public async void SendDeleteFriendRequest(string myId, string recipientId)
    {
        if (myId != _currentUserId)
        {
            Debug.LogError("You can only delete friend requests from your profile");
            return;
        }
        
        try
        {
            var requestData = new RemoveFriendRequestData
                (recipientId, "/SenderName/", _currentUserId).ToDictionary();

            var requestRef = _database.Child(RemoveFriendRequestData.CollectionName)
                .Push();

            await requestRef.SetValueAsync(requestData);

            Debug.Log("Friend request sent successfully");
        }
        catch (Exception e)
        {
            Debug.LogError($"Error sending friend request: {e.Message}");
        }
        
        _removeFriend.Invoke(recipientId);
    }
    
    private IDisposable ListenReceivedRemoveFriendRequests()
    {
        var query = _database.Child(RemoveFriendRequestData.CollectionName)
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
            
            var data = new RemoveFriendRequestData(args.Snapshot);
            _removeFriend.Invoke(data.SenderId);
            RemoveFriendRequestById(data.RequestId);
        }
        
        void RemoveFriendRequestById(string requestId)
        {
            try
            {
                _database.Child(RemoveFriendRequestData.CollectionName)
                    .Child(requestId)
                    .RemoveValueAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError($"Failed to remove request: {ex.Message}");
            }
        }
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
            if (status != StatusKeys.PendingKey)
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

        if (string.IsNullOrEmpty(newStatus)) return;
        Debug.Log($"Request to {recipientId} changed status to: {newStatus}");
        switch (newStatus)
        {
            case StatusKeys.AcceptedKey:
                Debug.Log($"Request to {recipientId} accepted");
                _addFriend.Invoke(recipientId);
                RemoveFriendRequestById();
                break;
                
            case StatusKeys.RejectedKey:
                RemoveFriendRequestById();
                break;
        }

        return;

        void RemoveFriendRequestById()
        {
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
        }
    }
}
}
#endif