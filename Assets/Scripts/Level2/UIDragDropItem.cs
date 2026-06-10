// Responsible team member: Hanyun Zhu, Zhiyu Huang; Description: Implements draggable UI symbol items and places them into compatible symbol slots.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UIDragDropItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private GameObject dragVisual;
    private RectTransform dragVisualRect;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        Debug.Log("New drag started: " + gameObject.name);

        canvasGroup.alpha = 0.5f;
        canvasGroup.blocksRaycasts = false;

        dragVisual = new GameObject(gameObject.name + "_DragVisual");
        dragVisual.transform.SetParent(canvas.transform, false);
        dragVisual.transform.SetAsLastSibling();

        Image img = dragVisual.AddComponent<Image>();
        Image sourceImg = GetComponent<Image>();

        if (sourceImg != null)
        {
            img.sprite = sourceImg.sprite;
            img.preserveAspect = true;
        }

        img.raycastTarget = false;

        dragVisualRect = dragVisual.GetComponent<RectTransform>();
        dragVisualRect.sizeDelta = rectTransform.sizeDelta;
        dragVisualRect.position = eventData.position;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (dragVisualRect != null)
        {
            dragVisualRect.position = eventData.position;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        Debug.Log("New drag ended: " + gameObject.name);

        TryPlaceToSlot(eventData);

        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (dragVisual != null)
        {
            Destroy(dragVisual);
            dragVisual = null;
            dragVisualRect = null;
        }
    }

    private void TryPlaceToSlot(PointerEventData eventData)
    {
        if (EventSystem.current == null)
        {
            Debug.LogWarning("No EventSystem found in the scene.");
            return;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log("UI hits detected at drag end: " + results.Count);

        foreach (RaycastResult result in results)
        {
            Debug.Log("Raycast hit: " + result.gameObject.name);

            UISymbolSlot slot = result.gameObject.GetComponentInParent<UISymbolSlot>();

            if (slot != null)
            {
                Debug.Log("Found UISymbolSlot: " + slot.gameObject.name);

                if (slot.CanAccept(gameObject))
                {
                    Debug.Log("Slot accepted placement: " + slot.gameObject.name);
                    slot.PlaceSymbol(gameObject);
                }
                else
                {
                    Debug.Log("Slot rejected placement because it may be full, duplicated, or an unknown shape type.");
                }

                return;
            }
        }

        Debug.Log("No UISymbolSlot found.");
    }
}
