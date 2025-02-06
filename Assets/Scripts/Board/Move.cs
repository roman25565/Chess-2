using Board;
using Board.Piece;
using Setting;

public class Move
{
    public Cell From { get; set; }
    public Cell To { get; set; }
    public AbstractPiece AbstractPiece { get; set; }
}