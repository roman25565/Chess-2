using System.Collections.Generic;
using Setting;
using UnityEngine;

namespace Board.Piece
{
public class King : AbstractPiece
{
    public King(PieceData pieceData) : base(pieceData)
    {
    }

    protected override List<Vector2Int> GetLastPointsInternal(Cell cell)
    {
        var points = new List<Vector2Int>();
        
        
        
        return points;
    }
}
}