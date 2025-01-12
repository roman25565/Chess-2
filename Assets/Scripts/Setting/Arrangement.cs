using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Serialization;

namespace Setting
{
    [CreateAssetMenu(fileName = "Arrangement", menuName = "Settings/Arrangement")]
    public class Arrangement : ScriptableObject
    {
        public List<ArrangementEntry> arrangements;

        public PieceType GetPieceType(int row, int column)
        {
            foreach (var entry in arrangements)
            {
                if (entry.column == row && entry.row == column)
                {
                    return entry.pieceType;
                }
            }
            return PieceType.Empty;
        }
        
    }
}