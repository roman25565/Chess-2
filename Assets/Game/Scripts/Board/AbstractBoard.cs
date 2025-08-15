using System;
using System.Collections.Generic;
using Board.Piece;
using JetBrains.Annotations;
using Setting;
using UnityEngine;
using Zenject;

namespace Board
{
public abstract class AbstractBoard : MonoBehaviour
{
    [Inject] protected Global Global;
    protected MatchCore MatchCore;

    protected MoveHistory MoveHistory;

    protected Dictionary<int, List<Cell>> BoardLines;
    protected virtual bool IsRotated => MatchCore.IsRotated;
    private bool _gameEnded;

    public virtual void ArrangeFigures(MatchData matchData, bool needRotate = true)
    {
        throw new NotImplementedException();
    }

    public virtual void ArrangeFigures(HistoryMatchData historyMatchData)
    {
        throw new NotImplementedException();
    }

    protected void ArrangeFigures(PlayerData playerBootstrapData, bool needRotate = true)
    {
        foreach (var arrangementArrangement in playerBootstrapData.StartArrangement)
        {
            var row = arrangementArrangement.column;
            var column = arrangementArrangement.row;
            var type = arrangementArrangement.pieceType;
            var piece = Global.CreatePiece(type);
            piece.OwnerId = playerBootstrapData.PlayerId;
            piece.IsRotated = playerBootstrapData.IsRotate;
            piece.Color = playerBootstrapData.IsWhite ? PieceColor.White : PieceColor.Black;
            if (needRotate && playerBootstrapData.IsRotate)
            {
                column = 7 - column;
                row = 7 - row;
            }

            GetCell(column, row).SetPiece(piece);
        }
    }

    public bool IsSelectedCell(Cell cell)
    {
        return _selectedCell == cell;
    }

    public virtual bool IsMyId(ulong id)
    {
        return MatchCore.IsMyId(id);
    }

    public void EndGame()
    {
        _gameEnded = true;
    }

    #region InitBoard

    [Inject] private GameData _gameData;

    public Cell GetCell(int row, int column)
    {
        if (row < 0 || row >= 8 || column < 0 || column >= 8) return null;

        return BoardLines[column][row];
    }

    private void Awake()
    {
        Init();
        MoveHistory = new MoveHistory(Move);
    }

    protected virtual void OnEnable()
    {
        _gameData.SetActiveBoard(this);
    }

    private void Init(int lineSize = 8)
    {
        var cells = GetComponentsInChildren<Cell>();
        BoardLines = new Dictionary<int, List<Cell>>();
        for (var i = 0; i < lineSize; i++)
        {
            BoardLines.Add(i, new List<Cell>());
            InitLine(i, lineSize, cells);
        }
    }

    private void InitLine(int column, int lineSize, Cell[] cells)
    {
        for (var i = 0; i < lineSize; i++) BoardLines[column].Add(cells[column * lineSize + i].Init(i, column, this));
    }

    public void RotateBoard()
    {
        transform.rotation = Quaternion.Euler(0, 0, 180);
        ForEachCell(cell => cell.transform.localRotation = Quaternion.Euler(0, 0, 180));
    }

    protected void ForEachCell(Action<Cell> action)
    {
        foreach (var boardLinesKey in BoardLines.Keys)
        foreach (var cell in BoardLines[boardLinesKey])
            action(cell);
    }

    public void SetMatchCore(MatchCore matchCore)
    {
        MatchCore = matchCore;
    }

    #endregion

    #region Move

    private Cell _selectedCell;
    private bool _isDragging;
    private float _draggingTime;
    private RectTransform _draggingRectTransform;

    public void MovePiece(Vector2Int from, Vector2Int to)
    {
        MovePiece(GetCell(from.x, from.y), GetCell(to.x, to.y));
    }

    protected void MovePiece(Cell from, Cell to)
    {
        MovePiece(from, to, MoveHistory);
    }

    protected void MovePiece(Cell from, Cell to, MoveHistory moveHistory, bool isInternalHistoryMove = false)
    {
        if (IsConfirmation(from, to))
            return;
        
        var killedPiece = to.Piece != null && to.Piece.PieceType == PieceType.Kings ? to.Piece : null;
        
        moveHistory.AddMove(from, to, isInternalHistoryMove);
        SetSelectedState(ref _firstSelectedCell, from);
        SetSelectedState(ref _secondSelectedCell, to);
        if (from.Piece != null && from.Piece.IsFirstMove) from.Piece.IsFirstMove = false;
        
        Move(from, to);
    }
    protected virtual bool IsFantom()
    {
        return false;
    }

    protected virtual bool IsConfirmation(Cell from, Cell to)
    {
        return false;
    }


    public void TryMove(Cell cell, bool isTab = true)
    {
        if (!(!MoveHistory.InHistory || !_gameEnded)) return;
        if (isTab && _selectedCell == cell)
        {
            Deselect();
        }
        else if (_selectedCell != null && IsValidMove(_selectedCell, cell))
        {
            BoardTryMove(_selectedCell, cell);
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

    protected abstract void BoardTryMove(Cell from, Cell to);

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

        if (cell == null)
        {
            MoveToOutScreen(_selectedCell);
            return;
        }

        if (cell == _selectedCell && _draggingTime <= 1.5f) return;
        OnDraggingStop(_selectedCell, cell);
    }

    protected virtual void MoveToOutScreen(Cell from)
    {
    }

    protected abstract void OnDraggingStop(Cell from, Cell to);

    protected virtual void Move(Cell from, Cell to)
    {
        Debug.Log("MOVE");
        to.SetPiece(from.Piece);
        from.SetPiece(null);
    }

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
        var cellRow = (int)(x / cellSize);

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

    protected void Deselect()
    {
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
        var toCell = GetCell(to.x, to.y);
        return IsValidMove(fromCell, toCell);
    }
    protected virtual bool IsValidMove(Cell from, Cell to)
    {
        Debug.Log("vir IsValidMove from " + from + "to " + to + "from.Piece " + from.Piece);
        return from.Piece.IsValidMove(from, to);
    }

    private void SettCellState(List<Vector2Int> points, Cell.CellState state)
    {
        foreach (var point in points) GetCell(point.x, point.y).SetMovedState(state);
    }

    private void SetSelectedState([CanBeNull] ref Cell cell, Cell newCell)
    {
        cell?.SetSelectedState(Cell.CellState.None);
        cell = newCell;
        cell.SetSelectedState(Cell.CellState.Selected);
    }

    #endregion

#if !UNITY_SERVER

    public virtual void NextMove()
    {
        if (!MoveHistory.InHistory) return;
        MoveHistory.SetHistoryIndex(MoveHistory.HistoryIndex + 1);
    }

    public virtual void UndoMove()
    {
        MoveHistory.SetHistoryIndex(MoveHistory.HistoryIndex - 1);
    }

    public List<Move> GetHistory()
    {
        return MoveHistory.GetHistory();
    }

    protected void ClearBoard()
    {
        ForEachCell(cell =>
        {
            cell.SetPiece(null);
            cell.SetMovedState(Cell.CellState.None);
            cell.SetSelectedState(Cell.CellState.None);
        });
    }
#endif
    public virtual void GetPiecesInBoard(ulong connectedPlayerId, ulong remainingPlayerId, out ArrangementEntry[] connectedPlayerPieces, out ArrangementEntry[] remainingPlayerPieces)
    {
        throw new NotImplementedException();
    }

    public virtual void UpdatePiecesId(ulong oldId, ulong clientId)
    {
        throw new NotImplementedException();
    }
}
}