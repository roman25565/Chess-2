using System;
using UnityEngine;
using UnityEngine.Serialization;

public class MainMenu : MonoBehaviour
{
    [SerializeField] private Canvas menuCanvas;
    [SerializeField] private Canvas arrangementCanvas;

    private void Start()
    {
        ShowMenu();
    }

    public void ShowMenu()
    {
        menuCanvas.gameObject.SetActive(true);
        arrangementCanvas.gameObject.SetActive(false);
    }

    public void ShowArrangementC()
    {
        menuCanvas.gameObject.SetActive(false);
        arrangementCanvas.gameObject.SetActive(true);
    }
}