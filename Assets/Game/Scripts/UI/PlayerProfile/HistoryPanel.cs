using System;
using System.Collections.Generic;
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
        private string _targetPlayerId;
        
        
        private void AddButton(HistoryMatchData historyMatchData)
        {
            var button = Instantiate(buttonPrefab, parentPanel);
            button.SetButton(historyMatchData, mainMenu);
        }
<<<<<<< Updated upstream:Assets/Game/Scripts/UI/PlayerProfile/HistoryPanel.cs
=======
        
        public void Init()
        {
            // _global.FirestoreManager.OnLogin.AddListener(OnLogin);
        }

        private void OnLogin()
        {
            SetTargetId(_global.FirestoreManager.PlayerData.ID);
        }
>>>>>>> Stashed changes:Assets/Game/Scripts/UI/HistoryPanel.cs

        public void SetTargetId(string id)
        {
            _targetPlayerId = id;
        }

        public void OnEnable()
        {
            DestroyButtons();
            if (_targetPlayerId == null) return;
            _global.FirestoreManager.LoadHistory(_targetPlayerId,AddButtons);
        }
        
        private void OnDisable()
        {
            _targetPlayerId = null;
        }

        private void AddButtons(string id, List<HistoryMatchData> historyMatches)
        {
            if (id != _targetPlayerId) return;
            if (historyMatches.Count == 0)
            {
                OnEmpty();
                return;
            }
            
            foreach (var historyMatchData in historyMatches)
            {
                AddButton(historyMatchData);
            }
        }

        private void OnEmpty()
        {
            throw new NotImplementedException();
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
