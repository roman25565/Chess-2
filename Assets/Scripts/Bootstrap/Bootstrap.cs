using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Google;
using Newtonsoft.Json;
using Setting;
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
    [Inject] private Settings _settings;
    
    [SerializeField] private ClientMatchmaker clientMatchmaker;
    [SerializeField] private SignIn signIn;
    
    [SerializeField] private Button startOnlineMatch;
    [SerializeField] private Button startLocalMatch;
    [SerializeField] private Button startTestMatch;
    [SerializeField] private Button hostLocalMatch;
    [SerializeField] private Button settingsButton;
    
    private async void Start()
    {
        if (_settings.IsSignIn)
        {
            signIn.Init();
            return;
        }
        var isServer = Environment.GetCommandLineArgs().Any(arg => arg == "-port");
        await LoadSettings(!isServer);
        if (isServer)
        {
            Debug.Log("Starting server");
            SceneManager.LoadScene(Server);
        }
        else
        {
            signIn.Init();
            startOnlineMatch.onClick.AddListener(() =>
            {
                Debug.Log("startOnlineMatch");
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
    }

    private async Task LoadSettings(bool isClient)
    {
        var arrangement = LoadArrangement();

        var piecesData = Resources.LoadAll<PieceData>("Settings/Pieces");

        var cellStates = Resources.Load<CellStates>("Settings/CellStates");
        
        var firestore = new FirestoreManager();
        if (isClient) await firestore.Init();

        _settings.Init(arrangement, piecesData, cellStates, firestore);
        Debug.Log("Settings init");
    }

    public void OnSignIn(GoogleSignInUser user)
    {
        SignInFireBase(user);
    }

    public void OnSignInDebug(string id)
    {
        Debug.Log("OnSignIn");
        SignInFireBase(new GoogleSignInUser { UserId = id });
    }

    private void SignInFireBase(GoogleSignInUser user)
    {
        if (user == null)
        {
            _settings.FirestoreManager.SingUp("001");
            return;
        }

        _ = _settings.FirestoreManager.GetPlayerData(user.UserId, CallBack);
        return;

        void CallBack(FirebasePlayerData result)
        {
            _settings.IsSignIn = true;
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


    private List<ArrangementEntry> LoadArrangement()
    {
        var filePath = Application.persistentDataPath + "/game_pieces.json";

        if (File.Exists(filePath))
        {
            var json = File.ReadAllText(filePath);
            var pieceData = JsonConvert.DeserializeObject<List<ArrangementEntry>>(json);
            Debug.Log("pieceData.Count: " + pieceData.Count);
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