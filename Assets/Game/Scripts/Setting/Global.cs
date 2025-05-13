using System;
using System.Collections.Generic;
using Board.Piece;
#if !UNITY_SERVER
using Firebase.RealtimeDatabase;
#endif
using UnityEngine;

namespace Setting
{
public enum EndGameType
{
    Null,
    Won,
    Lose,
    Draw,
    Canceled
}
public class Global : IDisposable
{
    public static readonly string ArrangementFile = Application.persistentDataPath + "/game_pieces.json";
    public CellStates CellStates;
#if !UNITY_SERVER
    public FirestoreManager FirestoreManager;
#endif
    public List<ArrangementEntry> MyArrangements;
    public Dictionary<PieceType, PieceData> Pieces;

    public bool IsSignIn;
    public EndGameType EndGameType;
    public void Init(List<ArrangementEntry> arrangement, PieceData[] pieces, CellStates cellStates
#if !UNITY_SERVER 
, FirestoreManager firestoreManager
#endif
)
    {
        MyArrangements = RepackingArrangement(arrangement);
        CellStates = cellStates;
#if !UNITY_SERVER
        FirestoreManager = firestoreManager;
#endif
        Pieces = new Dictionary<PieceType, PieceData>();
        foreach (var piece in pieces)
        {
            Debug.Log(piece);
            Pieces.Add(piece.pieceType, piece);
        };
    }
    
    public AbstractPiece CreatePiece(PieceType pieceType)
    {
        AbstractPiece result = pieceType switch
        {
            PieceType.Pawns => new Pawn(Pieces[pieceType]),
            PieceType.Rooks => new Rook(Pieces[pieceType]),
            PieceType.Knights => new Knight(Pieces[pieceType]),
            PieceType.Bishops => new Bishop(Pieces[pieceType]),
            PieceType.Queens => new Queen(Pieces[pieceType]),
            PieceType.Kings => new King(Pieces[pieceType]),
            _ => throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null)
        };
        return result;
    }

    private List<ArrangementEntry> RepackingArrangement(List<ArrangementEntry> arrangement)
    {
        List<ArrangementEntry> result = new();
        foreach (var arrangementArrangement in arrangement)
            result.Add(new ArrangementEntry
            {
                column = arrangementArrangement.column,
                row = arrangementArrangement.row,
                pieceType = arrangementArrangement.pieceType
            });
        return result;
    }

    public void Dispose()
    {
        GlobalTools.Dispose();
    }
}
}