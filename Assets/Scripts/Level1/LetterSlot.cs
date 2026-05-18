using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class LetterSlot : MonoBehaviour, IDropHandler
{
    public bool isOccupied = false;
    public bool isTarget = false;          // ← 新增：是否为正确位置

    private RectTransform rectTransform;
    private Draggable currentMarker;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void PlaceMarker(Draggable marker)
    {
        if (isOccupied) return;

        isOccupied = true;
        currentMarker = marker;

        // 将标记作为 Gap 的子物体（世界坐标保持不变）
        marker.transform.SetParent(transform, false);

        // 手动设置标记在 Gap 内的相对位置：水平居中，向下偏移 60
        RectTransform markerRect = marker.GetComponent<RectTransform>();
        markerRect.anchoredPosition = new Vector2(50, -65);

        // 添加 LayoutElement 并忽略布局，防止 HorizontalLayoutGroup 重置位置
        LayoutElement le = marker.GetComponent<LayoutElement>();
        if (le == null) le = marker.gameObject.AddComponent<LayoutElement>();
        le.ignoreLayout = true;

        // 撑开 Gap 自身的宽度，让字母分开
        rectTransform.sizeDelta = new Vector2(80, rectTransform.sizeDelta.y);
    }

    public void RemoveMarker(Draggable marker)
    {
        if (!isOccupied || currentMarker != marker) return;

        isOccupied = false;
        currentMarker = null;
        rectTransform.sizeDelta = new Vector2(0, rectTransform.sizeDelta.y);
    }

    public void OnDrop(PointerEventData eventData)
    {
        // 拖拽放置逻辑已在 Draggable 中处理，这里留空
    }
}
