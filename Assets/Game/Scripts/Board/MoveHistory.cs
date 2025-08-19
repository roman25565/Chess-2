using System.Collections.Generic;
using UnityEngine;

namespace Board
{
public class MoveHistory
{
    private readonly List<Move> _history = new List<Move>();
    public int HistoryIndex;
    public bool InHistory;

    public delegate void MoveDelegate(Cell from, Cell to);

    private readonly MoveDelegate _moveFunction;

    public MoveHistory(MoveDelegate moveFunction)
    {
        _moveFunction = moveFunction;
    }

    public void AddMove(Cell from, Cell to, bool isInternalHistoryMove)
    {
        if (InHistory && !isInternalHistoryMove) HistoryToReal();

        _history.Add(new Move { From = from, To = to, KilledPiece = to.Piece });
        Debug.Log("add move Killed Piece: " + to.Piece);
        
        HistoryIndex = _history.Count;
    }

    public void HistoryToReal()
    {
        if (InHistory) SetHistoryIndex(_history.Count);
    }

    public void SetHistoryIndex(int index)
    {
        Debug.Log("index: " + index);
        if (index < 0 || index > _history.Count) return;

        if (index == _history.Count)
        {
            if (InHistory) InHistory = false;
        }
        else if (index < _history.Count)
        {
            InHistory = true;
        }

        var movesCount = index - HistoryIndex;
        HistoryMove(movesCount);
        Debug.Log("selected_index: " + index);
    }

    private void HistoryMove(int recursionCount)
    {
        if (recursionCount < 0) //DOWN
        {
            var move = _history[HistoryIndex - 1];
            _moveFunction(move.To, move.From);
            move.To.SetPiece(move.KilledPiece);

            HistoryIndex--;
            recursionCount++;
            HistoryMove(recursionCount);
        }
        else if (recursionCount > 0) //UP
        {
            var move = _history[HistoryIndex];
            _moveFunction(move.From, move.To);

            HistoryIndex++;
            recursionCount--;
            HistoryMove(recursionCount);
        }
        else if (recursionCount == 0) //STOP
        {

        }
    }

    public List<Move> GetHistory()
    {
        return _history;
    }
}
}