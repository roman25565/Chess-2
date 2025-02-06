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
        Kings = 6,
    }
    [Serializable]
    public enum PieceColor
    {
        White = 0,
        Black = 1,
    }

    public abstract class AbstractPiece
    {
        public readonly PieceType PieceType;
        public readonly List<Sprite> Skins;
        public readonly int SelectedSkinIndex;
        public readonly List<PieceData.DirectionList> Steps;

        public PieceColor Color;
        public ulong OwnerId;
        public bool IsRotated;

        public AbstractPiece(PieceData pieceData)
        {
            PieceType = pieceData.pieceType;
            Skins = pieceData.skins;
            SelectedSkinIndex = pieceData.selectedSkinIndex;
            Steps = pieceData.steps;
        }

        public bool IsValidMove(Cell from, Cell to)
        {
            if (from.Piece == null) return false;

            var result = false;
            var points = GetLastPoints(from);
            foreach (var point in points)
            {
                if (point.x == to.Row && point.y == to.Column)
                {
                    result = true;
                }
            }

            Debug.Log("IsValidMove: " + result);
            return result;
        }

        public virtual List<Vector2Int> GetLastPoints(Cell cell)
        {
            var points = new List<Vector2Int>();
            var steps = Steps;
            var direction = new Vector2Int(1, 1);
            if (IsRotated)
            {
                direction.y = -1;
            }

            foreach (var step in steps)
            {
                var point = new Vector2Int(cell.Row, cell.Column);
                foreach (var stepDirection in step.directions)
                {
                    switch (stepDirection)
                    {
                        case Directions.Down:
                            point.y += 1 * direction.y;
                            break;
                        case Directions.Up:
                            point.y -= 1 * direction.y;
                            break;
                        case Directions.Left:
                            point.x -= 1 * direction.x;
                            break;
                        case Directions.Right:
                            point.x += 1 * direction.x;
                            break;
                    }
                }

                points.Add(point);
            }

            if (points.Count == 0)
            {
                Debug.LogError("No points found: Board GetLastPoints()");
            }

            return ValidationPoints(points);

        }

        private List<Vector2Int> ValidationPoints(List<Vector2Int> points)
        {
            foreach (var point in points.ToList())
            {
                if (point.x < 0 || point.y < 0)
                {
                    points.Remove(point);
                }

                if (point.x > 7 || point.y > 7)
                {
                    points.Remove(point);
                }
            }

            return points;
        }
    }
}