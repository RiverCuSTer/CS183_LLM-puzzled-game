using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
public class OutputSlot : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
{
    [Header("Slot Data")]
    public int slotIndex = 1;
    [Header("UI")]
    public RectTransform rectTransform;
    public Image slotImage;
    private void OnValidate()
{
    rectTransform = GetComponent<RectTransform>();
    slotImage = GetComponent<Image>();
}

    [Header("Visual")]
    public Color normalColor = new Color(1f, 1f, 1f, 0.05f);
    public Color hoverColor = new Color(0.4f, 1f, 1f, 0.25f);
    public Color occupiedColor = new Color(0.3f, 1f, 0.6f, 0.35f);
    private Level4Manager manager;
    private bool occupied = false;
    private FeatureBlock currentFeature;
    public void Init(Level4Manager levelManager)
    {
        manager = levelManager;
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        if (slotImage == null)
        {
            slotImage = GetComponent<Image>();
        }
        occupied = false;
        currentFeature = null;
        if (slotImage != null)
        {
            slotImage.color = normalColor;
        }
    }
    public void OnPointerEnter(PointerEventData eventData)
    {
        if (occupied)
        {
            return;
        }
        if (slotImage != null)
        {
            slotImage.color = hoverColor;
        }
    }
    public void OnPointerExit(PointerEventData eventData)
    {
        if (occupied)
        {
            return;
        }
        if (slotImage != null)
        {
            slotImage.color = normalColor;
        }
    }
    public void PlaceFeature(FeatureBlock feature)
    {
        if (feature == null)
        {
            return;
        }
        occupied = true;
        currentFeature = feature;
        if (slotImage != null)
        {
            slotImage.color = occupiedColor;
        }
        if (rectTransform != null)
        {
            feature.MoveToWorldPosition(rectTransform.position);
        }
    }
    public bool IsOccupied()
    {
        return occupied;
    }
    public RectTransform GetRectTransform()
    {
        if (rectTransform == null)
        {
            rectTransform = GetComponent<RectTransform>();
        }
        return rectTransform;
    }
}
