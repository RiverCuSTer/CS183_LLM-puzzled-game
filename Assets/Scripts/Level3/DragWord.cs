// Responsible team member: Zhaoning Chu; Description: Provides the legacy word dragging behaviour for an older Level 3 slot-based puzzle.
using UnityEngine;//   25125851  chuzhaoning
using UnityEngine.EventSystems;

public class DragWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rect;
    private Transform initialParent;

    // Store the start position in world coordinates to avoid snapping back to the screen center.
    private Vector3 homeWorldPos;

    private CanvasGroup canvasGroup;

    private bool isSnapped = false;
    private bool isDragging = false; // Drag-state lock to prevent movement from continuing after release.

    [Header("Target Settings")]
    public RectTransform mySlot;

    [Header("Snap Settings")]
    [Tooltip("Screen-pixel distance for automatic snapping. Increase this value for a larger snap range, such as 200-300.")]
    public float snapDistance = 200f;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        initialParent = transform.parent;

        // Record the initial world position so parent layout changes do not affect reset.
        homeWorldPos = transform.position;

        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    private Camera GetUICamera()
    {
        Canvas canvas = GetComponentInParent<Canvas>();
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            return canvas.worldCamera ?? Camera.main;
        }
        return null;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isSnapped) return;
        isDragging = true; // Mark dragging as started.
        rect.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // Do not move after snapping or when no drag is active.
        if (isSnapped || !isDragging) return;

        rect.anchoredPosition += eventData.delta;

        if (mySlot != null)
        {
            Camera cam = GetUICamera();
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(cam, mySlot.position);
            float distance = Vector2.Distance(eventData.position, slotScreenPos);

            // Snap immediately once the word enters the slot range.
            if (distance <= snapDistance)
            {
                SnapToSlot();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false; // End dragging when the pointer is released.
        if (isSnapped) return;

        canvasGroup.blocksRaycasts = true;
        ReturnToHome();
    }

    private void SnapToSlot()
    {
        isSnapped = true;
        isDragging = false; // Force the drag state to end immediately.

        // 1. Parent the word to the slot.
        transform.SetParent(mySlot, false);
        // 2. Center it in the slot.
        rect.anchoredPosition = Vector2.zero;

        // 3. Disable interaction and this script so the word stays in place.
        canvasGroup.interactable = false;
        this.enabled = false;

        // 4. Notify the slot checker.
        SlotChecker checker = mySlot.GetComponent<SlotChecker>();
        if (checker != null) checker.OnWordDropped(this);
    }

    public void ReturnToHome()
    {
        isSnapped = false;
        isDragging = false;
        this.enabled = true;
        canvasGroup.interactable = true;

        // Restore the original parent while keeping the current world position.
        transform.SetParent(initialParent, true);

        // Force the exact original world position.
        transform.position = homeWorldPos;

        canvasGroup.blocksRaycasts = true;
    }
}
