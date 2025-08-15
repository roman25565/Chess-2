using UnityEngine;
#if !UNITY_SERVER
using System;
using System.Collections.Generic;
using Setting;
using UI;
using Unity.Netcode;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;
#endif
public class MatchUIManager : MonoBehaviour
{
#if !UNITY_SERVER
    [SerializeField] private PlayerPanel myPlayerPanel;
    [SerializeField] private PlayerPanel enemyPlayerPanel;
    
    public static MatchUIManager Instance;
    
    private void Awake()
    {
        Instance = this;

        InitButtons();
    }

    public void Init(PlayerData enemyPlayerData, PlayerData myPlayerData, MatchCore matchCore)
    {
        _matchCore = matchCore;
        
        SetTime(myPlayerData.TimeToMove, false);
        SetTime(enemyPlayerData.TimeToMove, true);
    }

    public void SetPlayerUI(FirebasePlayerData playerData, bool isEnemyPlayer)
    {
        Debug.Log("SetPlayerUI(FirebasePlayerData playerData, bool");;
        if (isEnemyPlayer) enemyPlayerPanel.SetPlayerUI(playerData);
        else myPlayerPanel.SetPlayerUI(playerData);
    }

    public void SetTime(float time, bool isEnemyPlayer)
    {
        if (isEnemyPlayer) enemyPlayerPanel.SetTime(time);
        else myPlayerPanel.SetTime(time);
    }

    #region ButtonsHandlers

    [Inject] private Global _global;
    [SerializeField] private Button backButton;
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button offerDrawButton;
    [SerializeField] private Button cancelMatchButton;

    [SerializeField] private Confirmation confirmation;
    
    private List<Button> _buttons;
    private PlayerData _enemyPlayerData;
    private MatchCore _matchCore;
    
    
    private void InitButtons()
    {
        _buttons = new List<Button>();
        
        if (backButton != null)
        {
            _buttons.Add(backButton);
            backButton.onClick.AddListener(BackToMenu);
        }
    
        if (surrenderButton != null)
        {
            _buttons.Add(surrenderButton);
            surrenderButton.onClick.AddListener(Surrender);
        }
    
        if (offerDrawButton != null)
        {
            _buttons.Add(offerDrawButton);
            offerDrawButton.onClick.AddListener(OfferDraw);
        }
    
        if (cancelMatchButton != null)
        {
            _buttons.Add(cancelMatchButton);
            cancelMatchButton.onClick.AddListener(CancelMatch);
        }

    }

    private void BackToMenu()
    {
        confirmation.Show("Exit game?", OnAccept);
        return;

        void OnAccept()
        {
            _matchCore.TrySurrenderRpc();
        }
    }

    private void ReportPlayer()
    {
        confirmation.Show("Report player?", OnAccept);
        return;

        void OnAccept()
        {
            _global.FirestoreManager.StatisticManager.ReportAnotherPlayer(_enemyPlayerData.FirebasePlayer.ID);
        }
    }

    private void Surrender()
    {
        confirmation.Show("Surrender the game?", OnAccept);
        return;

        void OnAccept()
        {
            _matchCore.TrySurrenderRpc();
        }
    }

    private void OfferDraw()
    {
        confirmation.Show("Offer a draw?", OnAccept);
        return;

        void OnAccept()
        {
            Debug.Log("OfferDraw" + (_matchCore != null));
            _matchCore.TryOfferDrawRpc();
        }
    }

    private void CancelMatch()
    {
        confirmation.Show("Cancel current match?", OnAccept);
        return;

        void OnAccept()
        {
            _matchCore.TryCancelMatchRpc();
        }
    }

    
    private void OnDestroy()
    {
        foreach (var button in _buttons)
        {
            if (button == null) continue;
            button.onClick.RemoveAllListeners();
        }
    }



    #endregion
    
    #region BackButtonAndroid

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            Surrender();
        }
    }

    #endregion
#endif
    public void OnAnotherPlayerWantsDrawRpc()
    {
        confirmation.Show("Accept for a draw", OnAccept);
        return;

        void OnAccept()
        {
            _matchCore.AcceptAnotherPlayerWantsDrawRpc();
        }
    }
}