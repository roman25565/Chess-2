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
    [SerializeField] private EndGamePanel endGamePanel;
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
        _enemyPlayerData = enemyPlayerData;
        _myPlayerData = myPlayerData;
        _matchCore = matchCore;
        
        SetTime(myPlayerData.TimeToMove, false);
        SetTime(enemyPlayerData.TimeToMove, true);
    }
    
    public void EndGame(EndGameType type, int myNewElo = 0, int enemyNewElo = 0)
    {
        endGamePanel.EndGame();
        if (type == EndGameType.Draw) return;
        
        myPlayerPanel.EndGame(myNewElo);
        enemyPlayerPanel.EndGame(enemyNewElo);
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

    [Inject] Global _global;
    [SerializeField] private Button backButton;
    [SerializeField] private Button inviteFriendButton;
    [SerializeField] private Button inviteRematchButton;
    [SerializeField] private Button reportButton;
    [SerializeField] private Button surrenderButton;
    [SerializeField] private Button offerDrawButton;
    [SerializeField] private Button cancelMatchButton;

    [SerializeField] private Confirmation confirmation;
    
    private List<Button> _buttons;
    private PlayerData _enemyPlayerData;
    private PlayerData _myPlayerData;
    private MatchCore _matchCore;
    
    
    private void InitButtons()
    {
        _buttons = new List<Button>();
        // _buttons.Add(backButton);
        // _buttons.Add(inviteFriendButton);
        // _buttons.Add(inviteRematchButton);
        // _buttons.Add(reportButton);
        // _buttons.Add(surrenderButton);
        // _buttons.Add(offerDrawButton);
        // _buttons.Add(cancelMatchButton);
        //
        // backButton.onClick.AddListener(BackToMenu);
        // inviteFriendButton.onClick.AddListener(InviteFriend);
        // inviteRematchButton.onClick.AddListener(InviteRematch);
        // reportButton.onClick.AddListener(ReportPlayer);
        // surrenderButton.onClick.AddListener(Surrender);
        // offerDrawButton.onClick.AddListener(OfferDraw);
        // cancelMatchButton.onClick.AddListener(CancelMatch);
        
        if (backButton != null)
        {
            _buttons.Add(backButton);
            backButton.onClick.AddListener(BackToMenu);
        }
    
        if (inviteFriendButton != null)
        {
            _buttons.Add(inviteFriendButton);
            inviteFriendButton.onClick.AddListener(InviteFriend);
        }
    
        if (inviteRematchButton != null)
        {
            _buttons.Add(inviteRematchButton);
            inviteRematchButton.onClick.AddListener(InviteRematch);
        }
    
        if (reportButton != null)
        {
            _buttons.Add(reportButton);
            reportButton.onClick.AddListener(ReportPlayer);
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
            NetworkManager.Singleton.Shutdown();
            SceneManager.LoadScene("Main", LoadSceneMode.Single);
        }
    }
    
    private void InviteFriend()
    {
        confirmation.Show("Invite friend to game?", OnAccept);
        return;

        void OnAccept()
        {
            _ = _global.FirestoreManager.RealtimeDatabase.FriendRequestsManager.SendFriendRequest(_enemyPlayerData.FirebasePlayer.ID,
                _enemyPlayerData.FirebasePlayer.Name);
        }
    }

    private void InviteRematch()
    {
        confirmation.Show("Request a rematch?", OnAccept);
        return;

        void OnAccept()
        {
            _ = _global.FirestoreManager.RealtimeDatabase.MatchRequestsManager.SendMatchRequest(_enemyPlayerData.FirebasePlayer.ID,
                _enemyPlayerData.FirebasePlayer.Name);
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
            _matchCore.TrySurrenderRpc(_enemyPlayerData.PlayerId);
        }
    }

    private void OfferDraw()
    {
        confirmation.Show("Offer a draw?", OnAccept);
        return;

        void OnAccept()
        {
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
        confirmation.Show("Cancel current match?", OnAccept);
        return;

        void OnAccept()
        {
            _matchCore.AcceptAnotherPlayerWantsDrawRpc();
        }
    }
}