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
        Debug.Log("新版开始拖拽: " + gameObject.name);

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
        Debug.Log("新版结束拖拽: " + gameObject.name);

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
            Debug.LogWarning("场景中没有 EventSystem");
            return;
        }

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        Debug.Log("结束拖拽时检测到 UI 数量: " + results.Count);

        foreach (RaycastResult result in results)
        {
            Debug.Log("Raycast 命中: " + result.gameObject.name);

            UISymbolSlot slot = result.gameObject.GetComponentInParent<UISymbolSlot>();

            if (slot != null)
            {
                Debug.Log("找到 UISymbolSlot: " + slot.gameObject.name);

                if (slot.CanAccept(gameObject))
                {
                    Debug.Log("插槽允许放入: " + slot.gameObject.name);
                    slot.PlaceSymbol(gameObject);
                }
                else
                {
                    Debug.Log("插槽拒绝放入，可能已满、重复、或图形类型 Unknown");
                }

                return;
            }
        }

        Debug.Log("没有找到任何 UISymbolSlot");
    }
}