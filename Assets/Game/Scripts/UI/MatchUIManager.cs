using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class MatchUIManager : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private EndGamePanel endGamePanel;
    [SerializeField] private PlayerPanel myPlayerPanel;
    [SerializeField] private PlayerPanel enemyPlayerPanel;
    public static MatchUIManager Instance;

    public void EndGame(bool isWin, int myNewElo, int enemyNewElo)
    {
        endGamePanel.EndGame();
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

    private void Awake()
    {
        Instance = this;
        
        backButton.onClick.AddListener(BackToMenu);
    }

    private void BackToMenu()
    {
        NetworkManager.Singleton.Shutdown();
        
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
}