using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Draggable : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Canvas canvas;
    private LetterSlot currentSlot;   // 如果已放置，记录所在槽位

    [HideInInspector] public Transform textContainer;
    [HideInInspector] public List<LetterSlot> letterSlots;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvas = GetComponentInParent<Canvas>();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null)
        {
            currentSlot.RemoveMarker(this);
            currentSlot = null;
        }

        // 将标记移到 Canvas 根层级，避免布局干扰
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();

        // 移除 ignoreLayout 状态
        LayoutElement le = GetComponent<LayoutElement>();
        if (le != null && le.ignoreLayout)
            le.ignoreLayout = false;

        canvasGroup.alpha = 0.7f;
        canvasGroup.blocksRaycasts = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        // 寻找最近的空槽位
        LetterSlot closestSlot = null;
        float closestDistance = 100f;

        foreach (var slot in letterSlots)
        {
            if (slot.isOccupied) continue;

            RectTransform slotRect = slot.GetComponent<RectTransform>();
            float distance = Vector2.Distance(
                eventData.position,
                RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, slotRect.position)
            );

            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestSlot = slot;
            }
        }

        if (closestSlot != null)
        {
            // 吸附到槽位
            closestSlot.PlaceMarker(this);
            currentSlot = closestSlot;
            Debug.Log("标记已吸附");
        }
        else
        {
            // 没找到合适槽位，回到 tokenPool 位置（或留在原地）
            Debug.Log("未靠近任何间隙");
        }
    }
}
