// Responsible team member: Hanyun Zhu; Description: Accepts unique triangle, square, and circle symbols for the Level 2 symbol-slot puzzle.
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

public class UISymbolSlot : MonoBehaviour, IDropHandler
{
    [Header("Placed Symbol Settings")]
    public Vector2 placedSymbolSize = new Vector2(80f, 80f);

    private List<string> placedShapes = new List<string>();

    public bool IsFull
    {
        get { return placedShapes.Count >= 3; }
    }

    private void Awake()
    {
        Image img = GetComponent<Image>();

        if (img == null)
        {
            img = gameObject.AddComponent<Image>();
            img.color = new Color(1f, 1f, 1f, 0f);
        }

        img.raycastTarget = true;
    }

    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log("Slot received OnDrop: " + gameObject.name);

        GameObject dragObject = eventData.pointerDrag;

        if (dragObject == null)
        {
            Debug.LogWarning("OnDrop failed: pointerDrag is null.");
            return;
        }

        if (CanAccept(dragObject))
        {
            PlaceSymbol(dragObject);
        }
        else
        {
            Debug.Log("OnDrop slot rejected placement.");
        }
    }

    public bool CanAccept(GameObject dragObject)
    {
        if (IsFull)
        {
            Debug.Log("Slot is full.");
            return false;
        }

        string shapeType = GetShapeType(dragObject);

        Debug.Log("Detected shape type: " + dragObject.name + " -> " + shapeType);

        if (shapeType == "Unknown")
        {
            return false;
        }

        return !placedShapes.Contains(shapeType);
    }

    private string GetShapeType(GameObject obj)
    {
        if (obj.name.Contains("Triangle") || obj.name.Contains("\u4e09\u89d2\u5f62")) return "Triangle";
        if (obj.name.Contains("Square") || obj.name.Contains("\u6b63\u65b9\u5f62")) return "Square";
        if (obj.name.Contains("Circle") || obj.name.Contains("\u5706\u5f62")) return "Circle";

        return "Unknown";
    }

    public void PlaceSymbol(GameObject dragObject)
    {
        string shapeType = GetShapeType(dragObject);

        if (placedShapes.Contains(shapeType))
        {
            Debug.Log("This shape is repetitive");
            return;
        }

        placedShapes.Add(shapeType);
        CreateSymbolAtCenter(dragObject, shapeType);

        Debug.Log("Current shape: " + string.Join(", ", placedShapes));

        if (IsFull)
        {
            Debug.Log("The slot is filled");
        }
    }

    private void CreateSymbolAtCenter(GameObject dragObject, string shapeType)
    {
        GameObject symbol = new GameObject("Symbol_" + shapeType);
        symbol.transform.SetParent(transform, false);

        Image img = symbol.AddComponent<Image>();
        Image sourceImg = dragObject.GetComponent<Image>();

        if (sourceImg != null)
        {
            img.sprite = sourceImg.sprite;
            img.preserveAspect = true;
        }

        img.raycastTarget = false;

        RectTransform rt = symbol.GetComponent<RectTransform>();
        rt.anchorMin = new Vector2(0.5f, 0.5f);
        rt.anchorMax = new Vector2(0.5f, 0.5f);
        rt.pivot = new Vector2(0.5f, 0.5f);
        rt.anchoredPosition = Vector2.zero;
        rt.sizeDelta = placedSymbolSize;
    }
}
