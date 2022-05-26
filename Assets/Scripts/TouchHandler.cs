using System;
using UnityEngine;
using UnityEngine.EventSystems;

public class TouchHandler : MonoBehaviour, IPointerDownHandler, IPointerUpHandler, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    public Action<PointerEventData> OnPointerDownEvent;

    public Action<PointerEventData> OnPointerUpEvent;

    public Action<PointerEventData> OnPointerClickEvent;

    public Action<PointerEventData> OnBeginDragEvent;

    public Action<PointerEventData> OnDragEvent;

    public Action<PointerEventData> OnEndDragEvent;

    public void OnPointerDown(PointerEventData eventData)
    {
        OnPointerDownEvent?.Invoke(eventData);
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        OnPointerUpEvent?.Invoke(eventData);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        OnPointerClickEvent?.Invoke(eventData);
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        OnBeginDragEvent?.Invoke(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        OnDragEvent?.Invoke(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        OnEndDragEvent?.Invoke(eventData);
    }
}