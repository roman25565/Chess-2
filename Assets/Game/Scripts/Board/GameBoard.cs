using UnityEngine;

namespace Board
{
public class GameBoard : AbstractBoard
{
    private bool _lastMoveIsFantom;
    public override void StartGame(MatchBootstrap.PlayerBootstrapData player1, MatchBootstrap.PlayerBootstrapData player2)
    {
        ArrangeFigures(player1);
        ArrangeFigures(player2);
    }
    
    
    protected override void OnCanMove(Cell from, Cell to)
    {
        MovePiece(from, to);
        _lastMoveIsFantom  = true;
        MatchCore.TryMove(new Vector2Int(from.Row, from.Column),
            new Vector2Int(to.Row, to.Column));
    }

    protected override bool IsConfirmation(Cell from, Cell to)
    {
        if (_lastMoveIsFantom)
        {
            _lastMoveIsFantom = false;
            var lastMove = MoveHistory.GetHistory()[MoveHistory.HistoryIndex];
            if (lastMove.From == from && lastMove.To == to)
            {
                return true;
            }
            else
            {
                //TODO 
            }
            
        }

        return false;
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        TryMove(to, false);
    }
}
}