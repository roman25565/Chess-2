using Board;

public enum GameMode
{
    Online,
    Reconnect,
    Offline,
    Test,
    MigrateHost
}

public class GameData
{
    public AbstractBoard ActiveBoard;
    public GameMode Mode;
    public string RelayJoinCode;

    public void SetActiveBoard(AbstractBoard activeBoard)
    {
        ActiveBoard = activeBoard;
    }
    
    public float TimeControl = 10; 
}