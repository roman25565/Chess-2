#if !UNITY_SERVER
using System;
using Setting;
using UnityEngine;
using Zenject;

namespace UI
{
    public class HistoryPanel : MonoBehaviour
    {
        [SerializeField] private HistotyMatchButton buttonPrefab;
        [SerializeField] private MainMenu mainMenu;
        [SerializeField] private Transform parentPanel;
        [Inject] private Global _global;

        private void AddButton(HistoryMatchData historyMatchData)
        {
            var button = Instantiate(buttonPrefab, parentPanel);
            button.SetButton(historyMatchData, mainMenu);
        }

        private void OnEnable()
        {
            DestroyButtons();
            var historyIDs = _global.FirestoreManager.PlayerData?.HistoryMatchIDs;
            if (historyIDs == null)
            {
                return;
            }
            foreach (var historyID in historyIDs)
            {
                _global.FirestoreManager.GetHistory(historyID, AddButton);
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
#endif
