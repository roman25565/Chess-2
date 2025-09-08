using System;
using Setting;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class EndGamePanel : MonoBehaviour
{
    [Inject] private Global _global;
    [Inject] private GameData _gameData;

    [SerializeField] private EndGameProfile myProfile;
    [SerializeField] private EndGameProfile enemyProfile;
    [SerializeField] private Button addToFriendB;
    [SerializeField] private Button rematchB;
    [SerializeField] private Button viewHistoryB;

    public void EndGame(EndGameData endGameData, MainMenu mainMenu)
    {
        Debug.Log($"EndGameUI {endGameData.Type} {endGameData.MyNewElo}, {endGameData.WonReason}, {endGameData.MyPlayerData.FirebasePlayer.ID}");
        var enemyId = endGameData.EnemyPlayerData.FirebasePlayer.ID;

        myProfile.EndGame(endGameData.MyPlayerData, endGameData.MyNewElo, mainMenu);
        enemyProfile.EndGame(endGameData.EnemyPlayerData, endGameData.EnemyNewElo, mainMenu);

        var isMyFriend = _global.FirestoreManager.MyData.FriendIdsContains(enemyId);
        if (isMyFriend)
        {
            addToFriendB.interactable = false;
        }
        else
        {
            addToFriendB.interactable = true;
            addToFriendB.onClick.AddListener(() => AddToFriend(enemyId));
        }

        if (endGameData.IsLocal)
        {
            addToFriendB.interactable = false;
            rematchB.interactable = (false);
            viewHistoryB.interactable = (false);
            return;
        }
        rematchB.interactable = (true);
        viewHistoryB.interactable = (true);
        rematchB.onClick.AddListener(() => Rematch(enemyId));
        viewHistoryB.onClick.AddListener(() =>
        {
            _global.FirestoreManager.LoadOneHistory(_global.FirestoreManager.MyData.ID, endGameData.MatchId,
                (historyMatchData) =>
                {
                    viewHistoryB.onClick.RemoveAllListeners();
                    mainMenu.ShowEditBoard();
                    _gameData.ActiveBoard.ArrangeFigures(historyMatchData);

                });
        });
    }

    private void Rematch(string enemyId)
    {
        rematchB.onClick.RemoveAllListeners();
        
        _global.FirestoreManager.MyData.SendMatchRequest(enemyId);
        rematchB.interactable = false;
    }

    private void AddToFriend(string enemyId)
    {
        addToFriendB.onClick.RemoveAllListeners();
        
        _global.FirestoreManager.MyData.SendFriendRequest(enemyId);
        addToFriendB.interactable = false;
    }
}