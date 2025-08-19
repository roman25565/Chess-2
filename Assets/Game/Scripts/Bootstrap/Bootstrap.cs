using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google;
using Newtonsoft.Json;
using Setting;
using TMPro;
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
    [Inject] private GameData _gameData;
    [Inject] private Global _global;
    
    [SerializeReference] private AdvancedMatchmaking advancedMatchmaking;
    [SerializeReference] private SignIn signIn;
    [SerializeReference] private MainMenu mainMenu;
    [SerializeReference] private SoundManager soundManager;
    [SerializeReference] private ADSManager adsManager;
    [SerializeField] private ReconnectFetcher reconnectFetcher;
    
    [SerializeField] private Button startOnlineMatch;
    [SerializeField] private Button startLocalMatch;
    [SerializeField] private Button startTestMatch;
    [SerializeField] private Button hostLocalMatch;
    [SerializeField] private Button settingsButton;
    
    [SerializeField] private GameObject networkManagerPrefab;
    
    private async void Awake()
    {
        Application.targetFrameRate = 120;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Screen.SetResolution(500, 1040, false);
#endif
        Debug.Log("currentResolution.width" + Screen.currentResolution.width);
        SetupMatchButtonListeners();
        if (_global.IsSignIn)//Is Return To Main Menu
        {
            mainMenu.Init(true);
            mainMenu.InitUIComponents();
            adsManager.TryStartAds();
            soundManager.Init(true);
            _ = advancedMatchmaking.Init();
            signIn.Init(true);
            return;
        }
        
        if(NetworkManager.Singleton == null) Instantiate(networkManagerPrefab);
        await LoadSettings();

        mainMenu.InitUIComponents();
        mainMenu.Init();
        soundManager.Init(true);
        adsManager.Init();
        _ = advancedMatchmaking.Init();
        signIn.Init();
    }

    private void SetupMatchButtonListeners()
    {
        startOnlineMatch.onClick.AddListener(() => { mainMenu.ShowGameModeSelectorPanel(); });
        advancedMatchmaking.onStateChanged.AddListener((state =>
        {
            startOnlineMatch.interactable = state == MatchmakingState.Cancelled ? true : false;
        }));
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
        firestore.OnLogin.AddListener((() =>
        {
            reconnectFetcher.StartFetching(() => firestore.RealtimeDatabase.ReConnectRequestsManager.FetchReConnectRequests());
        }));
        
        
        await firestore.Init();
        

        _global.Init(arrangement, piecesData, cellStates, firestore);
    }

    public void OnSignIn(GoogleSignInUser user, SignTypes signType)
    {
        SignInFireBase(user);
        _ = advancedMatchmaking.OnSignIn(user.IdToken, signType);
    }

    public void OnSignInDebug(GoogleSignInUser user)
    {
        SignInFireBase(user);
        _ = advancedMatchmaking.OnSignIn(user.UserId, SignTypes.None);;
    }
    
    public void OnSignInAnonymously(GoogleSignInUser user)
    {
        SignInFireBase(user);
        _ = advancedMatchmaking.OnSignIn(user.UserId, SignTypes.Anonymous);;
    }

    private void SignInFireBase(GoogleSignInUser user)
    {
        Debug.Log("SignInFireBase " + user);

        _global.FirestoreManager.LoadPlayerData(user.UserId, CallBack);
        
        return;

        void CallBack(string id, FirebasePlayerData result)
        {
            if (result == null)
            {
                _global.FirestoreManager.PlayerDataManager.CreatePlayerData(user);
                _global.FirestoreManager.StatisticManager.CreatePlayerStatistics(user.UserId);
                _global.FirestoreManager.PlayerRankingManager.CreateMyPlayerRanking(id);
                _global.IsSignIn = true;
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