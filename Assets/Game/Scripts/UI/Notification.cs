using UnityEngine;
using System.Collections.Generic;

namespace UI
{
    public struct NotificationData
    {
        public enum NotificationType { FriendRequest, MatchInvite, SystemMessage }
        
        public NotificationType Type;
        public string SenderId;
        public string SenderName;
        public string Message;
        public System.DateTime Timestamp;
    }

    public class Notification : MonoBehaviour
    {
        [SerializeField] private MainMenu _mainMenu;
        private Queue<NotificationData> _notifications = new Queue<NotificationData>();
        private bool _isNotificationActive;
        private NotificationData _currentNotification;

        // private void Start()
        // {
        //     // Підписка на події
        //     FriendSystem.OnFriendRequest += OnFriendRequestReceived;
        //     Matchmaking.OnMatchInvite += OnMatchInviteReceived;
        // }
        //
        // private void OnDestroy()
        // {
        //     // Відписка від подій
        //     FriendSystem.OnFriendRequest -= OnFriendRequestReceived;
        //     Matchmaking.OnMatchInvite -= OnMatchInviteReceived;
        // }

        // Обробник кліку на кнопку сповіщення
        public void OnClick()
        {
            if (_isNotificationActive)
            {
                HideNotification();
            }
            else if (_notifications.Count > 0)
            {
                ShowNextNotification();
            }
        }

        // Реалізація інтерфейсу INotificationHandler
        public void HandleNotification(NotificationData notification)
        {
            _notifications.Enqueue(notification);
            
            if (!_isNotificationActive)
            {
                ShowNextNotification();
            }
        }

        private void ShowNextNotification()
        {
            // if (_notifications.Count == 0) return;
            //
            // _currentNotification = _notifications.Dequeue();
            // _isNotificationActive = true;
            //
            // switch (_currentNotification.Type)
            // {
            //     case NotificationData.NotificationType.FriendRequest:
            //         _mainMenu.ShowNotification(
            //             $"Запит у друзі від {_currentNotification.SenderName}",
            //             "Прийняти", () => AcceptFriendRequest(),
            //             "Відхилити", () => DeclineFriendRequest());
            //         break;
            //         
            //     case NotificationData.NotificationType.MatchInvite:
            //         _mainMenu.ShowNotification(
            //             $"Запрошення на матч від {_currentNotification.SenderName}",
            //             "Прийняти", () => AcceptMatchInvite(),
            //             "Відхилити", () => DeclineMatchInvite());
            //         break;
            // }
        }

        private void HideNotification()
        {
            _isNotificationActive = false;
            _mainMenu.HideNotification();
        }

        #region Specific Notification Handlers
        private void OnFriendRequestReceived(string senderId, string senderName)
        {
            var notification = new NotificationData
            {
                Type = NotificationData.NotificationType.FriendRequest,
                SenderId = senderId,
                SenderName = senderName,
                Message = "Надіслав вам запит у друзі",
                Timestamp = System.DateTime.Now
            };
            
            HandleNotification(notification);
        }

        private void OnMatchInviteReceived(string senderId, string senderName)
        {
            var notification = new NotificationData
            {
                Type = NotificationData.NotificationType.MatchInvite,
                SenderId = senderId,
                SenderName = senderName,
                Message = "Запрошує вас на матч",
                Timestamp = System.DateTime.Now
            };
            
            HandleNotification(notification);
        }
        #endregion

        #region Action Methods
        // private void AcceptFriendRequest()
        // {
        //     FriendSystem.AcceptRequest(_currentNotification.SenderId);
        //     HideNotification();
        // }
        //
        // private void DeclineFriendRequest()
        // {
        //     FriendSystem.DeclineRequest(_currentNotification.SenderId);
        //     HideNotification();
        // }
        //
        // private void AcceptMatchInvite()
        // {
        //     Matchmaking.AcceptInvite(_currentNotification.SenderId);
        //     HideNotification();
        // }
        //
        // private void DeclineMatchInvite()
        // {
        //     Matchmaking.DeclineInvite(_currentNotification.SenderId);
        //     HideNotification();
        // }
        #endregion
    }
}