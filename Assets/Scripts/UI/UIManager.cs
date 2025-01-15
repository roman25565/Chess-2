using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    [SerializeField] private Button backButton;
    [SerializeField] private EndGamePanel endGamePanel;
    [SerializeField] private PlayerPanel myPlayerPanel;
    [SerializeField] private PlayerPanel enemyPlayerPanel;
    public static UIManager instance;

    public void EndGame()
    {
        endGamePanel.EndGame();
    }

    public void SetTime(float time, bool isEnemyPlayer)
    {
        if (isEnemyPlayer) enemyPlayerPanel.SetTime(time);
        else myPlayerPanel.SetTime(time);
    }

    private void Start()
    {
        instance = this;
        
        backButton.onClick.AddListener(BackToMenu);
    }

    private void BackToMenu()
    {
        NetworkManager.Singleton.Shutdown();
        SceneManager.LoadScene("Main", LoadSceneMode.Single);
    }
}