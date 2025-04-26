using System;
using System.Collections.Generic;
using Board.Piece;
using Firebase.RealtimeDatabase;
using UnityEngine;

namespace Setting
{
public class Global
{
    public static readonly string ArrangementFile = Application.persistentDataPath + "/game_pieces.json";
    public CellStates CellStates;
    public FirestoreManager FirestoreManager;
    public List<ArrangementEntry> MyArrangements;
    public Dictionary<PieceType, PieceData> Pieces;

    public bool IsSignIn;

    public void Init(List<ArrangementEntry> arrangement, PieceData[] pieces, CellStates cellStates,
        FirestoreManager firestoreManager)
    {
        MyArrangements = RepackingArrangement(arrangement);
        CellStates = cellStates;
        FirestoreManager = firestoreManager;
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
}
}