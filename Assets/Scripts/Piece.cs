using System;
using System.Collections.Generic;
using UnityEngine;

namespace Setting
{
    public enum Directions
    {
        Left = 0,
        Right = 1,
        Up = 2,
        Down = 3
    }

    [Serializable]
    public enum PieceType
    {
        Empty = 0,
        Pawns = 1,
        Rooks = 2,
        Knights = 3,
        Bishops = 4,
        Queens = 5,
        Kings = 6,
    }
    [Serializable]
    public enum PieceColor
    {
        White = 0,
        Black = 1,
    }
    public class Piece
    {
        public readonly PieceType PieceType;
        public readonly List<Sprite> Skins;
        public readonly int SelectedSkinIndex;
        public readonly List<PieceData.DirectionList> Steps;
        
        public PieceColor Color;
        public ulong OwnerId;
        public bool IsRotated;

        public Piece(PieceData pieceData)
        {
            PieceType = pieceData.pieceType;
            Skins = pieceData.skins;
            SelectedSkinIndex = pieceData.selectedSkinIndex;
            Steps = pieceData.steps;
        }
        
    }
}