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

        private void AddButton(HistoryMatchData historyMatchData)
        {
            var button = Instantiate(buttonPrefab, parentPanel);
            button.SetButton(historyMatchData);
        }

        private void OnEnable()
        {

            var historyIDs = _settings.FirestoreManager.PlayerData.HistoryIDs;
            foreach (var historyID in historyIDs)
            {
                _settings.FirestoreManager.GetHistory(historyID, AddButton);
            }

        }
    }
}