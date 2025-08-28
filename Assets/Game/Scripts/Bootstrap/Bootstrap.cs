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
using UnityEngine.Serialization;
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
    [SerializeField] private Button startSinglePlayVsBotMatchB;
    [SerializeField] private Button settingsButton;
    
    [SerializeField] private GameObject networkManagerPrefab;

    [SerializeField] private Sprite[] botIcons = new Sprite[4]; // Easy, Medium, Hard, Expert

    public Sprite GetIcon(BotDifficulty difficulty)
    {
        return botIcons[(int)difficulty - 1];
    }
    
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
        startSinglePlayVsBotMatchB.onClick.AddListener(() => { mainMenu.ShowBotDifficultySelectorPanel(); });
        
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
        var icons = new Dictionary<BotDifficulty, Sprite>();
        icons.Add(BotDifficulty.Easy, GetIcon(BotDifficulty.Easy));
        icons.Add(BotDifficulty.Medium, GetIcon(BotDifficulty.Medium));
        icons.Add(BotDifficulty.Hard, GetIcon(BotDifficulty.Hard));
        icons.Add(BotDifficulty.Expert, GetIcon(BotDifficulty.Expert));
        _global.BotIcons = icons;
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
        startSinglePlayVsBotMatchB.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
    }
}
}