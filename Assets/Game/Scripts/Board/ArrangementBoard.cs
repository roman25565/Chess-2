using System.Collections.Generic;
using System.IO;
using System.Linq;
using Board.Piece;
using Newtonsoft.Json;
using Setting;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Board
{
public class ArrangementBoard : AbstractBoard
{
    [SerializeField] private Cell extraCellPrefab;
    [SerializeField] private Transform extraCellParent;
    [SerializeField] private Button saveButton;
    private List<Vector2Int> _allPoints;
    public override bool IsMyId(ulong id) => true;
    protected override bool IsRotated => false;
    private void Start()
    {
        _allPoints = SetUp();
    }

    private List<Vector2Int> SetUp()
    {
        var result = new List<Vector2Int>();
        
        foreach (var cell1 in BoardLines[5])
        {
            result.Add(new Vector2Int(cell1.Row, cell1.Column));
        }
        foreach (var cell1 in BoardLines[6])
        {
            result.Add(new Vector2Int(cell1.Row, cell1.Column));
        }
        foreach (var cell1 in BoardLines[7])
        {
            result.Add(new Vector2Int(cell1.Row, cell1.Column));
        }
        
        return result;
    }
#if !UNITY_SERVER
    private void OnDisable()
    {
        ClearBoard();
        saveButton.onClick.RemoveListener(SaveArrangement);
    }

#endif
    protected override void OnEnable()
    {
        base.OnEnable();
        SetUpPieceCount();
        LoadArrangement();
        LoadExtraLine();
        saveButton.onClick.AddListener(SaveArrangement);

    }

    private void SetUpPieceCount()
    {
        _piecesCount = new Dictionary<PieceType, int>();
        for (int i = 0; i < Global.Pieces.Count; i++) {
            var item = Global.Pieces.ElementAt(i);
            _piecesCount.Add(item.Key, 0);
        }
    }

    private void LoadArrangement()
    {
        foreach (var arrangement in Global.MyArrangements)
        {
            var row = arrangement.row;
            var column = arrangement.column;
            var pieceType = arrangement.pieceType;
            GetCell(row, column).SetPiece(Global.CreatePiece(pieceType));
            
            _piecesCount[pieceType]++;
            SetPieceCount(pieceType, _piecesCount[pieceType] + 1);
        }
    }

    private void LoadExtraLine()
    {
        int childCount = extraCellParent.childCount;

        for (int i = childCount - 1; i >= 0; i--)
        {
            Destroy(extraCellParent.GetChild(i).gameObject);
        }
        
        BoardLines.Remove(8);
        
        var extraCells = new List<Cell>();

        Debug.Log("Global.Pieces.Count: " + Global.Pieces.Count);
        for (int i = 0; i < Global.Pieces.Count; i++) {
            var item = Global.Pieces.ElementAt(i);
            
            var cell = Instantiate(extraCellPrefab, extraCellParent);
            ProjectContext.Instance.Container.InjectGameObject(cell.gameObject);
            extraCells.Add(cell);
            cell.Init(8,i, this, Global.CreatePiece(item.Value.pieceType));
            cell.Piece.OwnerId = 1;
            
        }
        BoardLines.Add(8, extraCells);
    }

    private void SaveArrangement()
    {
        var arrangements = new List<ArrangementEntry>();
        foreach (var vector2Int in _allPoints)
        {
            var cell = GetCell(vector2Int.x, vector2Int.y);
            Debug.Log("x" + vector2Int.x);
            if (cell.Piece != null)
            {
                arrangements.Add(new ArrangementEntry{row = cell.Row, column = cell.Column, pieceType = cell.Piece.PieceType});
                Debug.Log(cell.Row);
            }
        }
        Global.MyArrangements = arrangements;
        SaveArrangementToJson(arrangements);
    }

    private void SaveArrangementToJson(List<ArrangementEntry> pieces)
    {
        string json = JsonConvert.SerializeObject(pieces, Formatting.Indented);

        File.WriteAllText(Global.ArrangementFile, json);
    }

    #region Move

    private Dictionary<PieceType, int> _piecesCount;

    protected override List<Vector2Int> GetPoints(Cell cell, AbstractPiece piece)
    {
        return _allPoints;
    }

    protected override bool IsValidMove(Cell from, Cell to)
    {
        Debug.Log(to.Column);
        return to.Column > 4 && to.Column < 8;
    }

    protected override void OnCanMove(Cell from, Cell to)
    {
        MovePiece(from, to);
    }

    protected override void MoveToOutScreen(Cell from)
    {
        Debug.Log(from.Row);
        if (from.Row != 8)
        {
            var pieceType = from.Piece.PieceType;
            var min = Global.Pieces[pieceType].arrangementMin;
            var a = SetPieceCount(pieceType, _piecesCount[pieceType] - 1);
            Debug.Log(a);
            from.SetPiece(null);
            Deselect();
        }
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        if (from == to)
        {
            return;
        }
        MovePiece(from, to);
    }

    protected override void Move(Cell from, Cell to)
    {
        if (to.Piece != null)
        {
            MoveToOutScreen(to);
        }
        var picetype = from.Piece.PieceType;
        var max = Global.Pieces[picetype].arrangementMax;
        if (_piecesCount[picetype] + 1 > max)
        {
            return;
        }

        if (SetPieceCount(from.Piece.PieceType, _piecesCount[picetype] + 1))
        {
            to.SetPiece(from.Piece);
            Deselect();
        }
        
    }
    
    #endregion

    [SerializeField] private TextMeshProUGUI piecesCostText;
    private bool SetPieceCount(PieceType pieceType, int count)
    {
        var piecesCost = 0;
        foreach (var pieceCount in _piecesCount)
        {
            if (pieceCount.Key != pieceType)
            {
                var pieceCost = Global.Pieces[pieceCount.Key].arrangementCost;
                piecesCost += pieceCost * pieceCount.Value; 
            }
        }
        piecesCost += Global.Pieces[pieceType].arrangementCost * count;

        if (piecesCost < 50)
        {
            _piecesCount[pieceType] = count;
            piecesCostText.text = piecesCost.ToString() + "/50";
        }
        return piecesCost < 50;
    }
}
}