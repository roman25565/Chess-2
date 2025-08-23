using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace UI
{
public class Confirmation : MonoBehaviour
{
    [SerializeField] private GameObject confirmationPanel;
    [SerializeField] private TextMeshProUGUI questionText;
    [SerializeField] private Button acceptButton;
    [SerializeField] private Button rejectButton;
    [SerializeField] private Button closeBgButton;
    
    private bool _isShown;

    private void Start()
    {
        Hide();
    }

    private void OnDestroy()
    {
        ClearListeners();
    }


    public void Show(string text, UnityAction onAccept, UnityAction onReject = null)
    {
        if (_isShown) return;
        _isShown = true;
        
        confirmationPanel.SetActive(true);
        questionText.text = text;
        acceptButton.onClick.AddListener(onAccept);
        acceptButton.onClick.AddListener(Hide);
        rejectButton.onClick.AddListener(Hide);
        if (onReject != null) rejectButton.onClick.AddListener(onReject);
        closeBgButton.onClick.AddListener(Hide);
    }

    private void Hide()
    {
        _isShown = false;
        confirmationPanel.SetActive(false);
        ClearListeners();
    }
    
    private void ClearListeners()
    {
        acceptButton.onClick.RemoveAllListeners();
        rejectButton.onClick.RemoveAllListeners();
        closeBgButton.onClick.RemoveAllListeners();
    }
}
}