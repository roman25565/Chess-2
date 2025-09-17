using System.Collections.Generic;
using System.IO;
using System.Linq;
using Board;
using Board.Piece;
using Newtonsoft.Json;
using Setting;
using TMPro;
using UI;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

namespace Game.Scripts.Board
{
public class ArrangementBoard : AbstractBoard
{
    [SerializeField] private Cell extraCellPrefab;
    [SerializeField] private Transform extraCellParent;
    [SerializeField] private Button saveButton;
    [SerializeField] private Button clearButton;
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
        
        foreach (var cell in BoardLines[5])
        {
            result.Add(new Vector2Int(cell.Row, cell.Column));
        }
        foreach (var cell in BoardLines[6])
        {
            result.Add(new Vector2Int(cell.Row, cell.Column));
        }
        foreach (var cell in BoardLines[7])
        {
            result.Add(new Vector2Int(cell.Row, cell.Column));
        }
        
        return result;
    }
    private void OnDisable()
    {
        ClearBoard();
        saveButton.onClick.RemoveListener(SaveArrangement);
        clearButton.onClick.RemoveListener(ClearArrangement);
    }
    protected override void OnEnable()
    {
        base.OnEnable();
        SetUpPieceCount();
        LoadArrangement();
        LoadExtraLine();
        saveButton.onClick.AddListener(SaveArrangement);
        clearButton.onClick.AddListener(ClearArrangement);
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

        Debug.Log("LoadExtraLine PiecesCount: " + Global.Pieces.Count);
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
        if (!CanSave()) return;
        var arrangements = new List<ArrangementEntry>();
        foreach (var vector2Int in _allPoints)
        {
            var cell = GetCell(vector2Int.x, vector2Int.y);
            if (cell.Piece != null)
            {
                Debug.Log("SaveArrangement" + cell.Piece.PieceType + " " + cell.Row + " " + cell.Column);
                arrangements.Add(new ArrangementEntry{row = cell.Row, column = cell.Column, pieceType = cell.Piece.PieceType});
            }
        }
        Global.MyArrangements = arrangements;
        Debug.Log("SaveArrangement" + Global.MyArrangements.Count);
        SaveToJson(arrangements);
    }
    
    private void SaveToJson(List<ArrangementEntry> pieces)
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
        if(to.Piece != null) return false;
        return to.Column > 4 && to.Column < 8;
    }

    public override void BoardTryMove(Cell from, Cell to, bool isTab = true)
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
            SetPieceCount(pieceType, _piecesCount[pieceType] - 1);
            from.SetPiece(null);
            Deselect();
        }
    }

    protected override void OnDraggingStop(Cell from, Cell to)
    {
        TryMove(to, false);
    }

    protected override void Move(Cell from, Cell to)
    {
        Debug.Log($"Move {from.Piece.PieceType}");
        var picetype = from.Piece.PieceType;
        var max = Global.Pieces[picetype].arrangementMax;
        if (_piecesCount[picetype] + 1 > max)
        {
            return;
        };

        SetPieceCount(from.Piece.PieceType, _piecesCount[picetype] + 1);

        to.SetPiece(from.Piece);
        Deselect();
    }

    #endregion

    [SerializeField] private TextMeshProUGUI piecesCostText;
    [SerializeField] private TextMeshProUGUI errorText;
    [SerializeField] private Color normalColor = Color.green;   // Зелений (30-50)
    [SerializeField] private Color warningColor = Color.yellow; // Жовтий (<30)
    [SerializeField] private Color dangerColor = Color.red;    // Червоний (>50)
    [SerializeField] private ShakeManager shakeManager;
    private void SetPieceCount(PieceType pieceType, int count)
    {
        Debug.Log("SetPieceCount" + pieceType + " " + count);
        
        _piecesCount[pieceType] = count;
        var piecesCost = GetPiecesCost();
        piecesCostText.text = piecesCost.ToString() + "/50";
        UpdatePiecesCostText(piecesCost);
    }
    
    void UpdatePiecesCostText(int piecesCost)
    {
        piecesCostText.text = $"{piecesCost}/50";
    
        if (piecesCost > 50)
        {
            piecesCostText.color = dangerColor;
            shakeManager.ShakeObject(piecesCostText.transform);
        }
        else if (piecesCost < 30)
        {
            piecesCostText.color = warningColor;
        }
        else
        {
            piecesCostText.color = normalColor;
        }
    }

    private int GetPiecesCost()
    {
        var piecesCost = 0;
        foreach (var pieceCount in _piecesCount)
        {

            var pieceCost = Global.Pieces[pieceCount.Key].arrangementCost;
            piecesCost += pieceCost * pieceCount.Value;

        }

        return piecesCost;
    }

    private bool CanSave()
    {
        string errorMessage = null;

        var piecesCost = GetPiecesCost();
        if (piecesCost < 0)
        {
            errorMessage = "wow You Cheater or my code too bad";
        }

        if (piecesCost > 50)
        {
            errorMessage = $"Maximum piece limit exceeded ({piecesCost}/50). Please remove some pieces.";
            shakeManager.ShakeObject(piecesCostText.transform);
        }

        if (_piecesCount[PieceType.King] != 1)
        {
            errorMessage = "Your army must contain exactly 1 King\n(Current: " +
                           _piecesCount[PieceType.King] + ")";
        }

        errorText.color = dangerColor;

        if (errorMessage == null)
        {
            errorText.color = normalColor;
            errorText.text = "Successfully saved";
            return true;
        }
#if (UNITY_ANDROID || UNITY_IOS) && !UNITY_EDITOR
        Handheld.Vibrate();
#endif
        errorText.text = errorMessage;
        shakeManager.ShakeObject(saveButton.transform);
        shakeManager.ShakeObject(errorText.transform);
        return false;

    }

    private void ClearArrangement()
    {
        ForEachCell(cell =>
        {
            if (cell.Row == 8) return;
            cell.SetPiece(null);
        });
        
        foreach (var pieceCount in _piecesCount.Keys.ToList())
        {
            _piecesCount[pieceCount] = 0;
        }
        var piecesCost = GetPiecesCost();
        UpdatePiecesCostText(piecesCost);
    }
}
}