using System;
using Setting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class HistotyMatchButton : MonoBehaviour
{
    [Inject] private Settings _settings;
    
    [SerializeField] private Button button;
    
    [SerializeField] private Sprite scopeWin;
    [SerializeField] private Sprite scopeLose;
    [SerializeField] private Sprite scopeDraw;
    [SerializeField] private Image scopeImage;
    
    [SerializeField] private TextMeshProUGUI player1Name;
    [SerializeField] private TextMeshProUGUI player1Elo;
    [SerializeField] private TextMeshProUGUI player2Name;
    [SerializeField] private TextMeshProUGUI player2Elo;
    
    private void Awake()
    {
        ProjectContext.Instance.Container.InjectGameObject(gameObject);
    }
    
    public void SetButton(HistoryMatchData historyMatchData)
    {
        try
        {
            var iFirstPlayer = historyMatchData.Player1Id == _settings.FirestoreManager.PlayerData.ID;
            SetPlayers(historyMatchData);
            SetScope(historyMatchData, iFirstPlayer);
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
    }

    private void SetScope(HistoryMatchData historyMatchData, bool iFirstPlayer)
    {
        Debug.Log("asdasd");
        if (historyMatchData.WinnerID == null)
        {
            Draw();
        }

        bool winFirstPlayer = historyMatchData.WinnerID == historyMatchData.Player1Id;

        if (winFirstPlayer == iFirstPlayer) Win();
        else Lose();
        Debug.Log("ffff");
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
        Debug.Log("kkk");
        player1Name.text = historyMatchData.Player1Name;
        player1Elo.text = $"({historyMatchData.Player1Elo.ToString()})";
        
        player2Name.text = historyMatchData.Player2Name;
        player2Elo.text = $"({historyMatchData.Player2Elo.ToString()})";
        Debug.Log("ttt");
    }
}
