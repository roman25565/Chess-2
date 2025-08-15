using System;
using System.Collections.Generic;
using Board.Piece;
using UnityEngine;

namespace Setting
{
[CreateAssetMenu(fileName = "Piece", menuName = "Global/Piece")]
public class PieceData : ScriptableObject
{
    public PieceType pieceType;
    public List<Sprite> skins;//0 white 1 Black Piece Sprite

    public int arrangementMin;
    public int arrangementMax;
    public int arrangementCost;


    private void OnValidate()
    {
        arrangementMin = Mathf.Min(arrangementMin, arrangementMax);
        arrangementMin = Mathf.Max(0, arrangementMin);
        arrangementMax = Mathf.Max(arrangementMin, arrangementMax);
        arrangementMax = Mathf.Max(0, arrangementMax);
    }
}
}