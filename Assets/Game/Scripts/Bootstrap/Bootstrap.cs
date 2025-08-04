using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google;
using Newtonsoft.Json;
using Setting;
using UI;
using Unity.Netcode;
using UnityEditor;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using Zenject;

namespace Bootstrap
{
public class Bootstrap : MonoBehaviour
{
    private const string Server = "Server";

    [Inject] private GameData _gameData;
    [Inject] private Global _global;
    
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            signIn = GetComponent<SignIn>();
            mainMenu = GetComponent<MainMenu>();
            adsManager = GetComponent<ADSManager>();

            if (advancedMatchmaking == null || signIn == null || mainMenu == null || adsManager == null)
            {
                Debug.LogWarning("Деякі компоненти відсутні на цьому GameObject", this);
            }
        }
    }
#endif
    [SerializeReference] private AdvancedMatchmaking advancedMatchmaking;
    [SerializeReference] private SignIn signIn;
    [SerializeReference] private MainMenu mainMenu;
    [SerializeReference] private SoundManager soundManager;
    [SerializeReference] private ADSManager adsManager;
    [SerializeField] private Button startOnlineMatch;
    [SerializeField] private Button startLocalMatch;
    [SerializeField] private Button startTestMatch;
    [SerializeField] private Button hostLocalMatch;
    [SerializeField] private Button settingsButton;

    private async void Awake()
    {
        Application.targetFrameRate = 120;
        if (_global.IsSignIn)//Is Return To Main Menu
        {
            mainMenu.Init(true);
            mainMenu.InitUIComponents();
            signIn.Init(true);
            adsManager.TryStartAds();
            soundManager.Init(true);
            return;
        }
        
        await LoadSettings();

        mainMenu.InitUIComponents();
        signIn.Init();
        mainMenu.Init();
        soundManager.Init(true);
        adsManager.Init();

        startOnlineMatch.onClick.AddListener(() => { mainMenu.ShowGameModeSelectorPanel(); });
        startLocalMatch.onClick.AddListener(() =>
        {
            DisableButtons();
            _gameData.Mode = GameMode.Offline;
            NetworkManager.Singleton.StartClient();
        });
        startTestMatch.onClick.AddListener(() =>
        {
            DisableButtons();

            _gameData.Mode = GameMode.Test;
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);

            SceneManager.sceneLoaded += OnSceneLoaded;

            void OnSceneLoaded(Scene scene, LoadSceneMode loadSceneMode)
            {
                NetworkManager.Singleton.StartHost();

                SceneManager.sceneLoaded -= OnSceneLoaded;
            }
        });
        hostLocalMatch.onClick.AddListener(() =>
        {
            DisableButtons();
            SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
            NetworkManager.Singleton.StartServer();
        });

    }

    private async Task LoadSettings()
    {
        var arrangement = LoadArrangement();

        var piecesData = Resources.LoadAll<PieceData>("Settings/Pieces");
        if (piecesData == null)
            Debug.LogError("PieceData not found");
        Debug.Log("piecesData" + piecesData.Length);

        var cellStates = Resources.Load<CellStates>("Settings/CellStates");
        if (cellStates == null)
            Debug.LogError("CellStates not found");
        
        var firestore = new FirestoreManager(advancedMatchmaking);
        
        await firestore.Init();
        

        _global.Init(arrangement, piecesData, cellStates, firestore);
    }

    public void OnSignIn(GoogleSignInUser user)
    {
        SignInFireBase(user);
    }

    public void OnSignInDebug(GoogleSignInUser user)
    {
        SignInFireBase(user);
    }

    private void SignInFireBase(GoogleSignInUser user)
    {
        Debug.Log("SignInFireBase " + user);

        _ = _global.FirestoreManager.PlayerDataManager.GetPlayerData(user.UserId, CallBack);
        
        return;

        void CallBack(FirebasePlayerData result)
        {
            if (result == null)
            {
                _global.FirestoreManager.PlayerDataManager.CreatePlayerData(user);
                _ = _global.FirestoreManager.StatisticManager.CreatePlayerStatistics(user.UserId);
            }
            else
            {
                _global.IsSignIn = true;
                _global.FirestoreManager.Login(result);
            }
        }
    }


    private List<ArrangementEntry> LoadArrangement()
    {
        var filePath = Application.persistentDataPath + "/game_pieces.json";

        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var pieceData = JsonConvert.DeserializeObject<List<ArrangementEntry>>(json);
            return pieceData;
        }

        return Resources.Load<Arrangement>("Settings/Arrangement").arrangements;
    }

    private void DisableButtons()
    {
        startOnlineMatch.gameObject.SetActive(false);
        startLocalMatch.gameObject.SetActive(false);
        startTestMatch.gameObject.SetActive(false);
        hostLocalMatch.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
    }
}
}