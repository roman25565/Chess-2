using System.Collections.Generic;
using Setting;
using UnityEngine;

namespace Board.Piece
{
public class Bishop : AbstractPiece
{
    public Bishop(PieceData pieceData) : base(pieceData) { }

    protected override List<Vector2Int> GetLastPointsInternal(Cell cell)
    {
        var points = new List<Vector2Int>();

        var directions = new List<Vector2Int>
        {
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1)
        };

        foreach (var direction in directions)
        {
            var currentRow = cell.Row + direction.x;
            var currentColumn = cell.Column + direction.y;

            while (currentRow >= 0 && currentRow < 8 && currentColumn >= 0 && currentColumn < 8)
            {
                var targetCell = cell.Board.GetCell(currentRow, currentColumn);

                    points.Add(new Vector2Int(currentRow, currentColumn));
                if (targetCell.Piece != null)
                {
                    break; 
                }

                currentRow += direction.x;
                currentColumn += direction.y;
            }
        }

        return points;
    }
}
}