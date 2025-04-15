using System;
using System.Collections.Generic;
using System.Linq;
using Setting;
using UnityEngine;

namespace Board.Piece
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
    Kings = 6
}

[Serializable]
public enum PieceColor
{
    White = 0,
    Black = 1
}

public abstract class AbstractPiece
{
    public readonly PieceType PieceType;
    public readonly int SelectedSkinIndex;
    public readonly List<Sprite> Skins;
    public readonly List<PieceData.DirectionList> Steps;

    public PieceColor Color;

    protected bool IsFirstMove = true;
    public bool IsRotated;
    public ulong OwnerId;

    public AbstractPiece(PieceData pieceData)
    {
        PieceType = pieceData.pieceType;
        Skins = pieceData.skins;
        SelectedSkinIndex = pieceData.selectedSkinIndex;
        Steps = pieceData.steps;
    }

    public void Moved()
    {
        if (IsFirstMove) IsFirstMove = false;
    }

    public bool IsValidMove(Cell from, Cell to)
    {
        if (from.Piece == null) return false;

        var result = false;
        var points = GetLastPoints(from);
        foreach (var point in points)
            if (point.x == to.Row && point.y == to.Column)
                result = true;
        return result;
    }

    public List<Vector2Int> GetLastPoints(Cell cell)
    {
        var points = GetLastPointsInternal(cell);
        return ValidationPoints(points);
    }

    protected abstract List<Vector2Int> GetLastPointsInternal(Cell cell);

    private List<Vector2Int> ValidationPoints(List<Vector2Int> points)
    {
        foreach (var point in points.ToList())
        {
            if (point.x < 0 || point.y < 0) points.Remove(point);

            if (point.x > 7 || point.y > 7) points.Remove(point);
        }

        return points;
    }
}
}