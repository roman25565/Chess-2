using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Firebase.RealtimeDatabase.Data;
using UnityEngine;
using UnityEngine.Events;
using Type = Firebase.RealtimeDatabase.Data.Type;

namespace Firebase.RealtimeDatabase
{
    public class RealtimeDatabase
    {
        private ClientMatchmaker _clientMatchmaker;
        
        private const long WeekInMs = 7 * 24 * 60 * 60 * 1000; // 7 day in ms
        private readonly List<AbstractRequestData> Requests = new ();

        public IReadOnlyList<AbstractRequestData> GetRequests
        {
            get
            {
                var combinedList = new List<AbstractRequestData>();
                combinedList.AddRange(_friendRequestsManager.GetRequestData());
                combinedList.AddRange(_matchRequestsManager.GetRequestData());
                return combinedList;
            }
        }
        
        public readonly UnityEvent OnChangedRequests = new ();
        
        private readonly DatabaseReference _database;
        private readonly string _currentUserId;
        private FriendRequestsManager _friendRequestsManager;
        private MatchRequestsManager _matchRequestsManager;
        
        public class FirebaseEventDisposable : IDisposable
        {
            private Action _unsubscribeAction;

            public FirebaseEventDisposable(Action unsubscribeAction)
            {
                _unsubscribeAction = unsubscribeAction;
            }

            public void Dispose()
            {
                _unsubscribeAction?.Invoke();
                _unsubscribeAction = null;
            }
        }
        

        public RealtimeDatabase(string userId, UnityAction<string> addFriend, ClientMatchmaker clientMatchmaker)
        {
            _currentUserId = userId;
            _database = FirebaseDatabase.DefaultInstance.RootReference;
            _clientMatchmaker = clientMatchmaker;
            Init(addFriend);
        }

        private void Init(UnityAction<string> addFriend)
        {
            // _ = SendMatchInvite("003", "Data", "lol");
            
            DeleteLegacyRequests();
            
            _friendRequestsManager = new FriendRequestsManager(_database, _currentUserId, OnChangedRequests, addFriend);
            _matchRequestsManager = new MatchRequestsManager(_database, _currentUserId, OnChangedRequests, _clientMatchmaker);
        }


        private void DeleteLegacyRequests()
        {
            long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long cutoffTime = currentTimeMs - WeekInMs;
            
            _database.Child("friendRequests")
                .OrderByChild("senderId_timestamp")
                .StartAt($"{_currentUserId}_0")
                .EndAt($"{_currentUserId}_{cutoffTime}")
                .GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Error getting old received requests: " + task.Exception);
                        return;
                    }

                    DataSnapshot snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        foreach (DataSnapshot request in snapshot.Children)
                        {
                            request.Reference.RemoveValueAsync();
                            Debug.Log($"Removed old received request from {request.Key}");
                        }
                    }
                });
            
            _database.Child("matchRequests")
                .OrderByChild("senderId_timestamp")
                .StartAt($"{_currentUserId}_0")
                .EndAt($"{_currentUserId}_{cutoffTime}")
                .GetValueAsync().ContinueWith(task =>
                {
                    if (task.IsFaulted)
                    {
                        Debug.LogError("Error getting old received requests: " + task.Exception);
                        return;
                    }

                    var snapshot = task.Result;
                    if (snapshot.Exists)
                    {
                        foreach (var request in snapshot.Children)
                        {
                            request.Reference.RemoveValueAsync();
                            Debug.Log($"Removed old received request from {request.Key}");
                        }
                    }
                });

        }

        public void AcceptInvite(AbstractRequestData request)
        {
            if (request.RequestType == Type.FriendRequest)
            {
                _ = _friendRequestsManager.AcceptFriendRequest(request);
            }
            else if (request.RequestType == Type.MatchRequest)
            {
                _ = _matchRequestsManager.AcceptMatchRequest(request);
            }
        }
        public void DeclineInvite(AbstractRequestData request)
        {
            if (request.RequestType == Type.FriendRequest)
            {
                _ = _friendRequestsManager.DeclineFriendRequest(request);
            }
            else if (request.RequestType == Type.MatchRequest)
            {
                _ = _matchRequestsManager.DeclineMatchRequest(request);
            }
        }
    }
}