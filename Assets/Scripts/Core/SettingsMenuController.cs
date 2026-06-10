// Responsible team member: Zhiyan Lin; Description: Controls the settings menu and reset-progress confirmation flow.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenuController : MonoBehaviour
{
    [Header("Fixed Scene Objects")]
    [SerializeField] private TMP_Text messageText;
    [SerializeField] private Button resetProgressButton;
    [SerializeField] private Button cancelResetButton;

    private bool isConfirmingReset;
    private bool isInitialized;

    private void Awake()
    {
        ResolveFixedReferences();
        BindFixedButtons();
        isInitialized = true;
        ShowMainSettings();
    }

    private void OnEnable()
    {
        if (!isInitialized)
            return;

        ShowMainSettings();
    }

    private void ResolveFixedReferences()
    {
        if (messageText == null)
        {
            messageText = transform.Find("VideoSettings")?.GetComponent<TMP_Text>();
        }

        if (resetProgressButton == null)
        {
            resetProgressButton = transform.Find("ResetButton")?.GetComponent<Button>();
        }

        if (cancelResetButton == null)
        {
            cancelResetButton = transform.Find("CancelButton")?.GetComponent<Button>();
        }
    }

    private void BindFixedButtons()
    {
        if (resetProgressButton != null)
        {
            resetProgressButton.onClick.RemoveAllListeners();
            resetProgressButton.onClick.AddListener(HandleResetButton);
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.onClick.RemoveAllListeners();
            cancelResetButton.onClick.AddListener(CancelResetConfirmation);
        }
    }

    private void ShowMainSettings()
    {
        isConfirmingReset = false;

        if (messageText != null)
        {
            Image messageImage = messageText.GetComponent<Image>();
            if (messageImage != null)
            {
                messageImage.enabled = false;
                messageImage.raycastTarget = false;
            }

            messageText.gameObject.SetActive(false);
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.gameObject.SetActive(false);
            SetButtonLabel(cancelResetButton, "Cancel");
        }

        if (resetProgressButton != null)
        {
            resetProgressButton.gameObject.SetActive(true);
            resetProgressButton.interactable = true;
            SetButtonLayout(resetProgressButton, Vector2.zero, new Vector2(260f, 56f));
            SetButtonLabel(resetProgressButton, "Reset Progress");
        }
    }

    private void ShowResetConfirmation()
    {
        isConfirmingReset = true;

        if (messageText != null)
        {
            messageText.gameObject.SetActive(true);
            messageText.text = "Reset all game progress?";
            messageText.enableWordWrapping = false;
            messageText.alignment = TextAlignmentOptions.Center;
            messageText.color = new Color(0.08f, 0.08f, 0.08f, 1f);
            messageText.fontSize = 28f;

            RectTransform rect = messageText.transform as RectTransform;
            if (rect != null)
            {
                rect.anchorMin = new Vector2(0.5f, 0.5f);
                rect.anchorMax = new Vector2(0.5f, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = new Vector2(0f, 70f);
                rect.sizeDelta = new Vector2(520f, 60f);
            }
        }

        if (resetProgressButton != null)
        {
            SetButtonLayout(resetProgressButton, new Vector2(-130f, -45f), new Vector2(200f, 52f));
            SetButtonLabel(resetProgressButton, "Confirm");
        }

        if (cancelResetButton != null)
        {
            cancelResetButton.gameObject.SetActive(true);
            cancelResetButton.interactable = true;
            SetButtonLayout(cancelResetButton, new Vector2(130f, -45f), new Vector2(200f, 52f));
            SetButtonLabel(cancelResetButton, "Cancel");
        }
    }

    private void HandleResetButton()
    {
        if (isConfirmingReset)
        {
            ConfirmResetProgress();
            return;
        }

        ShowResetConfirmation();
    }

    private void CancelResetConfirmation()
    {
        if (isConfirmingReset)
        {
            ShowMainSettings();
        }
    }

    private void ConfirmResetProgress()
    {
        for (int levelNumber = 1; levelNumber <= 4; levelNumber++)
        {
            PlayerPrefs.DeleteKey($"Level{levelNumber}_Completed");
        }

        PlayerPrefs.Save();
        UIManager.Instance?.RefreshLevelSelectLocks();
        ShowMainSettings();
    }

    private void SetButtonLayout(Button button, Vector2 anchoredPosition, Vector2 size)
    {
        RectTransform rect = button.transform as RectTransform;
        if (rect == null)
            return;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = anchoredPosition;
        rect.sizeDelta = size;
    }

    private void SetButtonLabel(Button button, string label)
    {
        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text == null)
            return;

        text.gameObject.SetActive(true);
        text.text = label;
        text.enableWordWrapping = false;
        text.alignment = TextAlignmentOptions.Center;
    }

}
