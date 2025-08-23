using System.Collections.Generic;
using Setting;
using UnityEngine;

namespace Board.Piece
{
public class Pawn : AbstractPiece
{
    public Pawn(PieceData pieceData) : base(pieceData)
    {
    }

    protected override List<Vector2Int> GetLastPointsInternal(Cell cell)
    {
        var points = new List<Vector2Int>();
        var directionY = IsRotated ? 1 : -1;
        var abstractBoard = cell.Board;
        
        var moveForward = new Vector2Int(cell.Row, cell.Column + directionY);
        var forwardCell = abstractBoard.GetCell(moveForward.x, moveForward.y);
        if (forwardCell != null && forwardCell.Piece == null)
        {
            points.Add(moveForward);
        }
        
        var diagonalLeftCell = abstractBoard.GetCell(cell.Row - 1, cell.Column + directionY);
        if (diagonalLeftCell != null && diagonalLeftCell.Piece != null)
        {
            var takeLeft = new Vector2Int(cell.Row - 1, cell.Column + directionY);
            points.Add(takeLeft);
        }

        var diagonalRightCell = abstractBoard.GetCell(cell.Row + 1, cell.Column + directionY);
        if (diagonalRightCell != null && diagonalRightCell.Piece != null)
        {
            var takeRight = new Vector2Int(cell.Row + 1, cell.Column + directionY);
            points.Add(takeRight);
        }

        var doubleForwardCell = abstractBoard.GetCell(cell.Row, cell.Column + 1 * directionY);
        if (IsFirstMove && doubleForwardCell != null && doubleForwardCell.Piece == null)
        {
            var moveDoubleForward = new Vector2Int(cell.Row, cell.Column + 2 * directionY);
            points.Add(moveDoubleForward);
        }
        
        return points;
    }
}
}