using System.Collections.Generic;

namespace Setting
{
    public class Settings
    {
        private Dictionary<PieceType, PieceData> _pieces;
        public Arrangement ArrangementScriptableObject;
        public CellStates CellStates;
        public List<ArrangementEntry> MyArrangements;

        public void Init(Arrangement arrangement, PieceData[] pieces, CellStates cellStates)
        {
            ArrangementScriptableObject = arrangement;
            MyArrangements = RepackingArrangement(ArrangementScriptableObject);
            CellStates = cellStates;
            
            _pieces = new();
            foreach (var piece in pieces)
            {
                _pieces.Add(piece.pieceType, piece);
            }
        }

        public Piece CreatePiece(PieceType pieceType)
        {
            return new Piece(_pieces[pieceType]);
        }

        private List<ArrangementEntry> RepackingArrangement(Arrangement arrangement)
        {
            List<ArrangementEntry> result = new();
            foreach (var arrangementArrangement in arrangement.arrangements)
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