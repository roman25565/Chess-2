using System;
using System.Collections.Generic;
using System.Linq;
using Board;
using Board.Piece;
using JetBrains.Annotations;
using Setting;
using UnityEngine;
using Zenject;

public abstract class AbstractBoard : MonoBehaviour
{
    #region Init
    [Inject] private GameData _gameData;
    protected Dictionary<int, List<Cell>> BoardLines;

    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= 8 || column < 0 || column >= 8)
        {
            return null;
        }
        return BoardLines[column][row];
    }

    private void Awake()
    {
        Init();
    }

    protected virtual void OnEnable()
    {
        Debug.Log("Board Enabled");
        _gameData.SetActiveBoard(this);
    }

    private void Init(int lineSize = 8)
    {
        var cells = GetComponentsInChildren<Cell>();
        BoardLines = new Dictionary<int, List<Cell>>();
        for (int i = 0; i < lineSize; i++)
        {
            BoardLines.Add(i, new List<Cell>());
            InitLine(i, lineSize, cells);
        }
    }
    
    private void InitLine(int column, int lineSize, Cell[] cells)
    {
        for (int i = 0; i < lineSize; i++)
        {
            BoardLines[column].Add(cells[column * lineSize + i].Init(i, column, this));
        }
    }
    
    public void RotateBoard()
    {
        transform.rotation = Quaternion.Euler(0, 0, 180);
        ForEachCell(cell => cell.transform.localRotation = Quaternion.Euler(0, 0, 180));
        // foreach (var boardLinesKey in BoardLines.Keys)
        // {
        //     foreach (var cell in BoardLines[boardLinesKey])
        //     {
        //         cell.transform.localRotation = Quaternion.Euler(0, 0, 180);
        //     }
        // }
    }

    protected void ForEachCell(Action<Cell> action)
    {
        foreach (var boardLinesKey in BoardLines.Keys)
        {
            foreach (var cell in BoardLines[boardLinesKey])
            {
                action(cell);
            }
        }
    }
    public void SetMatchCore(MatchCore matchCore)
    {
        MatchCore = matchCore;
    }
    #endregion
    
    [Inject] protected Settings Settings;
    protected MatchCore MatchCore;
    
    public bool IsSelectedCell(Cell cell) => (_selectedCell == cell);
    public virtual bool IsMyId(ulong id) => MatchCore.IsMyId(id);
    protected virtual bool IsRotated => MatchCore.IsRotated;

    #region Move
    
    private Cell _selectedCell;
    private bool _isDragging;
    private bool _gameEnded;
    private float _draggingTime;
    private RectTransform _draggingRectTransform;
    
    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        MovePiece(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }

    protected void MovePiece(Cell from, Cell to)
    {
#if !UNITY_SERVER
        AddMoveToHistory(from, to);
        if (_inHistory)HistoryToReal();
        
        SetSelectedState(ref _firstSelectedCell, from);
        SetSelectedState(ref _secondSelectedCell, to);
#endif

        Move(from, to);
    }
    
    public void TryMove(Cell cell, bool isTab = true)
    {
        if (_inHistory) return;
        if (_gameEnded) return;
        if (isTab && _selectedCell == cell) Deselect();
        else if (_selectedCell != null && _selectedCell.Piece.IsValidMove(_selectedCell, cell))
        {
            OnCanMove(_selectedCell, cell);
            Deselect();
        }
        else if (cell.Piece != null)
        {
            if (IsMyId(cell.Piece.OwnerId)) SelectMyPiece(cell);
            else SelectEnemyPiece(cell);
        }

        return;

        void SelectMyPiece(Cell cell1)
        {
            SelectCell(cell1);
            
            _thirdSelectedCell?.SetSelectedState(Cell.CellState.None);
            _thirdSelectedCell = cell1;
            _thirdSelectedCell.SetSelectedState(Cell.CellState.Selected);
        }

        void SelectEnemyPiece(Cell cell1)
        {

        }
    }
    protected abstract void OnCanMove(Cell from, Cell to);


    public void StartDragging(RectTransform piece)
    {
        _draggingRectTransform = piece;
        _isDragging = true;
        _draggingTime = 0f;

    }

    public void StopDragging()
    {
        if (!_isDragging) return;
        
        _isDragging = false;
        var cell = FindCellOnScreen(_draggingRectTransform.position.x, _draggingRectTransform.position.y);
        
        _draggingRectTransform.anchoredPosition = Vector2.zero;
        _draggingRectTransform = null;
        
        if (_inHistory) return;
        if (_gameEnded) return;
        
        if (cell == null)
        {
            MoveToOutScreen(_selectedCell);
            return;
        }
        if (cell == _selectedCell && _draggingTime <= 1.5f) return;

        Debug.Log(cell.Row + ", " + cell.Column);
        OnDraggingStop(_selectedCell, cell);
    }

    protected virtual void MoveToOutScreen(Cell from){}
    protected abstract void OnDraggingStop(Cell from,Cell to);

    protected abstract void Move(Cell from, Cell to);
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

        if (IsRotated)
        {
            cellColumn = 7 - cellColumn;
            cellRow = 7 - cellRow;
        }
        
        return GetCell(cellRow, cellColumn);
    }
    
    #endregion

    #region CellState
    private List<Vector2Int> _points;
    private Cell _firstSelectedCell;
    private Cell _secondSelectedCell;
    private Cell _thirdSelectedCell;
    
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

    private void SelectCell(Cell cell)
    {
        Deselect();
        _selectedCell = cell;
        var piece = _selectedCell.Piece;
       
        if (piece == null) return;

        _points = GetPoints(cell, piece);
        SettCellState(_points, Cell.CellState.Moved);
    }

    protected virtual List<Vector2Int> GetPoints(Cell cell, AbstractPiece piece)
    {
        return piece.GetLastPoints(cell);
    }
    
    public bool IsValidMove(Vector2Int from, Vector2Int to)
    {
        var fromCell = GetCell(from.x, from.y);
        return fromCell.Piece.IsValidMove(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }
    
    private void SettCellState(List<Vector2Int> points, Cell.CellState state)
    {
        foreach (var point in points)
        {
            GetCell(point.x, point.y).SetMovedState(state);
        }
    }

    private void SetSelectedState([CanBeNull] ref Cell cell, Cell newCell)
    {
        cell?.SetSelectedState(Cell.CellState.None);
        cell = newCell;
        cell.SetSelectedState(Cell.CellState.Selected);
    }
    #endregion
    
    private bool _inHistory;
    public void EndGame()
    {
        _gameEnded = true;
    }
#if !UNITY_SERVER
    #region MoveHistory

    private List<Move> _moveHistory = new List<Move>();
    private int _historyIndex;

    public void NextMove()
    {
        if (!_inHistory) return;
        SetHistoryIndex(_historyIndex + 1);
    }

    public void UndoMove()
    {
        SetHistoryIndex(_historyIndex - 1);
    }
    
    private void AddMoveToHistory(Cell from, Cell to)
    {
        HistoryToReal();
        
        _moveHistory.Add(new Move{From = from, To = to, AbstractPiece = to.Piece});
        _historyIndex = _moveHistory.Count;
    }

    private void HistoryToReal()
    {
        if (_inHistory) SetHistoryIndex(_moveHistory.Count);
    }

    private void SetHistoryIndex(int index)
    {
        if (index < 0) return;
        if (index > _moveHistory.Count) return;
        if (index == _moveHistory.Count)
        {
            if (_inHistory) _inHistory = false;
        }
        else if (index < _moveHistory.Count)
        {
            _inHistory = true;
        }

        var movesCount = index - _historyIndex;
        HistoryMove(movesCount);
    }

    private void HistoryMove(int recursionCount)
    {
        if (recursionCount < 0)//DOWN
        {
            var move = _moveHistory[_historyIndex - 1]; 
            Move(move.To, move.From);
            move.To.SetPiece(move.AbstractPiece);
            
            _historyIndex--;
            recursionCount++;
            HistoryMove(recursionCount);
        }
        else if (recursionCount > 0)//UP
        {
            var move = _moveHistory[_historyIndex]; 
            Move(move.From, move.To);
            
            _historyIndex++;
            recursionCount--;
            HistoryMove(recursionCount);
        }
        else if (recursionCount == 0)//STOP
        {

        }
    }


    #endregion
    #endif
}