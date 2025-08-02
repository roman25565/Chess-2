using UnityEngine;
using UnityEngine.Serialization;
#if !UNITY_SERVER
using System;
using UnityEngine.UIElements;
using System.Collections.Generic;
using Setting;
using Statistics;
using Zenject;
#endif
namespace UI
{

public class PlayerProfileController : MonoBehaviour
{

    [Inject] private Global _global;
    private string _targetPlayerId;

    public void SetTargetId(string id)
    {
        _targetPlayerId = id;
        historyPanel.SetTargetId(id);
    }
    
    public void OnEnable()
    {
        if (_targetPlayerId == null) return;
        _global.FirestoreManager.LoadHistory(_targetPlayerId, UpdateEloGraph);
        historyPanel.OnEnable();
        friendsPanel.OnEnable();
    }

    private void OnDisable()
    {
        _targetPlayerId = null;
        HideHistoryPanel();
        HideFriendsPanel();
    }

    public void UpdateProfile(FirebasePlayerData player)
    {
        
    }

    private void UpdateEloGraph(string id,List<HistoryMatchData> matches)
    {
        
    }

    #region Bottom Panels
    [SerializeField] private HistoryPanel historyPanel;
    [SerializeField] private FriendsPanel friendsPanel;

    public void HideHistoryPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(false);
        }
    }
    public void ShowHistoryPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(true);
        }
    }

    public void HideFriendsPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(false);
        }
    }
    public void ShowFriendsPanel()
    {
        if (historyPanel != null)
        {
            historyPanel.gameObject.SetActive(true);
        }
    }
    #endregion
}

}