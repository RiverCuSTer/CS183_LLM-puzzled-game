using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private const float SnapDistance = 120f;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private LetterSlot currentSlot;
    private LetterSlot previewSlot;

    [HideInInspector] public Transform textContainer;
    [HideInInspector] public List<LetterSlot> letterSlots;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null)
        {
            currentSlot.RemoveMarker(this);
            currentSlot = null;
        }

        ClearPreviewSlot();

        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);

        LayoutElement layoutElement = GetComponent<LayoutElement>();
        if (layoutElement != null)
        {
            layoutElement.ignoreLayout = false;
        }

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
        MoveToPointer(eventData);
    }

    public void OnDrag(PointerEventData eventData)
    {
        MoveToPointer(eventData);
        UpdatePreviewSlot(eventData);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        LetterSlot closestSlot = FindClosestSlot(eventData.position, SnapDistance);
        ClearPreviewSlot();

        if (closestSlot != null)
        {
            closestSlot.PlaceMarker(this);
            currentSlot = closestSlot;
            Debug.Log("Marker snapped to gap.");
        }
        else
        {
            Debug.Log("Marker is not close to any gap.");
        }
    }

    private void UpdatePreviewSlot(PointerEventData eventData)
    {
        LetterSlot closestSlot = FindClosestSlot(eventData.position, SnapDistance);
        if (closestSlot == previewSlot) return;

        ClearPreviewSlot();

        previewSlot = closestSlot;
        if (previewSlot != null)
        {
            previewSlot.PreviewMarker(this);
        }
    }

    private LetterSlot FindClosestSlot(Vector2 screenPosition, float maxDistance)
    {
        if (letterSlots == null) return null;

        LetterSlot closestSlot = null;
        float closestDistance = maxDistance;
        Camera eventCamera = canvas == null ? null : canvas.worldCamera;

        foreach (LetterSlot slot in letterSlots)
        {
            if (slot == null || slot.isOccupied) continue;

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            Vector2 slotScreenPosition = RectTransformUtility.WorldToScreenPoint(eventCamera, slotRect.position);
            float distance = Vector2.Distance(screenPosition, slotScreenPosition);

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlot = slot;
            }
        }

        return closestSlot;
    }

    private void ClearPreviewSlot()
    {
        if (previewSlot != null)
        {
            previewSlot.ClearPreview();
            previewSlot = null;
        }
    }

    private void MoveToPointer(PointerEventData eventData)
    {
        if (canvas == null) return;

        RectTransform canvasRect = canvas.transform as RectTransform;
        if (canvasRect == null) return;

        Camera eventCamera = canvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : canvas.worldCamera;
        if (RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, eventData.position, eventCamera, out Vector2 localPoint))
        {
            rectTransform.anchoredPosition = localPoint;
        }
    }
}
