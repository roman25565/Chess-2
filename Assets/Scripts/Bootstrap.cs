using System.Linq;
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
    [SerializeField]private ClientMatchmaker clientMatchmaker;
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

    private void LoadSettings()
    {
        var arrangement = Resources.Load<Arrangement>("Settings/Arrangement");
        
        var piecesData = Resources.LoadAll<PieceData>("Settings/Pieces");
        
        var cellStates = Resources.Load<CellStates>("Settings/CellStates");
        
        _settings.Init(arrangement, piecesData, cellStates);
    }

    private void DisableButtons()
    {
        startOnlineMatch.gameObject.SetActive(false);
        startLocalMatch.gameObject.SetActive(false);
        startTestMatch.gameObject.SetActive(false);
        hostLocalMatch.gameObject.SetActive(false);
    }
}