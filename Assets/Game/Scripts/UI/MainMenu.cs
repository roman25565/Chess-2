#if !UNITY_SERVER

using Setting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace UI
{
    public class MainMenu : MonoBehaviour
    {
        [Inject] private Global _global;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private GameObject arrangementPanel;
        [SerializeField] private GameObject editBoard;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject signInPanel;
        
        [SerializeField] private GameObject defaultUI;
        [SerializeField] private GameObject findMatchPanel;
        [SerializeField] private Image profileImage;
        [SerializeField] private TextMeshProUGUI profileImageText;
        [SerializeField] private GameObject gameModeSelectorPanel;
        
        [SerializeField] private PlayerProfileController profilePanel;
        [SerializeField] private NotificationPanel notificationPanel;
        [SerializeField] private GameModeSelector gameModeSelector;
        [SerializeField] private HistoryPanel historyPanel;
        [SerializeField] private FriendsPanel friendsPanel;
        
        
        public void Init(bool isSignIn = false)
        {
            DisableAllPanels();
            DisableFindMatchPanel();
            ShowSignInPanel();

            if (isSignIn) SetProfileImageText(_global.FirestoreManager.PlayerData);
            _global.FirestoreManager.OnLogin.AddListener(() => SetProfileImageText(_global.FirestoreManager.PlayerData));
        }

        public void InitUIComponents()
        {
#if !UNITY_SERVER
            notificationPanel.Init();
            gameModeSelector.Init();
#endif
        }

        private void DisableAllPanels()
        {
            if (settingsPanel != null) settingsPanel.SetActive(false);
            if (arrangementPanel != null) arrangementPanel.SetActive(false);
            if (mainMenuPanel != null) mainMenuPanel.SetActive(false);
            if (editBoard != null) editBoard.SetActive(false);
            if (signInPanel != null) mainMenuPanel.SetActive(false);
            if (notificationPanel != null) notificationPanel.gameObject.SetActive(false);
            if (gameModeSelectorPanel != null) gameModeSelectorPanel.SetActive(false);
            if (profilePanel != null) profilePanel.gameObject.SetActive(false);
            if (historyPanel != null) historyPanel.gameObject.SetActive(false);
            if (friendsPanel != null) friendsPanel.gameObject.SetActive(false);
            
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

        public void ShowProfilePanelAsThisPlayer()
        {
            if (profilePanel == null) return;
            DisableAllPanels();
            profilePanel.SetTargetId(_global.FirestoreManager.PlayerData.ID);
            profilePanel.gameObject.SetActive(true);
        }
        
        public void ShowProfilePanel(string playerID)
        {
            if (profilePanel == null) return;
            DisableAllPanels();
            profilePanel.SetTargetId(playerID);
            profilePanel.gameObject.SetActive(true);
        }
        
        public void ShowEditBoard()
        {
            DisableAllPanels();
            if (editBoard != null) editBoard.SetActive(true);
        }
        public void ShowSignInPanel()
        {
            if (signInPanel == null) return;
            
            DisableAllPanels();
            signInPanel.SetActive(true);
        }
        
        public void EnableFindMatchPanel()
        {
            if (findMatchPanel != null) findMatchPanel.SetActive(true);
        }
        public void DisableFindMatchPanel()
        {
            if (findMatchPanel != null) findMatchPanel.SetActive(false);
        }
        public void DisableSignInPanel()
        {
            if (signInPanel != null) signInPanel.SetActive(false);
            ShowMainMenuPanel();
        }

        public void SetProfileImage(Sprite image)
        {
            if (image == null)
            {
                profileImage.sprite = null;
                profileImage.color = new Color(255, 255, 255, 0);
                return;
            }
            Debug.Log("Set profile image");
            profileImage.sprite = image;
            profileImage.color = new Color(255, 255, 255, 255);
        }

        public void SetProfileImageText(FirebasePlayerData playerData)
        {
            profileImageText.text = $"{playerData.Name}\n({playerData.Elo})";;
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
        
        public void HideHistoryPanel()
        {
            if (historyPanel != null)
            {
                historyPanel.gameObject.SetActive(false);
            }
        }
        public void ShowHistoryPanel()
        {
            if (historyPanel != null)
            {
                historyPanel.gameObject.SetActive(true);
            }
        }

        public void HideFriendsPanel()
        {
            if (friendsPanel != null)
            {
                friendsPanel.gameObject.SetActive(false);
            }
        }
        public void ShowFriendsPanel()
        {
            if (friendsPanel != null)
            {
                friendsPanel.gameObject.SetActive(true);
            }
        }
    }
}

#endif