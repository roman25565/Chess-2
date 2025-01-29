using System.Collections.Generic;
using System.IO;
using System.Linq;
using Newtonsoft.Json;
using NUnit.Framework;
using Setting;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

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

    private void OnDisable()
    {
        ForEachCell(cell =>
        {
            cell.SetPiece(null);
            cell.SetMovedState(Cell.CellState.None);
            cell.SetSelectedState(Cell.CellState.None);
        });
        saveButton.onClick.RemoveListener(SaveArrangement);
    }


    protected override void OnEnable()
    {
        foreach (var vector2Int in BoardLines.Values)
        {
            foreach (var cell in vector2Int)
            {
                Debug.Log("x " + cell.Row + " y " + cell.Column);
            }

        }
        base.OnEnable();
        LoadArrangement();
        LoadExtraLine();
        saveButton.onClick.AddListener(SaveArrangement);
    }

    private void LoadArrangement()
    {
        foreach (var arrangement in Settings.MyArrangements)
        {
            var row = arrangement.row;
            var column = arrangement.column;
            GetCell(row, column).SetPiece(Settings.CreatePiece(arrangement.pieceType));
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

        Debug.Log("Settings.Pieces.Count: " + Settings.Pieces.Count);
        for (int i = 0; i < Settings.Pieces.Count; i++) {
            var item = Settings.Pieces.ElementAt(i);
            
            var cell = Instantiate(extraCellPrefab, extraCellParent);
            ProjectContext.Instance.Container.InjectGameObject(cell.gameObject);
            extraCells.Add(cell);
            cell.Init(8,i, this, Settings.CreatePiece(item.Value.pieceType));
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
        Settings.MyArrangements = arrangements;
        SaveArrangementToJson(arrangements);
    }

    private void SaveArrangementToJson(List<ArrangementEntry> pieces)
    {
        string json = JsonConvert.SerializeObject(pieces, Formatting.Indented);

        File.WriteAllText(Settings.ArrangementFile, json);
    }

    #region Move

    protected override List<Vector2Int> GetLastPoints(Cell cell)
    {
        return _allPoints;
    }

    protected override void OnCanMove(Cell from, Cell to)
    {
        MovePiece(from, to);
    }

    protected override void MoveToOutScreen(Cell from)
    {
        if (from.Row != 8) from.SetPiece(null);
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        MovePiece(from, to);
    }

    protected override void Move(Cell from, Cell to)
    {
        to.SetPiece(from.Piece);
    }
    
    #endregion
}