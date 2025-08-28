using UI;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class BotDifficultySelector : MonoBehaviour
{
    [Inject] private GameData _gameData;
    
    [SerializeField] private MainMenu mainMenu;
    
    [SerializeField] private Button easyButton;
    [SerializeField] private Button mediumButton;
    [SerializeField] private Button hardButton;
    [SerializeField] private Button expertButton;

    
    public void Init()
    {
        easyButton.onClick.RemoveAllListeners();
        mediumButton.onClick.RemoveAllListeners();
        hardButton.onClick.RemoveAllListeners();
        expertButton.onClick.RemoveAllListeners();
        
        easyButton.onClick.AddListener(()=>ButtonOnClick(BotDifficulty.Easy));
        mediumButton.onClick.AddListener(()=>ButtonOnClick(BotDifficulty.Medium));
        hardButton.onClick.AddListener(()=>ButtonOnClick(BotDifficulty.Hard));
        expertButton.onClick.AddListener(()=>ButtonOnClick(BotDifficulty.Expert));
    }

    private void ButtonOnClick(BotDifficulty difficulty)
    {
        _gameData.BotDifficulty = difficulty;
        _gameData.Mode = GameMode.SinglePlayVsBot;
        mainMenu.HideBotDifficultySelectorPanel();
        SceneManager.LoadScene("GameScene", LoadSceneMode.Single);
    }
}
