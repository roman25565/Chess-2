using System;
using System.Collections.Generic;
using System.Linq;
using Setting;
using UnityEngine;
using UnityEngine.Serialization;
using Zenject;

public class Board : MonoBehaviour
{
    [Inject] GameData _gameData;
    #region Init
    public Dictionary<int, List<Cell>> _boardLines;

    public Cell GetCell(int row, int column)
    {
        return _boardLines[column][row];
    }

    private void Awake()
    {
        Init();
        _gameData.SetActiveBoard(this);
    }

    private void Init(int lineSize = 8)
    {
        var cells = GetComponentsInChildren<Cell>();
        _boardLines = new Dictionary<int, List<Cell>>();
        for (int i = 0; i < lineSize; i++)
        {
            _boardLines.Add(i, new List<Cell>());
            InitLine(i, lineSize, cells);
        }
    }


    private void InitLine(int column, int lineSize, Cell[] cells)
    {
        for (int i = 0; i < lineSize; i++)
        {
            _boardLines[column].Add(cells[column * lineSize + i].Init(i, column, this));
        }
    }
    #endregion

    [Inject] Settings _settings;
    private Cell _selectedCell;
    private MatchCore _matchCore;

    private List<Vector2Int> _points;
    
    public void SetMatchCore(MatchCore matchCore)
    {
        foreach (var boardLinesKey in _boardLines.Keys)
        {
            foreach (var cel in _boardLines[boardLinesKey])
            {
                if (cel.Piece == null) continue;
                Debug.Log($"cellId: {cel.Piece.OwnerId}");
            }
        }
        _matchCore = matchCore;
    }
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        MovePiece(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }

    private void MovePiece(Cell from, Cell to)
    {
        to.SetPiece(from.Piece);
        from.SetPiece(null);
    }
    
    public void OnClick(Cell cell)
    {
        foreach (var boardLinesKey in _boardLines.Keys)
        {
            foreach (var cel in _boardLines[boardLinesKey])
            {
                if (cel.Piece == null) continue;
                Debug.Log($"cellId: {cel.Piece.OwnerId}");
            }
        }
        
        if (cell.Piece != null && cell.Piece.OwnerId != _matchCore.OwnerClientId)
        {
            Debug.Log("cell.Piece.ownerId != _matchCore.OwnerClientId");
            Debug.Log($"OwnerId: {_matchCore.OwnerClientId}, PieceID: {cell.Piece.OwnerId}");
            Deselect();
            return;
        }
        if (_selectedCell == cell)
        {
            Deselect();
            return;
        }
        if (_selectedCell == null)
        {
            _selectedCell = cell;
            Select();
            return;
        }
        
        if (IsValidMove(_selectedCell, cell))
        {
            _matchCore.TryMove(new Vector2Int(_selectedCell.Row,_selectedCell.Column), new Vector2Int(cell.Row,cell.Column));
            Deselect();
        }

    }

    private void Deselect()
    {
        Debug.Log("Deselect");
        if (_selectedCell != null)
        {
            _selectedCell.SetState(Cell.CellState.None); 
            _selectedCell = null;
        }

        if (_points != null)
        {
            SettCellState(_points, Cell.CellState.None);
            _points = null;
        }
    }

    private void Select()
    {
        Debug.Log("Select");
        _points = GetLastPoints(_selectedCell);
        _points = ValidationPoints(_points);
        SettCellState(_points, Cell.CellState.Moved);
    }


    public bool IsValidMove(Vector2Int from, Vector2Int to)
    {
        return IsValidMove(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }

    private bool IsValidMove(Cell from, Cell to)
    {
        if (from.Piece == null) return false;
        
        var result = false;
        var points = GetLastPoints(from);
        points = ValidationPoints(points);

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

    public void RotateBoard()
    {
        Debug.Log("Rotating Board");
        transform.rotation = Quaternion.Euler(0, 0, 180);
        foreach (var boardLinesKey in _boardLines.Keys)
        {
            foreach (var cell in _boardLines[boardLinesKey])
            {
                cell.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }
    }
    
    private void SettCellState(List<Vector2Int> points, Cell.CellState state)
    {
        foreach (var point in points)
        {
            GetCell(point.x, point.y).SetState(state);
        }
    }

    private List<Vector2Int> GetLastPoints(Cell cell)
    {
        var result = new List<Vector2Int>();
        var steps = cell.Piece.Steps;
        foreach (var step in steps)
        {
            var point = new Vector2Int(cell.Row, cell.Column);
            foreach (var stepDirection in step.directions)
            {
                switch (stepDirection)
                {
                    case Directions.Down:
                        point.y += 1;
                        break;
                    case Directions.Up:
                        point.y -= 1;
                        break;
                    case Directions.Left:
                        point.x -= 1;
                        break;
                    case Directions.Right:
                        point.x += 1;
                        break;
                }
            }
            result.Add(point);
        }

        if (result.Count == 0)
        {
            Debug.LogError("No points found: Board GetLastPoints()");
        }
        return result;
        
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