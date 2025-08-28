using Board;
using Game.Scripts.Board;

public enum GameMode
{
    Online,
    Reconnect,
    Offline,
    SinglePlayVsBot,
    MigrateHost
}

public enum BotDifficulty
{
    Easy = 1,
    Medium = 2,
    Hard = 3,
    Expert = 4, 
}

public class GameData
{
    public AdvancedMatchmaking Matchmaking;
    public AbstractBoard ActiveBoard;
    public GameMode Mode;
    public string RelayJoinCode;

    public void SetActiveBoard(AbstractBoard activeBoard)
    {
        ActiveBoard = activeBoard;
    }
    
    public float TimeControl = 10 * 60;
    public BotDifficulty BotDifficulty;
}


