using UnityEngine;//   25125851  chuzhaoning
using UnityEngine.EventSystems;

public class DragWord : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rect;
    private Transform initialParent;

    // 【核心修复1】改用世界坐标记录初始位置，彻底解决回弹到屏幕中间的问题！
    private Vector3 homeWorldPos;

    private CanvasGroup canvasGroup;

    private bool isSnapped = false;
    private bool isDragging = false; // 【核心修复2】增加拖拽状态锁，解决停不下来的问题

    [Header("目标设置")]
    public RectTransform mySlot;

    [Header("吸附设置")]
    [Tooltip("靠近多远自动吸附（屏幕像素）。想要大范围吸附，请改大这个值（如 200~300）")]
    public float snapDistance = 200f;

    void Start()
    {
        rect = GetComponent<RectTransform>();
        initialParent = transform.parent;

        // 记录初始的世界坐标（绝对精准，不受父物体布局影响）
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
        isDragging = true; // 标记开始拖拽
        rect.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        // 【核心修复2】只要已经吸附，或者不在拖拽状态，绝对不执行移动代码！
        if (isSnapped || !isDragging) return;

        rect.anchoredPosition += eventData.delta;

        if (mySlot != null)
        {
            Camera cam = GetUICamera();
            Vector2 slotScreenPos = RectTransformUtility.WorldToScreenPoint(cam, mySlot.position);
            float distance = Vector2.Distance(eventData.position, slotScreenPos);

            // 只要进入范围，立刻吸附
            if (distance <= snapDistance)
            {
                SnapToSlot();
            }
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        isDragging = false; // 鼠标松开，结束拖拽状态
        if (isSnapped) return;

        canvasGroup.blocksRaycasts = true;
        ReturnToHome();
    }

    private void SnapToSlot()
    {
        isSnapped = true;
        isDragging = false; // 立刻强制结束拖拽状态！

        // 1. 成为槽的子物体
        transform.SetParent(mySlot, false);
        // 2. 居中（这里用 anchoredPosition 是因为相对于槽，居中就是 0,0）
        rect.anchoredPosition = Vector2.zero;

        // 3. 【关键】彻底关闭交互和脚本，物理级“停下来”！
        canvasGroup.interactable = false;
        this.enabled = false;

        // 4. 通知判定
        SlotChecker checker = mySlot.GetComponent<SlotChecker>();
        if (checker != null) checker.OnWordDropped(this);
    }

    public void ReturnToHome()
    {
        isSnapped = false;
        isDragging = false;
        this.enabled = true;
        canvasGroup.interactable = true;

        // 恢复父物体（true表示保持当前世界位置不变）
        transform.SetParent(initialParent, true);

        // 【核心修复1】直接强制恢复世界坐标！指哪打哪，绝对不回屏幕中间！
        transform.position = homeWorldPos;

        canvasGroup.blocksRaycasts = true;
    }
}
