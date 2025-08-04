using System.Collections.Generic;
using Game.Scripts.UI.PlayerProfile;
using Setting;
using UnityEngine;
using Zenject;

namespace UI
{
public class FriendsPanel : MonoBehaviour
{
    [SerializeField] private FriendsButton buttonPrefab;
    [SerializeField] private MainMenu mainMenu;
    [SerializeField] private Transform parentPanel;
    [Inject] private Global _global;
    private string _targetPlayerId;
        
    private List<Notification> _notifications = new();
    
    public void SetTargetId(string id)
    {
        _targetPlayerId = id;
    }
    public void ReloadUI()
    {
        DestroyButtons();
        if (_targetPlayerId == null) return;
        _global.FirestoreManager.LoadPlayerData(_targetPlayerId,(arg, player) =>
        {
            player.FriendIds.ForEach(id =>
            {
                _global.FirestoreManager.LoadPlayerData(id, AddButton);
            });
        });
    }
    
    private void AddButton(string id, FirebasePlayerData player)
    {
        Debug.Log($"AddButton {id}, {player == null}");
        if (player == null) return;
        
        var button = Instantiate(buttonPrefab, parentPanel);
        ProjectContext.Instance.Container.InjectGameObject(button.gameObject);
        button.SetButton(player, mainMenu, transform, _notifications);
    }

        
    private void OnDisable()
    {
        _targetPlayerId = null;
    }


    private void DestroyButtons()
    {
        Debug.Log("DestroyButtons" + parentPanel.childCount);
        for (int i = parentPanel.childCount - 1; i >= 0; i--)
        {
            Debug.Log($"Destroying {parentPanel.childCount} child");
            Destroy(parentPanel.GetChild(i).gameObject);
        }
    }
}
}