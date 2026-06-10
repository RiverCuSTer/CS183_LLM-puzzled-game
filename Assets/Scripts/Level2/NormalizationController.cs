// Responsible team member: Hanyun Zhu, Zhiyu Huang; Description: Runs the visual normalization sequence for raw attention weights.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class NormalizationController : MonoBehaviour
{
    [Header("Button")]
    public Button normalizeButton;

    [Header("Number UI, placed above the three trays")]
    public GameObject numberPanel;
    public TextMeshProUGUI whoNumberText;
    public TextMeshProUGUI amNumberText;
    public TextMeshProUGUI iNumberText;

    [Header("Objects To Hide After Normalization")]
    public AttentionBalanceController balanceController;
    public GameObject balanceRoot;
    public GameObject[] extraObjectsToHide;

    [Header("Final Popups")]
    public GameObject purposePopup;
    public TextMeshProUGUI purposeText;

    public GameObject nextLevelPopup;
    public TextMeshProUGUI nextLevelText;

    [Header("Timing")]
    public float normalizedNumberDuration = 2f;

    private int whoWeight;
    private int amWeight;
    private int iWeight;

    private bool hasNormalized = false;
    private bool isNormalizing = false;
    private bool hasReportedLevelComplete = false;

    void Start()
    {
        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(false);
            normalizeButton.onClick.RemoveAllListeners();
            normalizeButton.onClick.AddListener(OnNormalizeButtonClicked);
        }

        if (numberPanel != null)
            numberPanel.SetActive(false);

        if (purposePopup != null)
            purposePopup.SetActive(false);

        if (nextLevelPopup != null)
            nextLevelPopup.SetActive(false);
    }

    public void SetRawWeights(int who, int am, int i)
    {
        whoWeight = who;
        amWeight = am;
        iWeight = i;

        Debug.Log($"Raw weights: Who={whoWeight}, Am={amWeight}, I={iWeight}");
    }

    public void ShowNormalizeButton()
    {
        if (hasNormalized) return;

        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(true);
            normalizeButton.interactable = true;
        }
    }

    public void HideNormalizeButton()
    {
        if (normalizeButton != null)
            normalizeButton.gameObject.SetActive(false);
    }

    public void OnNormalizeButtonClicked()
    {
        if (isNormalizing) return;

        isNormalizing = true;
        hasNormalized = true;

        HideNormalizeButton();

        StartCoroutine(NormalizeRoutine());
    }

    IEnumerator NormalizeRoutine()
    {
        ShowNormalizedNumbers();

        yield return new WaitForSeconds(normalizedNumberDuration);

        HideAllPreviousObjects();

        ShowFinalPopups();

        isNormalizing = false;
    }

    void ShowNormalizedNumbers()
    {
        if (numberPanel != null)
            numberPanel.SetActive(true);

        float total = whoWeight + amWeight + iWeight;

        if (total <= 0f)
            total = 1f;

        float whoValue = whoWeight / total;
        float amValue = amWeight / total;
        float iValue = iWeight / total;

        // Keep the displayed two-decimal values adding up to exactly 1.00.
        // Round the first two values, then compute the third as 1 minus them.
        float whoDisplay = Mathf.Round(whoValue * 100f) / 100f;
        float amDisplay = Mathf.Round(amValue * 100f) / 100f;
        float iDisplay = 1f - whoDisplay - amDisplay;

        if (iDisplay < 0f)
            iDisplay = 0f;

        iDisplay = Mathf.Round(iDisplay * 100f) / 100f;

        if (whoNumberText != null)
            whoNumberText.text = whoDisplay.ToString("0.00");

        if (amNumberText != null)
            amNumberText.text = amDisplay.ToString("0.00");

        if (iNumberText != null)
            iNumberText.text = iDisplay.ToString("0.00");

        Debug.Log($"Normalized: Who={whoDisplay:0.00}, Am={amDisplay:0.00}, I={iDisplay:0.00}");
    }

    void HideAllPreviousObjects()
    {
        if (numberPanel != null)
            numberPanel.SetActive(false);

        if (normalizeButton != null)
            normalizeButton.gameObject.SetActive(false);

        if (balanceController != null)
        {
            balanceController.HideBalance();
        }
        else if (balanceRoot != null)
        {
            balanceRoot.SetActive(false);
        }

        if (extraObjectsToHide != null)
        {
            for (int i = 0; i < extraObjectsToHide.Length; i++)
            {
                if (extraObjectsToHide[i] != null)
                    extraObjectsToHide[i].SetActive(false);
            }
        }
    }

    void ShowFinalPopups()
    {
        if (purposePopup != null)
            purposePopup.SetActive(true);

        if (nextLevelPopup != null)
            nextLevelPopup.SetActive(true);

        if (!hasReportedLevelComplete)
        {
            hasReportedLevelComplete = true;
            GameManager.MarkLevelCompleted(2);
            GameManager.ReturnToLevelSelect();
        }

        // Popup copy can stay empty until the final text is ready.
        // Temporary test copy can be added here if needed.
        if (purposeText != null && string.IsNullOrEmpty(purposeText.text))
            purposeText.text = "";

        if (nextLevelText != null && string.IsNullOrEmpty(nextLevelText.text))
            nextLevelText.text = "";
    }

    public void ResetNormalization()
    {
        StopAllCoroutines();

        isNormalizing = false;
        hasNormalized = false;
        hasReportedLevelComplete = false;

        if (numberPanel != null)
            numberPanel.SetActive(false);

        if (purposePopup != null)
            purposePopup.SetActive(false);

        if (nextLevelPopup != null)
            nextLevelPopup.SetActive(false);

        if (normalizeButton != null)
        {
            normalizeButton.gameObject.SetActive(false);
            normalizeButton.interactable = true;
        }
    }
}
