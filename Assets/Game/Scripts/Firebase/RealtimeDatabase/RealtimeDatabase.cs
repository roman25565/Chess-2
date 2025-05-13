#if !UNITY_SERVER
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
        public FriendRequestsManager FriendRequestsManager;
        public MatchRequestsManager MatchRequestsManager;
        public ReConnectRequestsManager ReConnectRequestsManager;
        
        private ClientMatchmaker _clientMatchmaker;
        
        private const long WeekInMs = 7 * 24 * 60 * 60 * 1000; // 7 day in ms

        public IReadOnlyList<AbstractRequestData> GetRequests
        {
            get
            {
                var combinedList = new List<AbstractRequestData>();
                combinedList.AddRange(FriendRequestsManager.GetRequestData());
                combinedList.AddRange(MatchRequestsManager.GetRequestData());
                return combinedList;
            }
        }
        
        public readonly UnityEvent OnChangedRequests = new ();
        
        private readonly DatabaseReference _database;
        private readonly string _currentUserId;
        
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
            DeleteLegacyRequests();
            
            FriendRequestsManager = new FriendRequestsManager(_database, _currentUserId, OnChangedRequests, addFriend);
            MatchRequestsManager = new MatchRequestsManager(_database, _currentUserId, OnChangedRequests, _clientMatchmaker);
            ReConnectRequestsManager = new ReConnectRequestsManager(_database, _currentUserId, _clientMatchmaker);
        }


        private void DeleteLegacyRequests()
        {
            long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long cutoffTime = currentTimeMs - WeekInMs;
            
            _database.Child(FriendRequestData.CollectionName)
                .OrderByChild(AbstractRequestData.SenderIdTimestampKey)
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
            
            _database.Child(MatchRequestData.CollectionName)
                .OrderByChild(AbstractRequestData.SenderIdTimestampKey)
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
                _ = FriendRequestsManager.AcceptFriendRequest(request);
            }
            else if (request.RequestType == Type.MatchRequest)
            {
                _ = MatchRequestsManager.AcceptMatchRequest(request);
            }
        }
        public void DeclineInvite(AbstractRequestData request)
        {
            if (request.RequestType == Type.FriendRequest)
            {
                _ = FriendRequestsManager.DeclineFriendRequest(request);
            }
            else if (request.RequestType == Type.MatchRequest)
            {
                _ = MatchRequestsManager.DeclineMatchRequest(request);
            }
        }
    }
}
#endif