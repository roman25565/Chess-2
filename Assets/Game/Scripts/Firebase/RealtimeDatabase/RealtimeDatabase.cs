// using Firebase.Database;
// using System;
// using System.Collections.Generic;
// using System.Threading.Tasks;
// using UnityEngine;
//
// namespace Firebase.RealtimeDatabase
// {
//     public class RealtimeDatabase
//     {
//         private DatabaseReference _database;
//         private string _currentUserId;
//
//         public RealtimeDatabase(string userId)
//         {
//             _currentUserId = userId;
//             _database = FirebaseDatabase.DefaultInstance.RootReference;
//         }
//
//         #region Friend Requests
//
//         public async Task SendFriendRequest(string recipientId, string senderName)
//         {
//             try
//             {
//                 var requestData = new Dictionary<string, object>
//                 {
//                     ["senderId"] = _currentUserId,
//                     ["senderName"] = senderName,
//                     ["timestamp"] = ServerValue.Timestamp,
//                     ["status"] = "pending"
//                 };
//
//                 await _database.Child("friendRequests")
//                     .Child(recipientId)
//                     .Child(_currentUserId)
//                     .SetValueAsync(requestData);
//
//                 Debug.Log("Friend request sent successfully");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error sending friend request: {e.Message}");
//             }
//         }
//
//         public async Task AcceptFriendRequest(string senderId)
//         {
//             try
//             {
//                 // Оновлюємо статус запиту
//                 var updates = new Dictionary<string, object>
//                 {
//                     [$"friendRequests/{_currentUserId}/{senderId}/status"] = "accepted",
//                     [$"friends/{_currentUserId}/{senderId}"] = true,
//                     [$"friends/{senderId}/{_currentUserId}"] = true
//                 };
//
//                 await _database.UpdateChildrenAsync(updates);
//                 Debug.Log("Friend request accepted");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error accepting friend request: {e.Message}");
//             }
//         }
//
//         public async Task DeclineFriendRequest(string senderId)
//         {
//             try
//             {
//                 await _database.Child("friendRequests")
//                     .Child(_currentUserId)
//                     .Child(senderId)
//                     .RemoveValueAsync();
//
//                 Debug.Log("Friend request declined");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error declining friend request: {e.Message}");
//             }
//         }
//
//         public IDisposable ListenForFriendRequests(Action<string, string> onRequestReceived)
//         {
//             return _database.Child("friendRequests")
//                 .Child(_currentUserId)
//                 .OrderByChild("status")
//                 .EqualTo("pending")
//                 .ChildAdded += (sender, args) =>
//                 {
//                     if (args.DatabaseError != null)
//                     {
//                         Debug.LogError(args.DatabaseError.Message);
//                         return;
//                     }
//
//                     var senderId = args.Snapshot.Key;
//                     var senderName = args.Snapshot.Child("senderName").Value.ToString();
//                     onRequestReceived?.Invoke(senderId, senderName);
//                 };
//         }
//
//         #endregion
//
//         #region Match Invitations
//
//         public async Task SendMatchInvite(string recipientId, string senderName, string gameMode)
//         {
//             try
//             {
//                 var inviteData = new Dictionary<string, object>
//                 {
//                     ["senderId"] = _currentUserId,
//                     ["senderName"] = senderName,
//                     ["gameMode"] = gameMode,
//                     ["timestamp"] = ServerValue.Timestamp,
//                     ["status"] = "pending"
//                 };
//
//                 await _database.Child("matchInvites")
//                     .Child(recipientId)
//                     .Child(_currentUserId)
//                     .SetValueAsync(inviteData);
//
//                 Debug.Log("Match invite sent successfully");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error sending match invite: {e.Message}");
//             }
//         }
//
//         public async Task AcceptMatchInvite(string senderId, Action<string, string> onSuccess)
//         {
//             try
//             {
//                 // Отримуємо дані про запрошення
//                 var inviteSnapshot = await _database.Child("matchInvites")
//                     .Child(_currentUserId)
//                     .Child(senderId)
//                     .GetValueAsync();
//
//                 if (!inviteSnapshot.Exists)
//                 {
//                     Debug.LogWarning("Match invite no longer exists");
//                     return;
//                 }
//
//                 var gameMode = inviteSnapshot.Child("gameMode").Value.ToString();
//
//                 // Видаляємо запрошення
//                 await _database.Child("matchInvites")
//                     .Child(_currentUserId)
//                     .Child(senderId)
//                     .RemoveValueAsync();
//
//                 // Створюємо кімнату гри
//                 var roomId = Guid.NewGuid().ToString();
//                 var roomData = new Dictionary<string, object>
//                 {
//                     ["player1"] = senderId,
//                     ["player2"] = _currentUserId,
//                     ["gameMode"] = gameMode,
//                     ["status"] = "waiting"
//                 };
//
//                 await _database.Child("gameRooms")
//                     .Child(roomId)
//                     .SetValueAsync(roomData);
//
//                 onSuccess?.Invoke(roomId, gameMode);
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error accepting match invite: {e.Message}");
//             }
//         }
//
//         public async Task DeclineMatchInvite(string senderId)
//         {
//             try
//             {
//                 await _database.Child("matchInvites")
//                     .Child(_currentUserId)
//                     .Child(senderId)
//                     .RemoveValueAsync();
//
//                 Debug.Log("Match invite declined");
//             }
//             catch (Exception e)
//             {
//                 Debug.LogError($"Error declining match invite: {e.Message}");
//             }
//         }
//
//         public IDisposable ListenForMatchInvites(Action<string, string, string> onInviteReceived)
//         {
//             return _database.Child("matchInvites")
//                 .Child(_currentUserId)
//                 .OrderByChild("status")
//                 .EqualTo("pending")
//                 .ChildAdded += (sender, args) =>
//                 {
//                     if (args.DatabaseError != null)
//                     {
//                         Debug.LogError(args.DatabaseError.Message);
//                         return;
//                     }
//
//                     var senderId = args.Snapshot.Key;
//                     var senderName = args.Snapshot.Child("senderName").Value.ToString();
//                     var gameMode = args.Snapshot.Child("gameMode").Value.ToString();
//                     onInviteReceived?.Invoke(senderId, senderName, gameMode);
//                 };
//         }
//
//         #endregion
//
//         #region Helper Methods
//
//         public void DisposeListeners(IDisposable listener)
//         {
//             listener?.Dispose();
//         }
//
//         #endregion
//     }
// }