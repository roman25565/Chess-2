using System;
using System.Collections.Generic;
using System.Linq;
using Game.Scripts.Board;
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
    Pawn = 1,
    Rook = 2,
    Knight = 3,
    Bishop = 4,
    Queen = 5,
    King = 6
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

    public PieceColor Color;

    public bool IsFirstMove = true;
    public bool IsRotated;
    public ulong OwnerId;

    protected AbstractPiece(PieceData pieceData)
    {
        PieceType = pieceData.pieceType;
        Skins = pieceData.skins;
        SelectedSkinIndex = 0;//для скінів яких ніколи не буде
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