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

    void Start()
    {
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

        if (popupPanel != null)
            popupPanel.SetActive(false);  // 隐藏弹窗

        if (hintText != null)
            hintText.text = "";

        ShowAllInputs();

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
        }
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
                placeholderTMP.enableWordWrapping = false;
                placeholderTMP.overflowMode = TextOverflowModes.Overflow;
                placeholderTMP.rectTransform.localScale = Vector3.one;
            }
        }

        RectTransform rt = input.GetComponent<RectTransform>();
        if (rt != null)
            rt.localScale = Vector3.one;
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