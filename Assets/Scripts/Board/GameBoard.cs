using System;
using System.Collections.Generic;
using UnityEngine;

public class GameBoard : AbstractBoard
{
    protected override void OnCanMove(Cell from, Cell to)
    {
        MatchCore.TryMove(new Vector2Int(from.Row, from.Column),
            new Vector2Int(to.Row, to.Column));
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        TryMove(to, false);
    }

    protected override void Move(Cell from, Cell to)
    {
        to.SetPiece(from.Piece);
        from.SetPiece(null);
    }
#if !UNITY_SERVER
    #region MoveHistory

    private bool _inHistory;
    private bool _gameEnded;
    private readonly List<Move> _history = new List<Move>();
    private int _historyIndex;

    protected override void AddMoveToHistory(Cell from, Cell to)
    {
        HistoryToReal();
        
        _history.Add(new Move{From = from, To = to, AbstractPiece = to.Piece});
        _historyIndex = _history.Count;
        
        if (_inHistory)HistoryToReal();
    }
    
    public void NextMove()
    {
        if (!_inHistory) return;
        SetHistoryIndex(_historyIndex + 1);
    }

    public void UndoMove()
    {
        SetHistoryIndex(_historyIndex - 1);
    }
    

    private void HistoryToReal()
    {
        if (_inHistory) SetHistoryIndex(_history.Count);
    }

    private void SetHistoryIndex(int index)
    {
        if (index < 0) return;
        if (index > _history.Count) return;
        if (index == _history.Count)
        {
            if (_inHistory) _inHistory = false;
        }
        else if (index < _history.Count)
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
            var move = _history[_historyIndex - 1]; 
            Move(move.To, move.From);
            move.To.SetPiece(move.AbstractPiece);
            
            _historyIndex--;
            recursionCount++;
            HistoryMove(recursionCount);
        }
        else if (recursionCount > 0)//UP
        {
            var move = _history[_historyIndex]; 
            Move(move.From, move.To);
            
            _historyIndex++;
            recursionCount--;
            HistoryMove(recursionCount);
        }
        else if (recursionCount == 0)//STOP
        {

        }
    }

    protected override bool CanTryMove()
    {
        return !_inHistory || !_gameEnded;
    }

    public override void EndGame()
    {
        _gameEnded = true;
    }

    public override List<Move> GetHistory()
    {
        return _history;
    }
    #endregion
#endif
}