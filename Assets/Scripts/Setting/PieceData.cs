using System;
using System.Collections.Generic;
using Board;
using Board.Piece;
using UnityEngine;
using UnityEngine.Serialization;

namespace Setting
{
    [CreateAssetMenu(fileName = "Piece", menuName = "Settings/Piece")]
    public class PieceData : ScriptableObject
    {
        public PieceType pieceType;
        public List<Sprite> skins;
        public int selectedSkinIndex;
        [System.Serializable]
        public class DirectionList
        {
            public List<Directions> directions = new List<Directions>();
        }

        public List<DirectionList> steps = new List<DirectionList>();

        public int arrangementMin;
        public int arrangementMax;
        
    
        private void OnValidate()
        {
            arrangementMin = Mathf.Min(arrangementMin, arrangementMax);
            arrangementMin = Mathf.Max(0, arrangementMin);
            arrangementMax = Mathf.Max(arrangementMin, arrangementMax);
            arrangementMax = Mathf.Max(0, arrangementMax);
        }
    }
}