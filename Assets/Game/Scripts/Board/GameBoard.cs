using System.Collections.Generic;
using Board;
using Board.Piece;
using UnityEngine;
using UnityEngine.XR;

namespace Game.Scripts.Board
{
public class GameBoard : AbstractBoard
{
    private bool _lastMoveIsFantom;
    public override void ArrangeFigures(MatchData matchData, bool needRotate = true)
    {
        Debug.Log("ArrangeFigures " + matchData.Player2.StartArrangement.Length);
        ArrangeFigures(matchData.Player1, needRotate);
        ArrangeFigures(matchData.Player2, needRotate);
        
    }

    public override void BoardTryMove(Cell from, Cell to, bool isTab = true)
    {
        Debug.Log("BoardTryMove isTab" + isTab);
        if (!MatchCore.CanMove(from, to))
        {
            Deselect();
            return;
        }
        if (MatchCore.IsLocal && to.Piece != null && to.Piece.PieceType == PieceType.King)
        {
            MatchCore.HandleEndGameLogicLocal((int)to.Piece.OwnerId);
        }
        MovePiece(from, to, isTab);;
        _lastMoveIsFantom = true;
        MatchCore.TryMove(new Vector2Int(from.Row, from.Column),
            new Vector2Int(to.Row, to.Column));
    }

    protected override bool IsFantom()
    {
        return _lastMoveIsFantom;
    }

    public override bool IsConfirmation(Cell from, Cell to)
    {
        Debug.Log("IsConfirmation " + _lastMoveIsFantom);
        if (_lastMoveIsFantom)
        {
            _lastMoveIsFantom = false;
            var lastMove = MoveHistory.GetHistory()[MoveHistory.HistoryIndex - 1];
            if (lastMove.From == from && lastMove.To == to)
            {
                Debug.Log("IsConfirmation return true");
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