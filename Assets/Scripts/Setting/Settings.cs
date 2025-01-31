using System.Collections.Generic;
using UnityEngine;

namespace Setting
{
    public class Settings
    {
        public static readonly string ArrangementFile = Application.persistentDataPath + "/game_pieces.json";
        public Dictionary<PieceType, PieceData> Pieces;
        public List<ArrangementEntry> ArrangementScriptableObject;
        public CellStates CellStates;
        public List<ArrangementEntry> MyArrangements;
        public FirestoreManager FirestoreManager;

        public void Init(List<ArrangementEntry> arrangement, PieceData[] pieces, CellStates cellStates,FirestoreManager firestoreManager)
        {
            ArrangementScriptableObject = arrangement;
            MyArrangements = RepackingArrangement(ArrangementScriptableObject);
            CellStates = cellStates;
            FirestoreManager = firestoreManager;
            
            Pieces = new();
            foreach (var piece in pieces)
            {
                Pieces.Add(piece.pieceType, piece);
            }
        }

        public Piece CreatePiece(PieceType pieceType)
        {
            return new Piece(Pieces[pieceType]);
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