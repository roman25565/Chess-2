using Firebase.Database;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Firebase.Extensions;
using Firebase.RealtimeDatabase.Data;
using UnityEngine;

namespace Firebase.RealtimeDatabase
{

    public class RealtimeDatabase
    {
        private DatabaseReference _database;
        private string _currentUserId;
        // 7 днів у мілісекундах
        private const long WeekInMs = 7 * 24 * 60 * 60 * 1000; 
        
        private List<FriendRequestData> _friendRequests = new List<FriendRequestData>();
        private class FirebaseEventDisposable : IDisposable
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
        

        public RealtimeDatabase(string userId)
        {
            _currentUserId = userId;
            _database = FirebaseDatabase.DefaultInstance.RootReference;
            Init();
        }

        private void Init()
        {
            SendMatchInvite("003", "Dota", "lol");
            SendFriendRequest("004", "Dota");
            
            DeleteOldRequests();
            
            
            ListenForFriendRequests(((s, s1) => {}));
            ListenForMatchInvites(((s, s1, s3) => {}));
        }

        private void DeleteOldRequests()
        {
            long currentTimeMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            long cutoffTime = currentTimeMs + WeekInMs;
            
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
            
            _database.Child("matchInvites")
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

        }


        #region Friend Requests
        public async Task SendFriendRequest(string recipientId, string senderName)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var requestData = new Dictionary<string, object>
                {
                    ["senderId"] = _currentUserId,
                    ["timestamp"] = timestamp,
                    ["senderId_timestamp"] = $"{_currentUserId}_{timestamp}",
                    ["senderName"] = senderName,
                    ["status"] = "pending"
                };

                await _database.Child("friendRequests")
                    .Child(recipientId)
                    .SetValueAsync(requestData);

                Debug.Log("Friend request sent successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending friend request: {e.Message}");
            }
        }

        public async Task AcceptFriendRequest(string senderId)
        {
            try
            {
                // Оновлюємо статус запиту
                var updates = new Dictionary<string, object>
                {
                    [$"friendRequests/{_currentUserId}/{senderId}/status"] = "accepted",
                };

                await _database.UpdateChildrenAsync(updates);
                Debug.Log("Friend request accepted");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error accepting friend request: {e.Message}");
            }
        }

        public async Task DeclineFriendRequest(string senderId)
        {
            try
            {
                await _database.Child("friendRequests")
                    .Child(_currentUserId)
                    .Child(senderId)
                    .RemoveValueAsync();

                Debug.Log("Friend request declined");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error declining friend request: {e.Message}");
            }
        }

        public IDisposable ListenForFriendRequests(Action<string, string> onRequestReceived)
        {
            void Handler(object sender, ChildChangedEventArgs args)
            {
                if (args.DatabaseError != null)
                {
                    Debug.LogError(args.DatabaseError.Message);
                    return;
                }
                
                _friendRequests.Add(new FriendRequestData(args.Snapshot));

                var senderId = args.Snapshot.Key;
                var senderName = args.Snapshot.Child("senderName").Value.ToString();
                onRequestReceived?.Invoke(senderId, senderName);
            }

            var query = _database.Child("friendRequests")
                .Child(_currentUserId)
                .OrderByChild("status")
                .EqualTo("pending");

            query.ChildAdded += Handler;

            return new FirebaseEventDisposable(() => 
            {
                query.ChildAdded -= Handler;
            });
        }

        #endregion

        #region Match Invitations

        public async Task SendMatchInvite(string recipientId, string senderName, string gameMode)
        {
            try
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds().ToString();
                var inviteData = new Dictionary<string, object>
                {
                    ["senderId"] = _currentUserId,
                    ["timestamp"] = timestamp,
                    ["senderId_timestamp"] = $"{_currentUserId}_{timestamp}",
                    ["senderName"] = senderName,
                    ["gameMode"] = gameMode,
                    ["status"] = "pending"
                };

                await _database.Child("matchInvites")
                    .Child(recipientId)
                    .SetValueAsync(inviteData);

                Debug.Log("Match invite sent successfully");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error sending match invite: {e.Message}");
            }
        }

        public async Task AcceptMatchInvite(string senderId, Action<string, string> onSuccess)
        {
            try
            {
                // Отримуємо дані про запрошення
                var inviteSnapshot = await _database.Child("matchInvites")
                    .Child(_currentUserId)
                    .Child(senderId)
                    .GetValueAsync();

                if (!inviteSnapshot.Exists)
                {
                    Debug.LogWarning("Match invite no longer exists");
                    return;
                }

                var gameMode = inviteSnapshot.Child("gameMode").Value.ToString();

                // Видаляємо запрошення
                await _database.Child("matchInvites")
                    .Child(_currentUserId)
                    .Child(senderId)
                    .RemoveValueAsync();

                // Створюємо кімнату гри
                var roomId = Guid.NewGuid().ToString();
                var roomData = new Dictionary<string, object>
                {
                    ["player1"] = senderId,
                    ["player2"] = _currentUserId,
                    ["gameMode"] = gameMode,
                    ["status"] = "waiting"
                };

                await _database.Child("gameRooms")
                    .Child(roomId)
                    .SetValueAsync(roomData);

                onSuccess?.Invoke(roomId, gameMode);
            }
            catch (Exception e)
            {
                Debug.LogError($"Error accepting match invite: {e.Message}");
            }
        }

        public async Task DeclineMatchInvite(string senderId)
        {
            try
            {
                await _database.Child("matchInvites")
                    .Child(_currentUserId)
                    .Child(senderId)
                    .RemoveValueAsync();

                Debug.Log("Match invite declined");
            }
            catch (Exception e)
            {
                Debug.LogError($"Error declining match invite: {e.Message}");
            }
        }

        public IDisposable ListenForMatchInvites(Action<string, string, string> onInviteReceived)
        {
            void Handler(object sender, ChildChangedEventArgs args)
            {
                if (args.DatabaseError != null)
                {
                    Debug.LogError(args.DatabaseError.Message);
                    return;
                }

                var senderId = args.Snapshot.Key;
                var senderName = args.Snapshot.Child("senderName").Value.ToString();
                var gameMode = args.Snapshot.Child("gameMode").Value.ToString();
                Debug.Log("senderId" + senderId + " senderName" + senderName + " gameMode" + gameMode);
                onInviteReceived?.Invoke(senderId, senderName, gameMode);
            }

            var query = _database.Child("matchInvites")
                .Child(_currentUserId)
                .OrderByChild("status")
                .EqualTo("pending");

            query.ChildAdded += Handler;

            return new FirebaseEventDisposable(() => 
            {
                query.ChildAdded -= Handler;
            });
        }


        #endregion

        #region Helper Methods

        public void DisposeListeners(IDisposable listener)
        {
            listener?.Dispose();
        }

        #endregion
    }
}