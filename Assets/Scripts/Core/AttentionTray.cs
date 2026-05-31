using UnityEngine;
using TMPro;

public class AttentionTray : MonoBehaviour
{
    [Header("Tray Info")]
    public string keyName;

    [Header("Weight Generation")]
    public Transform weightParent;
    public GameObject weightPrefab;
    public TextMeshProUGUI countText;

    [Header("Display Count")]
    public bool useScaledDisplayCount = true;
    public int displayDivisor = 5;
    public int minDisplayCount = 1;
    public bool keepZeroAsZero = true;

    [Header("Weight Size")]
    public Vector2 weightSize = new Vector2(36f, 36f);
    public Vector3 weightScale = Vector3.one;

    [Header("Layout")]
    public float xSpacing = 18f;
    public float ySpacing = 12f;
    public int weightsPerRow = 5;
    public bool invertY = true;

    public void SetWeightCount(int count)
    {
        Debug.Log("Tray [" + keyName + "] SetWeightCount raw = " + count);
        ClearWeights();

        if (countText != null)
            countText.text = count.ToString();

        if (weightPrefab == null)
        {
            Debug.LogWarning("AttentionTray " + gameObject.name + ": weightPrefab is missing.");
            return;
        }

        if (weightParent == null)
        {
            Debug.LogWarning("AttentionTray " + gameObject.name + ": weightParent is missing.");
            return;
        }

        int displayCount = GetDisplayCount(count);

        Debug.Log("Tray [" + keyName + "] displayCount = " + displayCount);

        for (int i = 0; i < displayCount; i++)
        {
            GameObject weight = Instantiate(weightPrefab, weightParent);
            weight.name = keyName + "_Weight_" + i;

            RectTransform rect = weight.GetComponent<RectTransform>();

            if (rect != null)
            {
                int row = i / weightsPerRow;
                int col = i % weightsPerRow;

                float y = invertY ? -row * ySpacing : row * ySpacing;

                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(col * xSpacing, y);
                rect.sizeDelta = weightSize;
                rect.localScale = weightScale;
                rect.localRotation = Quaternion.identity;
            }
        }

        Debug.Log("Tray " + keyName + " generated " + displayCount + " display weights from raw count " + count);
    }

    int GetDisplayCount(int rawCount)
    {
        if (!useScaledDisplayCount)
            return rawCount;

        if (rawCount <= 0)
            return 0;

        if (displayDivisor <= 0)
            displayDivisor = 1;

        int scaled = Mathf.CeilToInt(rawCount / (float)displayDivisor);

        if (keepZeroAsZero && rawCount == 0)
            return 0;

        return Mathf.Max(minDisplayCount, scaled);
    }

    void ClearWeights()
    {
        if (weightParent == null)
            return;

        for (int i = weightParent.childCount - 1; i >= 0; i--)
        {
            Destroy(weightParent.GetChild(i).gameObject);
        }
    }

}