#if !UNITY_SERVER
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameModeSelector : MonoBehaviour
{
    [Inject] private GameData _gameData;
    
    [SerializeField] private ClientMatchmaker clientMatchmaker;
    [SerializeField] private MainMenu mainMenu;
    
    [SerializeField] private Button oneMinutesButton;
    [SerializeField] private Button fiveMinutesButton;
    [SerializeField] private Button tenMinutesButton;

    public GameObject TestGameObject;
    
    public void Init()
    {
        oneMinutesButton.onClick.RemoveAllListeners();
        fiveMinutesButton.onClick.RemoveAllListeners();
        tenMinutesButton.onClick.RemoveAllListeners();
        
        oneMinutesButton.onClick.AddListener(()=>ButtonOnClick(1));
        fiveMinutesButton.onClick.AddListener(()=>ButtonOnClick(5));
        tenMinutesButton.onClick.AddListener(()=>ButtonOnClick(10));
        
        TestGameObject.SetActive(true);
    }

    private void ButtonOnClick(int timeControl)
    {
        SelectGameMode(timeControl);
        FindMatch();
        mainMenu.HideGameModeSelectorPanel();
    }
    
    
    public void SelectGameMode(int timeControl)
    {
        _gameData.TimeControl = timeControl;
    }

    private void FindMatch()
    {
        _gameData.Mode = GameMode.Online;
        clientMatchmaker.SearchMatch(_gameData);
    }
    
    
}
#endif
