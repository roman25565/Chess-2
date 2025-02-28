using UnityEngine;

namespace Board
{
public class GameBoard : AbstractBoard
{
    public override void StartGame(MatchBootstrap.PlayerBootstrapData player1, MatchBootstrap.PlayerBootstrapData player2)
    {
        ArrangeFigures(player1);
        ArrangeFigures(player2);
    }
    
    
    protected override void OnCanMove(Cell from, Cell to)
    {
        MatchCore.TryMove(new Vector2Int(from.Row, from.Column),
            new Vector2Int(to.Row, to.Column));
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        TryMove(to, false);
    }
}
}