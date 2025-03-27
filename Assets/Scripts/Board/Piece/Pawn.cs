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
        var moveForward = new Vector2Int(cell.Row, cell.Column + directionY);
        var takeLeft = new Vector2Int(cell.Row - 1, cell.Column + directionY);
        var takeRight = new Vector2Int(cell.Row + 1, cell.Column + directionY);
        points.Add(moveForward);
        points.Add(takeLeft);
        points.Add(takeRight);
        
        if (IsFirstMove && cell.Board.GetCell(cell.Row, cell.Column + 1 * directionY).Piece == null)
        {
            var moveDoubleForward = new Vector2Int(cell.Row, cell.Column + 2 * directionY);
            points.Add(moveDoubleForward);
        }
        
        return points;
    }
}
}