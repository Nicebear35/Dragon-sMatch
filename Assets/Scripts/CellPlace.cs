using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class CellPlace : MonoBehaviour, IPointerClickHandler
{
    public CellView Cell { get; private set; }

    private bool _isSelected;

    private Image _image;
    private Color _defaultColor;

    public event UnityAction<CellPlace> Selected;
    public event UnityAction<CellPlace> Deselected;

    [SerializeField] private Color _selectedColor;

    private void Start()
    {
        _image = GetComponent<Image>();
        _defaultColor = _image.color;
    }

    public Tween SetCell(CellView cell)
    {
        Cell = cell;

        Tween takingPlace = Cell.TakePlace(transform.position);

        return takingPlace;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_isSelected)
        {
            Deselect();
        }
        else
        {
            Select();
        }
    }

    public void DeleteElement()
    {
        Cell.Delete();
        Cell = null;
    }

    private void Select()
    {
        _isSelected = true;
        _image.color = _selectedColor;
        Selected?.Invoke(this);
    }

    public void Deselect()
    {
        _isSelected = false;
        _image.color = _defaultColor;
        Deselected?.Invoke(this);
    }
}
