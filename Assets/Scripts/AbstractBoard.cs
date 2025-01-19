using System.Collections.Generic;
using System.Linq;
using Setting;
using UnityEngine;
using Zenject;

public abstract class AbstractBoard : MonoBehaviour
{
    [Inject] GameData _gameData;
    private MatchCore _matchCore;
    #region Init
    public Dictionary<int, List<Cell>> _boardLines;

    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= 8 || column < 0 || column >= 8)
        {
            return null;
        }
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
    
    public void RotateBoard()
    {
        transform.rotation = Quaternion.Euler(0, 0, 180);
        foreach (var boardLinesKey in _boardLines.Keys)
        {
            foreach (var cell in _boardLines[boardLinesKey])
            {
                cell.transform.localRotation = Quaternion.Euler(0, 0, 180);
            }
        }
    }
    public void SetMatchCore(MatchCore matchCore)
    {
        _matchCore = matchCore;
    }
    #endregion
    
    [Inject] Settings _settings;
    private Cell _selectedCell;
    private List<Vector2Int> _points;
    
    private bool _isDragging;
    private RectTransform _draggingRectTransform;
    private float _draggingTime;
    
    private Cell _firstSelectedCell;
    private Cell _secondSelectedCell;
    private Cell _thirdSelectedCell;
    
    public bool IsSelectedCell(Cell cell) => (_selectedCell == cell);
    public bool IsMyId(ulong id) => _matchCore.IsMyId(id);

    
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        MovePiece(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }

    public void MovePiece(Cell from, Cell to)
    {
        to.SetPiece(from.Piece);
        from.SetPiece(null);

        _firstSelectedCell?.SetSelectedState(Cell.CellState.None);
        _firstSelectedCell = from;
        _firstSelectedCell.SetSelectedState(Cell.CellState.Selected);
        
        _secondSelectedCell?.SetSelectedState(Cell.CellState.None);
        _secondSelectedCell = to;
        _secondSelectedCell.SetSelectedState(Cell.CellState.Selected);
    }


    public void StartDragging(RectTransform piece)
    {
        _draggingRectTransform = piece;
        _isDragging = true;
        _draggingTime = 0f;

    }

    public void StopDragging()
    {
        if (!_isDragging) return;

        Cell cell = null;
        cell = FindCellOnScreen(_draggingRectTransform.position.x, _draggingRectTransform.position.y);

        _draggingRectTransform.anchoredPosition = Vector2.zero;
        _draggingRectTransform = null;

        if (cell == null) return;

        _isDragging = false;
        Debug.Log(cell.Row + ", " + cell.Column);
        if (cell == _selectedCell && _draggingTime <= 1.5f)
        {
            return;
        }

        OnDraggingStop(cell);
    }

    protected abstract void OnDraggingStop(Cell cell);

    private void Update()
    {
        if (_isDragging)
        {
            _draggingTime += Time.deltaTime;
            _draggingRectTransform.position = Input.mousePosition;
        }
    }

    private Cell FindCellOnScreen(float x, float y)
    {
        var cellSize = Screen.currentResolution.width / 8;
        int cellRow = (int)(x / cellSize);
        
        var freeSpace = (Screen.currentResolution.height - cellSize * 8) / 2;
        var cellColumn = (int)((y - freeSpace) / cellSize);
        cellColumn = 7 - cellColumn;

        if (_matchCore.IsRotated)
        {
            cellColumn = 7 - cellColumn;
            cellRow = 7 - cellRow;
        }
        
        return GetCell(cellRow, cellColumn);
    }

    private void Deselect()
    {
        Debug.Log("Deselect");
        if (_selectedCell != null)
        {
            _selectedCell.SetMovedState(Cell.CellState.None); 
            _selectedCell = null;
        }

        if (_points != null)
        {
            SettCellState(_points, Cell.CellState.None);
            _points = null;
        }
    }

    private void Select(Cell cell)
    {
        Deselect();
        _selectedCell = cell;
        _points = GetLastPoints(cell);
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

    
    private void SettCellState(List<Vector2Int> points, Cell.CellState state)
    {
        foreach (var point in points)
        {
            GetCell(point.x, point.y).SetMovedState(state);
        }
    }

    private List<Vector2Int> GetLastPoints(Cell cell)
    {
        var result = new List<Vector2Int>();
        var steps = cell.Piece.Steps;
        var direction = new Vector2Int(1, 1);
        if (cell.Piece.IsRotated)
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