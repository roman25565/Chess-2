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

    public void SetActiveBoard(AbstractBoard activeBoard)
    {
        ActiveBoard = activeBoard;
    }

    public string IP;
    public ushort Port;
    
    public void SetConnectionData(string ip, ushort port)
    {
        IP = ip;
        Port = port;
    }
    
    public int TimeControl = 10; 
}