#if !UNITY_SERVER
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class GameModeSelector : MonoBehaviour
{
    [Inject] private GameData _gameData;
    
    [SerializeField] private MainMenu mainMenu;
    
    [SerializeField] private Button oneMinutesButton;
    [SerializeField] private Button fiveMinutesButton;
    [SerializeField] private Button tenMinutesButton;

    
    public void Init()
    {
        oneMinutesButton.onClick.RemoveAllListeners();
        fiveMinutesButton.onClick.RemoveAllListeners();
        tenMinutesButton.onClick.RemoveAllListeners();
        
        oneMinutesButton.onClick.AddListener(()=>ButtonOnClick(1 * 60f));
        fiveMinutesButton.onClick.AddListener(()=>ButtonOnClick(5 * 60f));
        tenMinutesButton.onClick.AddListener(()=>ButtonOnClick(10 * 60f));
    }

    private void ButtonOnClick(float timeControl)
    {
        SelectGameMode(timeControl);
        FindMatch();
        mainMenu.HideGameModeSelectorPanel();
    }
    
    
    public void SelectGameMode(float timeControl)
    {
        _gameData.TimeControl = timeControl;
    }

    private void FindMatch()
    {
        _gameData.Mode = GameMode.Online;
        mainMenu.EnableFindMatchPanel();
        _gameData.Matchmaking.SearchMatch(_gameData);
    }
    
    
}
#endif
