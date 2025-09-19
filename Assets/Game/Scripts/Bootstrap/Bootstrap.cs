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
    [SerializeField] private OnlineStatsFetcher onlineStatsFetcher;
    
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
        // Application.targetFrameRate = 120;
#if UNITY_STANDALONE_WIN && !UNITY_EDITOR
        Screen.SetResolution(500, 1040, false);
#endif
        Debug.Log("currentResolution.width" + Screen.currentResolution.width);
        SetupMatchButtonListeners();
        if (_global.IsSignIn)//If Return To Main Menu
        {
            mainMenu.Init(true);
            mainMenu.InitUIComponents();
            adsManager.TryStartAds();
            soundManager.Init(true);
            _ = advancedMatchmaking.Init();
            signIn.Init(true);
            onlineStatsFetcher.Init(true);
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
        onlineStatsFetcher.Init();
        SetButtonsInteractable();
    }

    private void SetupMatchButtonListeners()
    {
        startOnlineMatch.onClick.AddListener(() => { mainMenu.ShowGameModeSelectorPanel(); });
        startSinglePlayVsBotMatchB.onClick.AddListener(() => { mainMenu.ShowBotDifficultySelectorPanel(); });
        advancedMatchmaking.onStateChanged.AddListener((state =>
        {
            Debug.Log("onStateChanged " + state);
            var interactable = state == MatchmakingState.Cancelled ? true : false;
            startOnlineMatch.interactable = interactable;
            startSinglePlayVsBotMatchB.interactable = interactable;
        }));
        
    }
    
    private void SetButtonsInteractable()
    {
        startOnlineMatch.interactable = false;
        startSinglePlayVsBotMatchB.interactable = false;
        _global.BackendManager.OnLogin.AddListener(() =>
        {
            _global.IsSignIn = true;
            startOnlineMatch.interactable = true;
            startSinglePlayVsBotMatchB.interactable = true;
        });
        _global.BackendManager.OnSignOut.AddListener(() =>
        {
            _global.IsSignIn = false;
            startOnlineMatch.interactable = false;
            startSinglePlayVsBotMatchB.interactable = false;
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
        
        var firestore = new BackendManager(advancedMatchmaking);
        await firestore.Init();
        _global.Init(arrangement, piecesData, cellStates, firestore);
        reconnectFetcher.Init();
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

        _global.BackendManager.LoadPlayerData(user.UserId, CallBack);
        
        return;

        void CallBack(string id, FirebasePlayerData result)
        {
            Debug.Log($"SignInFireBase Result {result} {result == null}");
            if (result == null)
            {
                _global.BackendManager.PlayerDataManager.CreatePlayerData(user);
                _global.BackendManager.StatisticManager.CreatePlayerStatistics(user.UserId);
                _global.BackendManager.PlayerRankingManager.CreateMyPlayerRanking(id);
            }
            else
            {
                _global.BackendManager.Login(result);
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