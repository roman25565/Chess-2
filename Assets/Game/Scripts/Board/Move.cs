using Board.Piece;

namespace Game.Scripts.Board
{
public class Move
{
    public Cell From { get; set; }
    public Cell To { get; set; }
    public AbstractPiece KilledPiece { get; set; }
}
}