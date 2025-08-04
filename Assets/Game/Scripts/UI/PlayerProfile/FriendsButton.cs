using System;
using System.Collections.Generic;
using Setting;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.UI.PlayerProfile
{
public class FriendsButton : MonoBehaviour
{
    [Inject] private Global _global;

    [SerializeField] private Button toProfileButton;
    [SerializeField] private Button sendMatchRequest;
    [SerializeField] private Button sendDeleteFriendRequest;
    [SerializeField] private Image playerIcon;
    [SerializeField] private TextMeshProUGUI playerNameAndElo;

    [SerializeField] private Notification prefab;

    private List<Notification> _notifications;
    
    public void SetButton(FirebasePlayerData playerData, MainMenu mainMenu, Transform notificationParent, List<Notification> notifications)
    {
        _notifications = notifications;
        try
        {
            Debug.Log($"{_global?.FirestoreManager?.PlayerData.Name}, {_global?.FirestoreManager?.PlayerData.ID}, {playerData.Name}, {playerData.ID}");
            if (_global?.FirestoreManager?.PlayerData == null) 
            {
                Debug.LogError("PlayerData is not initialized");
                return;
            }
            
            var myName = _global.FirestoreManager.PlayerData.Name;
            var myId = _global.FirestoreManager.PlayerData.ID;

            playerIcon.color = new Color(255, 255, 255, 255);
            playerIcon.sprite = playerData.Icon;
            Debug.Log($"SetButton {playerData.Name}, {playerData.Elo}");
            playerNameAndElo.text = $"{playerData.Name} ({playerData.Elo})";

            toProfileButton.onClick.AddListener(() => { mainMenu.ShowProfilePanel(playerData.ID); });
            sendMatchRequest.onClick.AddListener(() => OnSendMatchRequest(playerData.ID, myName, notificationParent));
            var isMyFriend = _global.FirestoreManager.PlayerData.FriendIds.Contains(playerData.ID);
            if (isMyFriend)
            {
                sendDeleteFriendRequest.onClick.AddListener(() => { OnsendDeleteFriendRequest(myId, playerData.ID, notificationParent); });
            }
            else
            {
                sendDeleteFriendRequest.interactable = false;
                sendDeleteFriendRequest.onClick.RemoveAllListeners();
            }
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }

    }


    private void OnSendMatchRequest(string recipientId, string senderName, Transform notificationParent)
    {
        var realtimeDatabase = _global.FirestoreManager.RealtimeDatabase;

        var notification = Instantiate(prefab, notificationParent);
        ClearNotifications();
        _notifications.Add(notification);
        notification.Init("Send Match Request?",() => { _ = realtimeDatabase.MatchRequestsManager.SendMatchRequest(recipientId, senderName); }, () => { _notifications.Remove(notification); });
    }

    private void OnsendDeleteFriendRequest(string myId, string friendId, Transform notificationParent)
    {
        var realtimeDatabase = _global.FirestoreManager.RealtimeDatabase;

        var notification = Instantiate(prefab, notificationParent);
        ClearNotifications();
        _notifications.Add(notification);
        notification.Init("Delete Friend?",() => { realtimeDatabase.FriendRequestsManager.DeleteFriendRequest(myId, friendId); }, () => { _notifications.Remove(notification); });
    }

    private void OnDisable()
    {
        ClearNotifications();
    }

    private void ClearNotifications()
    {
        foreach (var notification in _notifications)
        {
            if (notification != null && notification.gameObject != null)
            {
                Destroy(notification.gameObject);
            }
        }

        _notifications.Clear();
    }
}
}
