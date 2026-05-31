using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LetterSlot : MonoBehaviour, IDropHandler
{
    public bool isOccupied = false;
    public bool isTarget = false;

    private const float NormalWidth = 0f;
    private const float MinExpandedWidth = 90f;
    private const float ExpandPadding = 10f;
    private const float ExpandSpeed = 18f;

    private RectTransform rectTransform;
    private LayoutElement layoutElement;
    private Draggable currentMarker;
    private float targetWidth = NormalWidth;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        layoutElement = GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = NormalWidth;
        layoutElement.preferredWidth = NormalWidth;
        layoutElement.flexibleWidth = 0f;
    }

    void Update()
    {
        if (layoutElement == null || rectTransform == null) return;

        layoutElement.preferredWidth = Mathf.Lerp(layoutElement.preferredWidth, targetWidth, Time.deltaTime * ExpandSpeed);
        if (Mathf.Abs(layoutElement.preferredWidth - targetWidth) < 0.5f)
        {
            layoutElement.preferredWidth = targetWidth;
        }

        rectTransform.sizeDelta = new Vector2(layoutElement.preferredWidth, rectTransform.sizeDelta.y);
    }

    public void PreviewMarker(Draggable marker)
    {
        if (isOccupied) return;

        targetWidth = CalculateExpandedWidth(marker);
    }

    public void ClearPreview()
    {
        if (isOccupied) return;

        targetWidth = NormalWidth;
    }

    public void PlaceMarker(Draggable marker)
    {
        if (isOccupied) return;

        isOccupied = true;
        currentMarker = marker;
        targetWidth = CalculateExpandedWidth(marker);

        marker.transform.SetParent(transform, false);

        RectTransform markerRect = marker.GetComponent<RectTransform>();
        markerRect.anchorMin = new Vector2(0.5f, 0.5f);
        markerRect.anchorMax = new Vector2(0.5f, 0.5f);
        markerRect.pivot = new Vector2(0.5f, 0.5f);
        markerRect.anchoredPosition = new Vector2(0f, -65f);

        LayoutElement markerLayout = marker.GetComponent<LayoutElement>();
        if (markerLayout == null)
        {
            markerLayout = marker.gameObject.AddComponent<LayoutElement>();
        }

        markerLayout.ignoreLayout = true;
    }

    public void RemoveMarker(Draggable marker)
    {
        if (!isOccupied || currentMarker != marker) return;

        isOccupied = false;
        currentMarker = null;
        targetWidth = NormalWidth;
    }

    public void OnDrop(PointerEventData eventData)
    {
        // Placement is handled by Draggable so nearest-slot snapping can work.
    }

    private float CalculateExpandedWidth(Draggable marker)
    {
        RectTransform markerRect = marker == null ? null : marker.GetComponent<RectTransform>();
        float markerWidth = markerRect == null ? MinExpandedWidth : markerRect.rect.width;
        return Mathf.Max(MinExpandedWidth, markerWidth + ExpandPadding);
    }
}
