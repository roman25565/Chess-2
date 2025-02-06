using System;
using System.Collections.Generic;
using Board;
using Board.Piece;
using UnityEngine;

namespace Setting
{
    public class Settings
    {
        public static readonly string ArrangementFile = Application.persistentDataPath + "/game_pieces.json";
        public Dictionary<PieceType, PieceData> Pieces;
        public CellStates CellStates;
        public List<ArrangementEntry> MyArrangements;
        public FirestoreManager FirestoreManager;

        public void Init(List<ArrangementEntry> arrangement, PieceData[] pieces, CellStates cellStates,FirestoreManager firestoreManager)
        {
            Debug.Log("setting Init");
            MyArrangements = RepackingArrangement(arrangement);
            CellStates = cellStates;
            FirestoreManager = firestoreManager;
            
            Pieces = new();
            foreach (var piece in pieces)
            {
                Pieces.Add(piece.pieceType, piece);
            }
        }

        public AbstractPiece CreatePiece(PieceType pieceType)
        {
            AbstractPiece result = null;
            switch (pieceType)
            {
                case PieceType.Pawns:
                    result = new Pawn(Pieces[pieceType]);
                    break;
                case PieceType.Rooks:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
                    break;
                case PieceType.Knights:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
                    break;
                case PieceType.Bishops:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
                    break;
                case PieceType.Queens:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
                    break;
                case PieceType.Kings:
                    result = new King(Pieces[pieceType]);
                    break;
                default:
                    throw new ArgumentOutOfRangeException(nameof(pieceType), pieceType, null);
            }
            return result;
        }

        private List<ArrangementEntry> RepackingArrangement(List<ArrangementEntry> arrangement)
        {
            List<ArrangementEntry> result = new();
            foreach (var arrangementArrangement in arrangement)
            {
                result.Add(new ArrangementEntry
                {
                    column = arrangementArrangement.column,
                    row = arrangementArrangement.row,
                    pieceType = arrangementArrangement.pieceType
                });
            }
            return result;
        }
    }
}