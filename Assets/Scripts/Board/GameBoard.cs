using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : AbstractBoard
{
    protected override void OnCanMove(Cell from, Cell to)
    {
        MatchCore.TryMove(new Vector2Int(from.Row, from.Column),
            new Vector2Int(to.Row, to.Column));
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        TryMove(to, false);
    }

    protected override void Move(Cell from, Cell to)
    {
        to.SetPiece(from.Piece);
        from.SetPiece(null);
    }
}