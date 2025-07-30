using System.Collections.Generic;
using Board.Piece;
using UnityEngine;

namespace Board
{
public class GameBoard : AbstractBoard
{
    private bool _lastMoveIsFantom;
    public override void ArrangeFigures(MatchData matchData, bool needRotate = true)
    {
        Debug.Log("ArrangeFigures 2:1");
        ArrangeFigures(matchData.Player1, needRotate);
        ArrangeFigures(matchData.Player2, needRotate);
    }
    
    
    protected override void BoardTryMove(Cell from, Cell to)
    {
        if (!MatchCore.CanMove())
        {
            Deselect();
            return;
        }
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
            var lastMove = MoveHistory.GetHistory()[MoveHistory.HistoryIndex - 1];
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
        // Deselect();
    }

    private void DeleteFantomMove()
    {
        
    }
    
    public override void GetPiecesInBoard(ulong connectedPlayerId, ulong remainingPlayerId, out ArrangementEntry[] connectedPlayerPieces, out ArrangementEntry[] remainingPlayerPieces)
    {
        List<ArrangementEntry> connectedPlayerPiecesList = new();
        List<ArrangementEntry> remainingPlayerPiecesList = new();
        ForEachCell(cell =>
        {
            if (cell.Piece == null) return;
            Debug.Log(cell.Piece.OwnerId);
            if (cell.Piece.OwnerId == connectedPlayerId)
                connectedPlayerPiecesList.Add(new ArrangementEntry { row = cell.Row, column = cell.Column, pieceType = cell.Piece.PieceType });
            else if (cell.Piece.OwnerId == remainingPlayerId)
                remainingPlayerPiecesList.Add(new ArrangementEntry { row = cell.Row, column = cell.Column, pieceType = cell.Piece.PieceType });
        });
        connectedPlayerPieces = connectedPlayerPiecesList.ToArray();
        remainingPlayerPieces = remainingPlayerPiecesList.ToArray();
    }

    public override void UpdatePiecesId(ulong oldId, ulong clientId)
    {
        var pieces = GetAllPiecesInCells();
        foreach (var cell in pieces)
        {
            if (cell.Piece.OwnerId == oldId)
            {
                cell.Piece.OwnerId = clientId;
            }
        }
    }

    private List<Cell> GetAllPiecesInCells()
    {
        var result = new List<Cell>();

        ForEachCell(cell =>
        {
            if (cell.Piece != null) result.Add(cell);
        });
    
        return result;
    }
}
}