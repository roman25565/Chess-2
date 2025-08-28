using System.Collections.Generic;
using Board;
using Board.Piece;
using Setting;
using UnityEngine;

namespace Game.Scripts.Board.Piece
{
public class Queen : AbstractPiece
{
    public Queen(PieceData pieceData) : base(pieceData)
    {
    }

    protected override List<Vector2Int> GetLastPointsInternal(Cell cell)
    {
        var points = new List<Vector2Int>();

        var directions = new List<Vector2Int>
        {
            new Vector2Int(1, 0),
            new Vector2Int(-1, 0),
            new Vector2Int(0, 1),
            new Vector2Int(0, -1),
            new Vector2Int(1, 1),
            new Vector2Int(1, -1),
            new Vector2Int(-1, 1),
            new Vector2Int(-1, -1) 
        };

        foreach (var direction in directions)
        {
            for (int step = 1; step < 8; step++)
            {
                var currentRow = cell.Row + direction.x * step;
                var currentColumn = cell.Column + direction.y * step;

                if (currentRow < 0 || currentRow >= 8 || currentColumn < 0 || currentColumn >= 8)
                    break;

                var targetCell = cell.Board.GetCell(currentRow, currentColumn);

                points.Add(new Vector2Int(currentRow, currentColumn));

                if (targetCell != null && targetCell.Piece != null)
                    break;
            }
        }

        return points;
    }
}
}