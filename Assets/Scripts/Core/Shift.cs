using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Shift : MonoBehaviour
{
    [Header("UI References")]
    public GameObject popupPanel;
    public TextMeshProUGUI messageText;
    public TextMeshProUGUI hintText;
    public Button confirmButton;
    public AttentionBalanceController balanceController;
    public AttentionInputManager attentionInputManager;


    [Header("All Number Inputs")]
    public TMP_InputField[] numberInputs;

    [Header("Input Labels")]
    public string[] inputLabelTexts = new string[]
    {
        "who->who", "who->am", "who->i",
        "am->who", "am->am", "am->i",
        "i->who", "i->am", "i->i"
    };

    [Header("Slots")]
    public UISymbolSlot[] allSlots;

    [Header("Dialogue Content")]
    [TextArea]
    public string[] messages = new string[]
    {
        "Now, enter the self-attention connection setup phase!(click to shift)",
        "Please fill in all 9 values between 0.0 and 1.0(one column one word)"
       
    };

    private int currentIndex = -1;
    private bool isActive = false;
    private bool hasStartedDialogue = false;
    private bool isInInputStage = false;
    private TextMeshProUGUI[] inputLabels;
    private int selectedInputIndex = -1;

    void Start()
    {
        EnsureInputLabels();

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (messageText != null)
            messageText.text = "";

        if (hintText != null)
            hintText.text = "";

        HideAllInputs();

        if (confirmButton != null)
        {
            confirmButton.gameObject.SetActive(false);
            confirmButton.onClick.RemoveAllListeners();
            confirmButton.onClick.AddListener(OnSubmitInput);
        }
    }

    void Update()
    {
        if (!hasStartedDialogue)
        {
            if (CheckAllSlotsFull())
            {
                hasStartedDialogue = true;
                isActive = true;
                currentIndex = -1;
                ShowNextMessage();
            }
        }

        if (!isActive)
            return;

        if (isInInputStage)
        {
            HandleManualInputStage();
            return;
        }

        if (!isInInputStage && Input.GetMouseButtonDown(0))
        {
            ShowNextMessage();
        }
    }

    bool CheckAllSlotsFull()
    {
        if (allSlots == null || allSlots.Length == 0)
        {
            Debug.LogWarning("Shift: allSlots not set");
            return false;
        }

        foreach (UISymbolSlot slot in allSlots)
        {
            if (slot == null)
            {
                Debug.LogWarning("Shift: one slot reference is missing");
                return false;
            }

            if (!slot.IsFull)
                return false;
        }

        return true;
    }

    void ShowNextMessage()
    {
        if (messages == null || messages.Length == 0)
        {
            Debug.LogWarning("Shift: messages is empty");
            return;
        }

        currentIndex++;

        if (popupPanel != null)
            popupPanel.SetActive(true);

        
        if (currentIndex < messages.Length)
        {
            // 显示当前消息
            if (messageText != null)
                messageText.text = messages[currentIndex];

            if (hintText != null)
                hintText.text = "";

            HideAllInputs();

            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);

            Debug.Log("Show message index: " + currentIndex);

            if (currentIndex == messages.Length - 1)
            {
                //延迟一下再进入输入阶段，让用户看到最后一条消息
                Invoke("EnterInputStage", 1f); // 延迟1秒后进入
                // 或者立即进入：EnterInputStage();
            }
        }
        else
        {
            // 所有消息都显示完了，进入输入阶段
            EnterInputStage();
        }
    }

    void EnterInputStage()
    {
        isInInputStage = true;
        selectedInputIndex = -1;

        if (popupPanel != null)
            popupPanel.SetActive(false);  // 隐藏弹窗

        if (hintText != null)
            hintText.text = "";

        ShowAllInputs();
        PrepareInputStageRaycasts();

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);

        Debug.Log("Entered input stage - popup hidden");
    }

    void ShowAllInputs()
    {
        if (numberInputs == null || numberInputs.Length == 0)
        {
            Debug.LogWarning("Shift: numberInputs not set");
            return;
        }

        for (int i = 0; i < numberInputs.Length; i++)
        {
            if (numberInputs[i] == null)
                continue;

            numberInputs[i].gameObject.SetActive(true);
            numberInputs[i].text = "";
            SetupSingleInputAppearance(numberInputs[i]);
            SetupSingleInputInteraction(numberInputs[i]);
            SetInputLabelVisible(i, true);
        }
    }

    void HideAllInputs()
    {
        if (numberInputs == null || numberInputs.Length == 0)
            return;

        for (int i = 0; i < numberInputs.Length; i++)
        {
            if (numberInputs[i] == null)
                continue;

            numberInputs[i].text = "";
            numberInputs[i].gameObject.SetActive(false);
            SetInputLabelVisible(i, false);
        }

        selectedInputIndex = -1;
    }

    void SetupSingleInputAppearance(TMP_InputField input)
    {
        if (input == null)
            return;

        if (input.textComponent != null)
        {
            input.textComponent.text = "";
            input.textComponent.fontSize = 10;
            input.textComponent.color = Color.black;
            input.textComponent.faceColor = Color.black;
            input.textComponent.alignment = TextAlignmentOptions.Center;
            input.textComponent.enableWordWrapping = false;
            input.textComponent.overflowMode = TextOverflowModes.Overflow;
            input.textComponent.rectTransform.localScale = Vector3.one;
        }

        if (input.placeholder != null)
        {
            TextMeshProUGUI placeholderTMP = input.placeholder as TextMeshProUGUI;
            if (placeholderTMP != null)
            {
                placeholderTMP.text = "";
                placeholderTMP.fontSize = 10;
                placeholderTMP.color = Color.black;
                placeholderTMP.faceColor = Color.black;
                placeholderTMP.enableWordWrapping = false;
                placeholderTMP.overflowMode = TextOverflowModes.Overflow;
                placeholderTMP.rectTransform.localScale = Vector3.one;
            }
        }

        RectTransform rt = input.GetComponent<RectTransform>();
        if (rt != null)
            rt.localScale = Vector3.one;
    }

    void HandleManualInputStage()
    {
        if (Input.GetMouseButtonDown(0))
            HandleManualInputClick(Input.mousePosition);

        if (selectedInputIndex < 0)
            return;

        TMP_InputField selectedInput = GetSelectedInput();
        if (selectedInput == null)
        {
            selectedInputIndex = -1;
            return;
        }

        foreach (char inputChar in Input.inputString)
            ApplyManualInputCharacter(selectedInput, inputChar);
    }

    void HandleManualInputClick(Vector2 screenPosition)
    {
        if (IsConfirmButtonHit(screenPosition))
        {
            OnSubmitInput();
            return;
        }

        int hitIndex = GetInputIndexAtScreenPosition(screenPosition);
        if (hitIndex < 0)
            return;

        selectedInputIndex = hitIndex;
        TMP_InputField selectedInput = numberInputs[selectedInputIndex];
        selectedInput.Select();
        selectedInput.ActivateInputField();
        MoveManualCaretToEnd(selectedInput);
    }

    int GetInputIndexAtScreenPosition(Vector2 screenPosition)
    {
        if (numberInputs == null)
            return -1;

        for (int i = numberInputs.Length - 1; i >= 0; i--)
        {
            TMP_InputField input = numberInputs[i];
            if (input == null || !input.gameObject.activeInHierarchy)
                continue;

            RectTransform inputRect = input.GetComponent<RectTransform>();
            if (inputRect == null)
                continue;

            if (RectTransformUtility.RectangleContainsScreenPoint(inputRect, screenPosition, GetInputCanvasCamera(inputRect)))
                return i;
        }

        return -1;
    }

    bool IsConfirmButtonHit(Vector2 screenPosition)
    {
        if (confirmButton == null || !confirmButton.gameObject.activeInHierarchy)
            return false;

        RectTransform confirmRect = confirmButton.GetComponent<RectTransform>();
        return confirmRect != null && RectTransformUtility.RectangleContainsScreenPoint(confirmRect, screenPosition, GetInputCanvasCamera(confirmRect));
    }

    Camera GetInputCanvasCamera(RectTransform rectTransform)
    {
        if (rectTransform == null)
            return null;

        Canvas canvas = rectTransform.GetComponentInParent<Canvas>();
        if (canvas == null || canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            return null;

        return canvas.worldCamera;
    }

    TMP_InputField GetSelectedInput()
    {
        if (numberInputs == null || selectedInputIndex < 0 || selectedInputIndex >= numberInputs.Length)
            return null;

        return numberInputs[selectedInputIndex];
    }

    void ApplyManualInputCharacter(TMP_InputField input, char inputChar)
    {
        if (input == null)
            return;

        if (inputChar == '\b')
        {
            if (!string.IsNullOrEmpty(input.text))
                input.text = input.text.Substring(0, input.text.Length - 1);

            MoveManualCaretToEnd(input);
            return;
        }

        if (inputChar == '\n' || inputChar == '\r')
        {
            OnSubmitInput();
            return;
        }

        if (inputChar == '\t')
        {
            SelectNextManualInput();
            return;
        }

        if (!IsAllowedInputCharacter(input, inputChar))
            return;

        input.text += inputChar;
        MoveManualCaretToEnd(input);
    }

    bool IsAllowedInputCharacter(TMP_InputField input, char inputChar)
    {
        if (char.IsDigit(inputChar))
            return true;

        if (inputChar == '.' && input != null && !input.text.Contains("."))
            return true;

        return false;
    }

    void SelectNextManualInput()
    {
        if (numberInputs == null || numberInputs.Length == 0)
            return;

        selectedInputIndex++;
        if (selectedInputIndex >= numberInputs.Length)
            selectedInputIndex = 0;

        TMP_InputField selectedInput = GetSelectedInput();
        if (selectedInput == null)
            return;

        selectedInput.Select();
        selectedInput.ActivateInputField();
        MoveManualCaretToEnd(selectedInput);
    }

    void MoveManualCaretToEnd(TMP_InputField input)
    {
        if (input == null)
            return;

        input.caretPosition = input.text.Length;
        input.selectionAnchorPosition = input.text.Length;
        input.selectionFocusPosition = input.text.Length;
    }

    void SetupSingleInputInteraction(TMP_InputField input)
    {
        if (input == null)
            return;

        input.interactable = true;
        input.readOnly = true;

        Graphic targetGraphic = input.targetGraphic;
        if (targetGraphic != null)
            targetGraphic.raycastTarget = true;

        Graphic[] childGraphics = input.GetComponentsInChildren<Graphic>(true);
        foreach (Graphic graphic in childGraphics)
        {
            if (graphic == null || graphic == targetGraphic)
                continue;

            graphic.raycastTarget = false;
        }
    }

    void PrepareInputStageRaycasts()
    {
        if (numberInputs == null || numberInputs.Length == 0)
            return;

        Transform inputPanel = null;
        foreach (TMP_InputField input in numberInputs)
        {
            if (input != null && input.transform.parent != null)
            {
                inputPanel = input.transform.parent;
                break;
            }
        }

        if (inputPanel != null)
        {
            Graphic panelGraphic = inputPanel.GetComponent<Graphic>();
            if (panelGraphic != null)
                panelGraphic.raycastTarget = false;
        }

        for (int i = 0; i < numberInputs.Length; i++)
        {
            SetupSingleInputInteraction(numberInputs[i]);
            SetInputLabelRaycast(i, false);
        }

        if (confirmButton != null)
        {
            Graphic confirmGraphic = confirmButton.targetGraphic;
            if (confirmGraphic != null)
                confirmGraphic.raycastTarget = true;
        }
    }

    void EnsureInputLabels()
    {
        if (numberInputs == null || numberInputs.Length == 0)
            return;

        inputLabels = new TextMeshProUGUI[numberInputs.Length];

        for (int i = 0; i < numberInputs.Length; i++)
        {
            TMP_InputField input = numberInputs[i];
            if (input == null)
                continue;

            Transform labelParent = input.transform.parent != null ? input.transform.parent : input.transform;
            Transform existing = labelParent.Find("AttentionLabel_" + i);
            TextMeshProUGUI label = existing != null ? existing.GetComponent<TextMeshProUGUI>() : null;

            if (label == null)
            {
                GameObject labelObject = new GameObject("AttentionLabel_" + i);
                labelObject.transform.SetParent(labelParent, false);
                label = labelObject.AddComponent<TextMeshProUGUI>();
            }
            CanvasGroup labelCanvasGroup = label.GetComponent<CanvasGroup>();
            if (labelCanvasGroup == null)
                labelCanvasGroup = label.gameObject.AddComponent<CanvasGroup>();
            labelCanvasGroup.blocksRaycasts = false;
            labelCanvasGroup.interactable = false;

            RectTransform inputRect = input.GetComponent<RectTransform>();
            RectTransform labelRect = label.GetComponent<RectTransform>();
            if (inputRect != null && labelRect != null)
            {
                labelRect.anchorMin = inputRect.anchorMin;
                labelRect.anchorMax = inputRect.anchorMax;
                labelRect.pivot = inputRect.pivot;
                labelRect.anchoredPosition = inputRect.anchoredPosition + new Vector2(0f, -GetLabelOffset(inputRect));
                labelRect.sizeDelta = new Vector2(100f, 18f);
                labelRect.localScale = Vector3.one;
            }

            label.text = GetInputLabelText(i);
            label.fontSize = 10;
            label.color = Color.white;
            label.faceColor = Color.white;
            label.alignment = TextAlignmentOptions.Center;
            label.enableWordWrapping = false;
            label.overflowMode = TextOverflowModes.Overflow;
            label.raycastTarget = false;

            inputLabels[i] = label;
            SetInputLabelVisible(i, false);
            SetInputLabelRaycast(i, false);
        }
    }

    float GetLabelOffset(RectTransform inputRect)
    {
        if (inputRect == null)
            return 28f;

        return inputRect.rect.height * 0.5f + 12f;
    }

    string GetInputLabelText(int index)
    {
        if (inputLabelTexts != null && index >= 0 && index < inputLabelTexts.Length && !string.IsNullOrEmpty(inputLabelTexts[index]))
            return inputLabelTexts[index];

        return "";
    }

    void SetInputLabelVisible(int index, bool visible)
    {
        if (inputLabels == null || index < 0 || index >= inputLabels.Length || inputLabels[index] == null)
            return;

        inputLabels[index].gameObject.SetActive(visible);
    }

    void SetInputLabelRaycast(int index, bool raycastTarget)
    {
        if (inputLabels == null || index < 0 || index >= inputLabels.Length || inputLabels[index] == null)
            return;

        inputLabels[index].raycastTarget = raycastTarget;

        CanvasGroup labelCanvasGroup = inputLabels[index].GetComponent<CanvasGroup>();
        if (labelCanvasGroup != null)
        {
            labelCanvasGroup.blocksRaycasts = raycastTarget;
            labelCanvasGroup.interactable = raycastTarget;
        }
    }

    public void OnSubmitInput()
    {
        Debug.Log("Confirm button clicked");

        if (!isActive || !isInInputStage)
        {
            Debug.Log("Submit ignored: not in input stage");
            return;
        }
        if (AreAllInputsValid())
        {
            if (hintText != null)
                hintText.text = "";

            Debug.Log("All inputs are valid");

            if (attentionInputManager != null)
                attentionInputManager.ProcessConfirmedInputs(numberInputs);
            else
                Debug.LogWarning("Shift: attentionInputManager is not assigned.");

            EndDialogue();
        }

        else
        {
            if (hintText != null)
                hintText.text = "Please fill in all 9 boxes with values between 0.0 and 1.0.";

            Debug.Log("Input validation failed");
        }
    }

    bool AreAllInputsValid()
    {
        if (numberInputs == null || numberInputs.Length == 0)
        {
            Debug.LogWarning("Shift: numberInputs not set");
            return false;
        }

        for (int i = 0; i < numberInputs.Length; i++)
        {
            TMP_InputField input = numberInputs[i];

            if (input == null)
            {
                Debug.LogWarning("Shift: numberInputs[" + i + "] is null");
                return false;
            }

            string valueText = input.text.Trim();
            Debug.Log("Input " + i + " = [" + valueText + "]");

            if (string.IsNullOrEmpty(valueText))
            {
                Debug.Log("Input " + i + " is empty");
                return false;
            }

            float value;
            if (!float.TryParse(valueText, out value))
            {
                Debug.Log("Input " + i + " is not a valid number");
                return false;
            }

            if (value < 0f || value > 1f)
            {
                Debug.Log("Input " + i + " is out of range: " + value);
                return false;
            }
        }

        return true;
    }

    public void RestartFromInputStage()
    {
        Debug.Log("RestartFromInputStage called");

        hasStartedDialogue = true;
        isActive = true;
        isInInputStage = true;
        currentIndex = messages.Length;  // 跳过消息，直接输入阶段

        // 输入阶段隐藏弹窗
        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (hintText != null)
            hintText.text = "";

        ShowAllInputs();
        PrepareInputStageRaycasts();

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
    }

    void EndDialogue()
    {
        isActive = false;
        isInInputStage = false;

        HideAllInputs();

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (messageText != null)
            messageText.text = "";

        if (hintText != null)
            hintText.text = "";

        Debug.Log("Dialogue finished successfully");
    }
}
