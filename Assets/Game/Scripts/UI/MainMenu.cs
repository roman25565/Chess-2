#if !UNITY_SERVER

using UnityEngine;
using UnityEngine.UI;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        [SerializeField] private GameObject historyPanel;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject arrangementPanel;
        [SerializeField] private GameObject editBoard;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject signInPanel;
        
        [SerializeField] private GameObject defaultUI;
        [SerializeField] private GameObject findMatchPanel;
        [SerializeField] private Image profileImage;
        [SerializeField] private GameObject profilePanel;
        [SerializeField] private GameObject gameModeSelectorPanel;
        
        [SerializeField] private NotificationPanel notificationPanel;
        [SerializeField] private GameModeSelector gameModeSelector;
        [SerializeField] private HistoryPanel historyPanelUI;
        
        
        public void Init()
        {
            DisableAllPanels();
            DisableFindMatchPanel();
            ShowSignInPanel();
        }

        public void InitUIComponents()
        {
#if !UNITY_SERVER
            notificationPanel.Init();
            gameModeSelector.Init();
            historyPanelUI.Init();
#endif
        }

        private void DisableAllPanels()
        {
            if (historyPanel != null) historyPanel.gameObject.SetActive(false);
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (arrangementPanel != null) arrangementPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (profilePanel != null) profilePanel.SetActive(false);
            if (editBoard != null) editBoard.SetActive(false);
            if (signInPanel != null) mainMenuPanel.SetActive(false);
            if (notificationPanel != null) notificationPanel.gameObject.SetActive(false);
            if (gameModeSelectorPanel != null) gameModeSelectorPanel.SetActive(false);
            
            if (defaultUI != null) defaultUI.SetActive(true);
        }

        #region BackButtonAndroid

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMainMenuPanel();
            }
        }

        #endregion

        public void ShowHistoryPanel()
        {
            DisableAllPanels();
            if (historyPanel != null)
            {
                historyPanel.gameObject.SetActive(true);
            }
        }

        public void ShowSettingsPanel()
        {
            DisableAllPanels();
            if (settingsPanel != null) settingsPanel.SetActive(true);
        }

        public void ShowArrangementPanel()
        {
            DisableAllPanels();
            defaultUI.SetActive(false);
            if (arrangementPanel != null) arrangementPanel.SetActive(true);
        }

        public void ShowMainMenuPanel()
        {
            DisableAllPanels();
            if (mainMenuPanel != null) mainMenuPanel.SetActive(true);
        }

        public void ShowProfilePanel()
        {
            DisableAllPanels();
            if (profilePanel != null) profilePanel.SetActive(true);
        }
        
        public void ShowEditBoard()
        {
            DisableAllPanels();
            if (editBoard != null) editBoard.SetActive(true);
        }
        public void ShowSignInPanel()
        {
            DisableAllPanels();
            if (signInPanel != null) signInPanel.SetActive(true);
        }

        public void EnableFindMatchPanel()
        {
            if (findMatchPanel != null) findMatchPanel.SetActive(true);
        }
        public void DisableFindMatchPanel()
        {
            if (findMatchPanel != null) findMatchPanel.SetActive(false);
        }
        
        public void EnableSignInPanel()
        {
            if (signInPanel != null) signInPanel.SetActive(true);
        }
        public void DisableSignInPanel()
        {
            if (signInPanel != null) signInPanel.SetActive(false);
            ShowMainMenuPanel();
        }

        public void SetProfileImage(Sprite image)
        {
            Debug.Log("Set profile image");
            profileImage.sprite = image;
            profileImage.color = new Color(255, 255, 255, 255);
        }

        private bool _notificationOpen;
        public void NotificationOnClick()
        {
#if !UNITY_SERVER
            if (notificationPanel == null) return;
            
            if (_notificationOpen)
            {
                notificationPanel.gameObject.SetActive(false);
                _notificationOpen = false;
            }
            else
            {
                notificationPanel.gameObject.SetActive(true);
                notificationPanel.OnOpen();
                _notificationOpen = true;
            }
#endif
        }

        public void DisableNotificationPanel()
        {
            notificationPanel.gameObject.SetActive(false);
            _notificationOpen = false;
        }
        
        public void ShowGameModeSelectorPanel()
        {
            if (gameModeSelectorPanel != null) gameModeSelectorPanel.SetActive(true);
        }
        public void HideGameModeSelectorPanel()
        {
            if (gameModeSelectorPanel != null) gameModeSelectorPanel.SetActive(false);
        }
    }
}

#endif