using System;
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
        [SerializeField] private GameObject botDifficultySelectorPanel;
        
        [SerializeField] private PlayerProfileController profilePanel;
        [SerializeField] private NotificationPanel notificationPanel;
        [SerializeField] private GameModeSelector gameModeSelector;
        [SerializeField] private HistoryPanel historyPanel;
        [SerializeField] private FriendsPanel friendsPanel;
        [SerializeField] private EndGamePanel endGamePanel;
        [SerializeField] private BotDifficultySelector botDifficultySelector;
        
        public void Init(bool isSignIn = false)
        {
            Debug.Log("Init" + isSignIn);
            DisableAllPanels();
            DisableFindMatchPanel();
            if (!isSignIn) ShowSignInPanel();

            if (isSignIn) SetProfileImageText(_global.FirestoreManager.MyData);
            if (isSignIn) SetProfileImage(_global.FirestoreManager.MyData.Icon);
            _global.FirestoreManager.OnLogin.AddListener(OnLogin);
            if (isSignIn)
            {
                var endGameData = _global.EndGameData;
                if (endGameData == null) return;
                endGamePanel.gameObject.SetActive(true);
                
                endGamePanel.EndGame(endGameData, this);
            } 
            return; 
            
            void OnLogin()
            {
                SetProfileImageText(_global.FirestoreManager.MyData);
                if (_global.FirestoreManager.MyData.Icon != null)
                {
                    SetProfileImage(_global.FirestoreManager.MyData.Icon);
                }
                else
                {
                    _global.FirestoreManager.MyData.OnIconLoaded.AddListener(() =>
                        SetProfileImage(_global.FirestoreManager.MyData.Icon));
                }
            }
        }

        public void InitUIComponents()
        {
            notificationPanel.Init();
            gameModeSelector.Init();
            botDifficultySelector.Init();
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
            if (botDifficultySelectorPanel != null) botDifficultySelectorPanel.gameObject.SetActive(false);
            
            if (defaultUI != null) defaultUI.SetActive(true);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                ShowMainMenuPanel();
            }
        }

        public void HideEndGamePanel()
        {
            if (endGamePanel != null) endGamePanel.gameObject.SetActive(false);
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

        public void ShowProfilePanelAsThisPlayer()
        {
            if (profilePanel == null) return;
            DisableAllPanels();
            profilePanel.SetTargetId(_global.FirestoreManager.MyData.ID);
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

        private void SetProfileImageText(MyData player)
        {
            player.GetPlayerRanking(playerRanking =>
            {
                var playerRankingPosition = playerRanking.Position;
                string text = $"{player.Name}\n({playerRanking.Elo.ToString()})";
                if (playerRankingPosition != -1)
                {
                    text += $" #{playerRankingPosition.ToString()}";
                }
                profileImageText.text = text;
            });
        }

        private bool _notificationOpen;
        public void NotificationOnClick()
        {
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
        
        public void ShowBotDifficultySelectorPanel()
        {
            if (botDifficultySelectorPanel != null)
            {
                botDifficultySelectorPanel.gameObject.SetActive(true);
            }
        }
        

        public void HideBotDifficultySelectorPanel()
        {
            if (botDifficultySelectorPanel != null)
            {
                botDifficultySelectorPanel.gameObject.SetActive(false);
            }
        }
    }
}