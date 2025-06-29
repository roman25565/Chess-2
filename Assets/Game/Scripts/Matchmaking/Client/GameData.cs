using Board;

public enum GameMode
{
    Online,
    Reconnect,
    Offline,
    Test
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
    
    public int TimeControl = 10; 
}