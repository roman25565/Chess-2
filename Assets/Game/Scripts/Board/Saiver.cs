using System;
using System.Collections.Generic;
using Setting;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.Board
{
public class Saiver : MonoBehaviour
{
    [Inject] Global _global;
    
    [SerializeField] private GameObject panel;
    [SerializeField] private Button button1;
    [SerializeField] private Button button2;
    [SerializeField] private Button button3;
    
    [SerializeField] private TextMeshProUGUI saveButtonText;
    
    

    private Action<int> _action;

    public void OnShowButtonClick()
    {
        _action = Select;
        UpdateButtonsInteractable();
        ShowPanel();
    }

    private void UpdateButtonsInteractable()
    {
        DisableButtons();

        var arr = _global.SavedArrangements;
        var buttons = new[] { button1, button2, button3 };

        for (int i = 0; i < buttons.Length; i++)
        {
            if (arr.ContainsKey(i) && arr[i] != null)
            {
                buttons[i].interactable = true;
            }
        }
    }

    public void OnShowSave(List<ArrangementEntry> arr)
    {
        _action = (index) =>
        {
            _global.SetArrangement(index, arr);
            HidePanel();
            saveButtonText.text = index.ToString();
        };
        EnableButtons();
        ShowPanel();
    }


    public void HidePanel()
    {
        panel.SetActive(false);
        _action = null;
    }
    
    private void ShowPanel()
    {
        panel.SetActive(true);
    }

    private void Select(int i)
    {
        _global.SetSelectedArrangement(i);
        HidePanel();
    }

    private void OnEnable()
    {
        SubscriptionButtons();
    }
    
    private void OnDisable()
    {
        RemoveListeners();
    }

    private void SubscriptionButtons()
    {
        button1.onClick.AddListener(() => _action(0));
        button2.onClick.AddListener(() => _action(1));
        button3.onClick.AddListener(() => _action(2));
    }

    private void RemoveListeners()
    {
        button1.onClick.RemoveAllListeners();
        button2.onClick.RemoveAllListeners();
        button3.onClick.RemoveAllListeners();
    }

    private void DisableButtons()
    {
        SetInteractive(false);
    }
    private void EnableButtons()
    {
        SetInteractive(true);
    }

    private void SetInteractive(bool value)
    {
        button1.interactable = value;
        button2.interactable = value;
        button3.interactable = value;
    }
}
}
