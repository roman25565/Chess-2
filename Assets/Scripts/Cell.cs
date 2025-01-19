using System;
using Setting;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.Serialization;
using UnityEngine.UI;
using Zenject;

public class Cell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum CellState
    {
        None = 0,
        Attacked = 1,
        Moved = 2,
        Selected = 3,
    }

    [SerializeField] private Image pieceImage;
    [SerializeField] private Image movedImage;
    [SerializeField] private Image selectedImage;
    
    [Inject] Settings _setting;
    public void SetMovedState(CellState state)
    {
        switch (state)
        {
           case CellState.Moved:
               SetImage(movedImage, _setting.CellStates.moved);
               break;
           case CellState.None:
               SetImage(movedImage, _setting.CellStates.none);
               break;
           default:
               throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    public void SetSelectedState(CellState state)
    {
        switch (state)
        {
            case CellState.Selected:
                SetImage(selectedImage, _setting.CellStates.selected);
                break;
            case CellState.None:
                SetImage(selectedImage, _setting.CellStates.none);
                break;
            default:
                throw new ArgumentOutOfRangeException(nameof(state), state, null);
        }
    }

    private void SetImage(Image image, CellStatesData data)
    {
        image.sprite = data.value;
        image.color = data.color;
    }

    public int Column { get; set; }
    public int Row { get; set; }
    public Piece Piece { get; private set; }
    public Board Board { get; set; }
    
    public void SetPiece(Piece piece)
    {
        Piece = piece;
        if (piece == null)
        {
            pieceImage.sprite = null;
            SetAlpha(pieceImage, 0);
            return;
        }
        SetAlpha(pieceImage, 1);
        var skinIndex = Piece.Color == PieceColor.Black ? Piece.SelectedSkinIndex + 1 : Piece.SelectedSkinIndex;
        pieceImage.sprite = piece.Skins[skinIndex];

        void SetAlpha(Image image,float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
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
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Piece != null && Board.IsMyId(Piece.OwnerId))
        {
            transform.SetAsLastSibling();
            Board.StartDragging(pieceImage.rectTransform);
        }
        if (!Board.IsSelectedCell(this))
        {
            Board.TryMove(this);
        }
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("Board.IsSelectedCell(this)" + Board.IsSelectedCell(this));
        Board.StopDragging();
    }
}