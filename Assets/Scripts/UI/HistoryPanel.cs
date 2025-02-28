using System;
using Setting;
using UnityEngine;
using Zenject;

namespace UI
{
    public class HistoryPanel : MonoBehaviour
    {
        [Inject] private Settings _settings;
        [SerializeField] private Transform parentPanel;
        [SerializeField] private HistotyMatchButton buttonPrefab;
        [SerializeField] private MainMenu mainMenu;

        private void AddButton(HistoryMatchData historyMatchData)
        {
            var button = Instantiate(buttonPrefab, parentPanel);
            button.SetButton(historyMatchData, mainMenu);
        }

        private void OnEnable()
        {
            DestroyButtons();
            var historyIDs = _settings.FirestoreManager.PlayerData.HistoryIDs;
            foreach (var historyID in historyIDs)
            {
                _settings.FirestoreManager.GetHistory(historyID, AddButton);
            }

        }

        private void DestroyButtons()
        {
            Debug.Log("DestroyButtons" + parentPanel.childCount);
            for (int i = parentPanel.childCount - 1; i >= 0; i--)
            {
                Debug.Log($"Destroying {parentPanel.childCount} child");
                Destroy(parentPanel.GetChild(i).gameObject);
            }
        }
    }
}