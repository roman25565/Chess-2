using System;
using Board.Piece;
using Setting;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using Zenject;

namespace Board
{
public class Cell : MonoBehaviour, IPointerDownHandler, IPointerUpHandler
{
    public enum CellState
    {
        None = 0,
        Attacked = 1,
        Moved = 2,
        Selected = 3,
    }
    
    public int Column { get; set; }
    public int Row { get; private set; }
    public AbstractPiece Piece { get; private set; }
    public AbstractBoard Board { get; private set; }

    [SerializeField] private Image pieceImage;
    [SerializeField] private Image movedImage;
    [SerializeField] private Image selectedImage;
    
    [Inject] private Global _global;
    
    public void SetMovedState(CellState state)
    {
        switch (state)
        {
            case CellState.Moved:
                SetImage(movedImage, _global.CellStates.moved);
                break;
            case CellState.None:
                SetImage(movedImage, _global.CellStates.none);
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
                SetImage(selectedImage, _global.CellStates.selected);
                break;
            case CellState.None:
                SetImage(selectedImage, _global.CellStates.none);
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

    
    public void SetPiece(AbstractPiece abstractPiece)
    {
        Piece = abstractPiece;
        if (abstractPiece == null)
        {
            pieceImage.sprite = null;
            SetAlpha(pieceImage, 0);
            return;
        }
        SetAlpha(pieceImage, 1);
        var skinIndex = Piece.Color == PieceColor.Black ? Piece.SelectedSkinIndex + 1 : Piece.SelectedSkinIndex;
        pieceImage.sprite = abstractPiece.Skins[skinIndex];

        void SetAlpha(Image image,float alpha)
        {
            var color = image.color;
            color.a = alpha;
            image.color = color;
        }
    }

    public Cell Init(int row, int column, AbstractBoard board, AbstractPiece abstractPiece = null)
    {
        SetPiece(abstractPiece);
        Row = row;
        Column = column;
        Piece = abstractPiece;
        Board = board;
        return this;
    }

    public void OnClick()
    {
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        if (Piece != null && Board.IsMyId(Piece.OwnerId) && Row != 8)
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
        Board.StopDragging();
    }
}
}