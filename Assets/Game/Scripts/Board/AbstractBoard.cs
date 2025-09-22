using System;
using System.Collections;
using System.Collections.Generic;
using Board;
using Board.Piece;
using Game.Scripts.Board;
using JetBrains.Annotations;
using Setting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.Board
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

    [CanBeNull]
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

    public void ForEachCell(Action<Cell> action)
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

    protected void MovePiece(Cell from, Cell to, bool isTab = true)
    {
        MovePiece(from, to, MoveHistory, false,isTab);
    }

    protected void MovePiece(Cell from, Cell to, MoveHistory moveHistory, bool isInternalHistoryMove = false, bool isTab = true)
    {
        if (IsConfirmation(from, to))
            return;
        
        moveHistory.AddMove(from, to, isInternalHistoryMove);
        SetSelectedState(ref _firstSelectedCell, from);
        SetSelectedState(ref _secondSelectedCell, to);
        if (from.Piece != null && from.Piece.IsFirstMove) from.Piece.IsFirstMove = false;
        Move(from, to);
        if ((to.Column == 0 || to.Column == 7) && to.Piece != null && to.Piece.PieceType == PieceType.Pawn && this.GetType() != typeof(ArrangementBoard)) //Queen Update
        {
            var queen = Global.CreatePiece(PieceType.Queen);
            GetCell(to.Row, to.Column).SetPiece(queen);
        }
        Global.Sound.OnMove();
        if (isTab)
        {
            AnimateMove(from, to);
        }
    }

    protected virtual bool IsFantom()
    {
        return false;
    }

    public virtual bool IsConfirmation(Cell from, Cell to)
    {
        return false;
    }


    public void TryMove(Cell cell, bool isTab = true)
    {
        Debug.Log("TryMove");
        if (!(!MoveHistory.InHistory || !_gameEnded)) return;
        if (isTab && _selectedCell == cell)
        {
            Deselect();
        }
        else if (_selectedCell != null)
        {
            Debug.Log("IsValidMove " + IsValidMove(_selectedCell, cell));
            if (IsValidMove(_selectedCell, cell))
            {
                BoardTryMove(_selectedCell, cell, isTab);
                Deselect();
            }
            else if (cell.Piece != null && IsMyId(cell.Piece.OwnerId))
            { 
                SelectMyPiece(cell);
            }
            else
            {
                Deselect();
            }
        }
        else if (cell.Piece != null)
        {
            if (IsMyId(cell.Piece.OwnerId)) SelectMyPiece(cell);
            else SelectEnemyPiece(cell);
        }

        return;

        void SelectMyPiece(Cell cell1)
        {
            Debug.Log($"cell1 {cell1.Piece.PieceType}");
            SelectCell(cell1);

            _thirdSelectedCell?.SetSelectedState(Cell.CellState.None);
            _thirdSelectedCell = cell1;
            _thirdSelectedCell.SetSelectedState(Cell.CellState.Selected);
            Debug.Log($"SelectMyPiece {_selectedCell.Piece.PieceType}");
        }

        void SelectEnemyPiece(Cell cell1)
        {
        }
    }

    public abstract void BoardTryMove(Cell from, Cell to, bool isTab = true);

    public void StartDragging(RectTransform piece)
    {
        if (MoveHistory.InHistory)
        {
            MoveHistory.HistoryToReal();
            return;
        }
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
    [CanBeNull]
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
    
    private void AnimateMove(Cell from, Cell to)
    {
        to.transform.SetAsLastSibling();
        var image = to.pieceImage;

        StartCoroutine(AnimatePieceMove(image, from.transform.position, to.transform.position, 0.1f));
    }

    private IEnumerator AnimatePieceMove(Image image, Vector3 startPos, Vector3 endPos, float duration)
    {
        float elapsed = 0f;

        image.transform.position = startPos;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / duration;
            image.transform.position = Vector3.Lerp(startPos, endPos, t);
            yield return null;
        }

        image.transform.position = endPos;
    }
}
}