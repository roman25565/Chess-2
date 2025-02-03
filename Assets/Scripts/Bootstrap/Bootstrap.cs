using System.Collections.Generic;
using System.IO;
using System.Linq;
using Google;
using Newtonsoft.Json;
using Setting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class Bootstrap : MonoBehaviour
{
    private const string Server = "Server";
    
    [Inject]
    private Settings _settings;
    [Inject]
    private GameData _gameData;
    
    [SerializeField] private Button startOnlineMatch;
    [SerializeField] private Button startLocalMatch;
    [SerializeField] private Button startTestMatch;
    [SerializeField] private Button hostLocalMatch;
    [SerializeField] private Button settingsButton;
    [SerializeField] private ClientMatchmaker clientMatchmaker;
    private void Start()
    {
        LoadSettings();
        bool isServer = System.Environment.GetCommandLineArgs().Any(arg => arg == "-port");
        if (isServer)
        {
            Debug.Log("Starting server");
            SceneManager.LoadScene(Server);
        }
        else
        {
            startOnlineMatch.onClick.AddListener(() =>
            {
                DisableButtons();
                _gameData.Mode = GameMode.Online;
                clientMatchmaker.SearchMatch();
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
                SceneManager.sceneLoaded += (scene, loadSceneMode) => NetworkManager.Singleton.StartHost();
            });
            hostLocalMatch.onClick.AddListener(() =>
            {
                DisableButtons();
                SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
                NetworkManager.Singleton.StartServer();
            });
        }
    }

    public void OnSignIn(GoogleSignInUser user)
    {
        SignInFireBase(user);
    }
    public void OnSignIn(string id)
    {
        Debug.Log("OnSignIn");
        SignInFireBase(new GoogleSignInUser{UserId = id});
    }

    private void SignInFireBase(GoogleSignInUser user)
    {
        if (user == null)
        {
            _settings.FirestoreManager.SingUp("001");
            return;
        }
        _ = _settings.FirestoreManager.GetPlayerData(user.UserId,CallBack);
        return;

        void CallBack(FirebasePlayerData result)
        {
            if (result == null)
            {
                _settings.FirestoreManager.SingUp(user);
            }
            else
            {
                _settings.FirestoreManager.PlayerData = result;
                Debug.Log(result.Elo);
            }
        }
    }

    private void LoadSettings()
    {
        var arrangement = LoadArrangement();
        
        var piecesData = Resources.LoadAll<PieceData>("Settings/Pieces");
        
        var cellStates = Resources.Load<CellStates>("Settings/CellStates");
        
        var firestore = new FirestoreManager();
        firestore.Init();
        
        _settings.Init(arrangement, piecesData, cellStates, firestore);
    }

    private List<ArrangementEntry> LoadArrangement()
    {
        var filePath = Application.persistentDataPath + "/game_pieces.json";
        
        if (File.Exists(filePath))
        {
            string json = File.ReadAllText(filePath);
            var pieceData = JsonConvert.DeserializeObject<List<ArrangementEntry>>(json);
            Debug.Log("pieceData.Count: " + pieceData.Count);
            return pieceData;
        }
        else
        {
            return Resources.Load<Arrangement>("Settings/Arrangement").arrangements;
        }
    }

    private void DisableButtons()
    {
        
        startOnlineMatch.gameObject.SetActive(false);
        startLocalMatch.gameObject.SetActive(false);
        startTestMatch.gameObject.SetActive(false);
        hostLocalMatch.gameObject.SetActive(false);
        settingsButton.gameObject.SetActive(false);
    }
    
    // void InitializePlayGamesLogin()
    // {
    //     var config = new PlayGamesClientConfiguration.Builder()
    //         // Requests an ID token be generated.  
    //         // This OAuth token can be used to
    //         // identify the player to other services such as Firebase.
    //         .RequestIdToken()
    //         .Build();
    //
    //     PlayGamesPlatform.InitializeInstance(config);
    //     PlayGamesPlatform.DebugLogEnabled = true;
    //     PlayGamesPlatform.Activate();
    // }
    //
    // void LoginGoogle()
    // {
    //     Social.localUser.Authenticate(OnGoogleLogin);
    // }
    //
    // void OnGoogleLogin(bool success)
    // {
    //     if (success)
    //     {
    //         // Call Unity Authentication SDK to sign in or link with Google.
    //         Debug.Log("Login with Google done. IdToken: " + ((PlayGamesLocalUser)Social.localUser).GetIdToken());
    //     }
    //     else
    //     {
    //         Debug.Log("Unsuccessful login");
    //     }
    // }
}