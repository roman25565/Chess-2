using System.Collections.Generic;
using Setting;
using UnityEngine;

namespace Board.Piece
{
public class Knight : AbstractPiece
{
    public Knight(PieceData pieceData) : base(pieceData) { }

    protected override List<Vector2Int> GetLastPointsInternal(Cell cell)
    {
        var points = new List<Vector2Int>();

        var moves = new List<Vector2Int>
        {
            new Vector2Int(2, 1),
            new Vector2Int(2, -1),
            new Vector2Int(-2, 1),
            new Vector2Int(-2, -1),
            new Vector2Int(1, 2),
            new Vector2Int(1, -2),
            new Vector2Int(-1, 2),
            new Vector2Int(-1, -2)
        };

        foreach (var move in moves)
        {
            var targetRow = cell.Row + move.x;
            var targetColumn = cell.Column + move.y;
            points.Add(new Vector2Int(targetRow, targetColumn));
        }

        return points;
    }
    
}
}