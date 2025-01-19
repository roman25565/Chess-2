public enum GameMode
{
    Online,
    Offline,
    Test
}

public class GameData
{
    public Board ActiveBoard;
    public GameMode Mode;
    public void SetActiveBoard(Board activeBoard)
    {
        ActiveBoard = activeBoard;
    }

    public ArrangementEntryArrayWithId Player0 = new();
    public ArrangementEntryArrayWithId Player1 = new();
    
}
    public class ArrangementEntryArrayWithId
    {
        public ulong ID;
        public ArrangementEntryArray Arrangement;
    }
