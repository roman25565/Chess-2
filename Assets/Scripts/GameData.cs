public enum GameMode
{
    Online,
    Offline,
    Test
}

public class GameData
{
    public AbstractBoard ActiveBoard;
    public GameMode Mode;
    public void SetActiveBoard(AbstractBoard activeBoard)
    {
        ActiveBoard = activeBoard;
    }

    
}
