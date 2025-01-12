using System;
using Setting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;
using Zenject;

public class Cell : MonoBehaviour
{
    public enum CellState
    {
        None = 0,
        Attacked = 1,
        Moved = 2,
        Selected = 3,
    }
    private CellState _state = CellState.None;

    [SerializeField] private Image pieceImage;
    [SerializeField] private Image stateImage;
    
    [Inject] Settings _setting;
    public void SetState(CellState state)
    {
        _state = state;
        switch (state)
        {
           case CellState.Attacked:
               SetImage(_setting.CellStates.attacked);
               break;
           case CellState.Moved:
               SetImage(_setting.CellStates.moved);
               break;
           case CellState.Selected:
               SetImage(_setting.CellStates.selected);
               break;
           case CellState.None:
               SetAlpha(stateImage, 0);
               break;
           default:
               throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
        void SetImage(CellStatesData data)
        {
            stateImage.sprite = data.Value;
            SetAlpha(stateImage, data.Alpha);
        }
    }
    
    public int Column { get; set; }
    public int Row { get; set; }
    public Piece Piece { get; private set; }
    public Board Board { get; set; }


    public void SetPiece(Piece piece)
    {
        if (piece == null)
        {
            pieceImage.sprite = null;
            SetAlpha(pieceImage, 0);
            return;
        }
        Piece = piece;
        var skinIndex = Piece.Color == PieceColor.Black ? Piece.SelectedSkinIndex + 1 : Piece.SelectedSkinIndex;
        pieceImage.sprite = piece.Skins[skinIndex];
        SetAlpha(pieceImage, 1);


    }

    public Cell Init(int row, int column, Board board, Piece piece = null)
    {
        Row = row;
        Column = column;
        Piece = piece;
        Board = board;
        return this;
    }

    public void OnClick()
    {
        Debug.Log("Click");
        Board.OnClick(this);
    }
    

    private void SetAlpha(Image image,float alpha)
    {
        var color = image.color;
        color.a = alpha;
        image.color = color;
    }
}