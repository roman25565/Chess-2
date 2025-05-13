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
    
#if !UNITY_SERVER
#if UNITY_EDITOR
    private void OnValidate()
    {
        if (!Application.isPlaying)
        {
            clientMatchmaker = GetComponent<ClientMatchmaker>();
            signIn = GetComponent<SignIn>();
            mainMenu = GetComponent<MainMenu>();
            adsManager = GetComponent<ADSManager>();

            if (clientMatchmaker == null || signIn == null || mainMenu == null || adsManager == null)
            {
                Debug.LogWarning("Деякі компоненти відсутні на цьому GameObject", this);
            }
        }
    }
#endif
    [SerializeReference] private ClientMatchmaker clientMatchmaker;
    [SerializeReference] private SignIn signIn;
    [SerializeReference] private MainMenu mainMenu;
    [SerializeReference] private ADSManager adsManager;
#endif
    [SerializeField] private Button startOnlineMatch;
    [SerializeField] private Button startLocalMatch;
    [SerializeField] private Button startTestMatch;
    [SerializeField] private Button hostLocalMatch;
    [SerializeField] private Button settingsButton;
    
    private async void Awake()
    {
        Application.targetFrameRate = 60;
#if !UNITY_SERVER
        if (_global.IsSignIn)
        {
            signIn.Init();
            adsManager.TryStartAds();
            return;
        }
        mainMenu.Init();
#endif
        var isServer = Environment.GetCommandLineArgs().Any(arg => arg == "-port");
        // isServer = true;//TO Test
        await LoadSettings(!isServer);
        if (isServer)
        {
            Debug.Log("Starting server");
            SceneManager.LoadScene(Server);
        }
        else
        {
#if !UNITY_SERVER
            mainMenu.InitUIComponents();
            signIn.Init();
            clientMatchmaker.Init();
            adsManager.Init();
            
            startOnlineMatch.onClick.AddListener(() =>
            {
                mainMenu.ShowGameModeSelectorPanel();
            });
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
#endif
        }
    }

    private async Task LoadSettings(bool isClient)
    {
        var arrangement = LoadArrangement();

        var piecesData = Resources.LoadAll<PieceData>("Settings/Pieces");
        if (piecesData == null)
            Debug.LogError("PieceData not found");
        Debug.Log("piecesData" + piecesData.Length);

        var cellStates = Resources.Load<CellStates>("Settings/CellStates");
        if (cellStates == null)
            Debug.LogError("CellStates not found");
        
#if !UNITY_SERVER
        var firestore = new FirestoreManager(clientMatchmaker);
        if (isClient)
        {
            await firestore.Init();
        }

        _global.Init(arrangement, piecesData, cellStates, firestore);
#else
        _global.Init(arrangement, piecesData, cellStates);
        #endif
    }

    public void OnSignIn(GoogleSignInUser user)
    {
        SignInFireBase(user);
    }

    public void OnSignInDebug(string id)
    {
        SignInFireBase(new GoogleSignInUser { UserId = id });
    }

    private void SignInFireBase(GoogleSignInUser user)
    {
#if !UNITY_SERVER
        if (user == null)
        {
            _global.FirestoreManager.SingUp(user.UserId);
            return;
        }

        _ = _global.FirestoreManager.GetPlayerData(user.UserId, CallBack);
        
        return;

        void CallBack(FirebasePlayerData result)
        {
            _global.IsSignIn = true;
            if (result == null)//TODO WTF
            {
                _global.FirestoreManager.SingUp(user);
            }
            else
            {
                _global.FirestoreManager.SetPlayerData(result);
                var historyIDs = result.HistoryMatchIDs;
                _global.FirestoreManager.GetAllHistory(historyIDs, list =>
                {
                    _global.FirestoreManager.PlayerData.SetHistoryMatches(list);
                });
            }
        }
#endif
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