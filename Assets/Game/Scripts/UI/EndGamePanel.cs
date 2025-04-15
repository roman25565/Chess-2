using System;
using UnityEngine;
using UnityEngine.Serialization;
using UnityEngine.UI;

public class EndGamePanel : MonoBehaviour
{
    [SerializeField] private Button closePanelButton;
    [SerializeField] private GameObject endGamePanel;

    public void EndGame()
    {
        endGamePanel.SetActive(true);
    }

    private void Start()
    {
        closePanelButton.onClick.AddListener(ClosePanel);
        ClosePanel();
    }

    private void ClosePanel()
    {
        endGamePanel.SetActive(false);
    }

}