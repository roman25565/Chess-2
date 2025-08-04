using System;
using System.Collections;
using Setting;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class HistotyMatchButton : MonoBehaviour
{
#if !UNITY_SERVER
    [Inject] private Global _global;
    [Inject] private GameData _gameData;
    
    [SerializeField] private Button button;
    
    [SerializeField] private Sprite scopeWin;
    [SerializeField] private Sprite scopeLose;
    [SerializeField] private Sprite scopeDraw;
    [SerializeField] private Image scopeImage;
    
    [SerializeField] private TextMeshProUGUI player1NameAndElo;
    [SerializeField] private TextMeshProUGUI player2NameAndElo;
    
    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }

    public void SetButton(HistoryMatchData historyMatchData, MainMenu mainMenu, string targetPlayerId)
    {
        var iFirstPlayer = historyMatchData.FirestorePlayer1Id == targetPlayerId;
        SetPlayers(historyMatchData);
        SetScope(historyMatchData, iFirstPlayer);

        button.onClick.AddListener(() =>
        {
            mainMenu.ShowEditBoard();
            _gameData.ActiveBoard.ArrangeFigures(historyMatchData);
            //ShowEditBoardAndStartGame(historyMatchData, mainMenu);
        });
    }

    private void SetScope(HistoryMatchData historyMatchData, bool iFirstPlayer)
    {
        if (historyMatchData.WinnerID == null)
        {
            Draw();
        }

        bool winFirstPlayer = historyMatchData.WinnerID == historyMatchData.FirestorePlayer1Id;

        if (winFirstPlayer == iFirstPlayer) Win();
        else Lose();
        return;
        
        void Win()
        {
            scopeImage.color = Color.green;
        }
        void Lose()
        {
            scopeImage.color = Color.red;
        }
        void Draw()
        {
            scopeImage.color = Color.gray;
        }
    }

    private void SetPlayers(HistoryMatchData historyMatchData)
    {
        player1NameAndElo.text = $"{historyMatchData.Player1Name} ({historyMatchData.Player1Elo})";
        player2NameAndElo.text = $"{historyMatchData.Player2Name} ({historyMatchData.Player2Elo})";
    }
#endif
}
