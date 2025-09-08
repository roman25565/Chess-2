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
        closeBgButton.onClick.AddListener(() => Hide());
    }

    private void OnDestroy()
    {
        ClearListeners();
    }


    public void Show(string text, UnityAction onAccept, UnityAction onReject = null)
    {
        Debug.Log("Show");
        if (_isShown) return;
        _isShown = true;
        
        confirmationPanel.SetActive(true);
        questionText.text = text;
        acceptButton.onClick.AddListener(() => Hide(onAccept));
        rejectButton.onClick.AddListener(() => Hide());
        if (onReject != null) rejectButton.onClick.AddListener(onReject);
    }

    private void Hide(UnityAction onAccept = null)
    {
        Debug.Log("Hide");
        _isShown = false;
        confirmationPanel.SetActive(false);
        ClearListeners();
        try
        {
            onAccept?.Invoke();
            Debug.Log("onAccept");
        }
        catch (Exception e)
        {
            Debug.LogError(e);
            Console.WriteLine(e);
            throw;
        }
    }
    
    private void ClearListeners()
    {
        acceptButton.onClick.RemoveAllListeners();
        rejectButton.onClick.RemoveAllListeners();
    }
}
}