#if !UNITY_SERVER
using UnityEngine;
using TMPro;
using Firebase.RealtimeDatabase.Data;
using Setting;
using Zenject;
using Type = Firebase.RealtimeDatabase.Data.Type;
namespace UI
{
public class NotificationPanel : MonoBehaviour
{
    private const string EmptyText = "Null";

    [SerializeField] private MainMenu _mainMenu;
    [SerializeField] private Transform parent;
    [SerializeField] private Notification prefab;

    [SerializeField] private TextMeshProUGUI emptyText;
    [SerializeField] private TextMeshProUGUI notificationCountText;
    [SerializeField] private GameObject notificationCountPanel;
    [Inject] private Global _global;

    public void Init()
    {
        _global.FirestoreManager.OnLogin.AddListener(Subscribe);
    }

    private void Subscribe()
    {
        Debug.Log("Subscribed");
        UpdateNotificationCount();
        _global.FirestoreManager.RealtimeDatabase.OnChangedRequests.AddListener(OnChanged);
    }

    public void OnDestroy()
    {
        _global.FirestoreManager.OnLogin.RemoveListener(Subscribe);
        _global.FirestoreManager.RealtimeDatabase.OnChangedRequests?.RemoveListener(OnChanged);
    }

    public void OnOpen()
    {
        UpdateNotifications();
        UpdateNotificationCount(0);
    }


    private void OnChanged()
    {
        Debug.Log("OnChanged");
        UpdateNotificationCount();
        if (gameObject.activeInHierarchy)
        {
            UpdateNotifications();
        }
    }

    private void UpdateNotificationCount(int count = -1)
    {
        if (count < 0)
            count = _global.FirestoreManager.RealtimeDatabase.GetRequests.Count;
        if (count == 0)
        {
            // notificationCountPanel.SetActive(false);
        }
        else
        {
            // notificationCountPanel.SetActive(true);
        }
    }

    private void UpdateNotifications()
    {
        DestroyAllNotifications();
        
        var requests = _global.FirestoreManager.RealtimeDatabase.GetRequests;
        if (requests.Count == 0)
        {
            emptyText.text = EmptyText;
        }
        else
        {
            emptyText.text = "";


            foreach (var request in requests)
            {
                CreateNotification(request);
            }
        }
    }

    private void DestroyAllNotifications()
    {
        int childCount = parent.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    private void CreateNotification(AbstractRequestData request)
    {
        var realtimeDatabase = _global.FirestoreManager.RealtimeDatabase;
        var notification = Instantiate(prefab, parent);
        notification.Init(
            GetNotificationText(request.RequestType, request.SenderName),
            () =>
            {
                _mainMenu.DisableNotificationPanel();
                if (request.RequestType == Type.MatchRequest) _mainMenu.EnableFindMatchPanel();
                realtimeDatabase.AcceptInvite(request);
            },
            () => realtimeDatabase.DeclineInvite(request));
    }

    private string GetNotificationText(Type type, string senderName)
    {
        var result = "";
        switch (type)
        {
            case Type.FriendRequest:
                result = $"{senderName} sent you a friend request";
                break;
            case Type.MatchRequest:
                result = $"{senderName} wants to play a match with you";
                break;
            default:
                Debug.LogError("Unknown notification type");
                break;
        }

        return result;
    }
}
}
#endif
