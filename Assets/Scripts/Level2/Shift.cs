// Responsible team member: Hanyun Zhu, Zhiyu Huang; Description: Controls the Level 2 dialogue flow, symbol-slot stage, and attention input stage.
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

    [Header("Stage Roots")]
    public GameObject wordPlatformRoot;
    public GameObject attentionStageRoot;

    [Header("Attention Graph")]
    public AttentionGraph attentionGraph;

    [Header("Controllers")]
    public AttentionInputManager attentionInputManager;

    [Header("Slots")]
    public UISymbolSlot[] allSlots;

    [Header("Dialogue Content")]
    [TextArea]
    public string[] messages = new string[]
    {
        "Now, enter the self-attention connection setup phase! (click to shift)",
        "Click on any line and use the two buttons to adjust its thickness.Thicker means higher attention."
    };

    private int currentIndex = -1;
    private bool isActive = false;
    private bool hasStartedDialogue = false;
    private bool isInInputStage = false;

    void Start()
    {
        if (popupPanel != null) popupPanel.SetActive(false);
        if (messageText != null) messageText.text = "";
        if (hintText != null) hintText.text = "";

        if (attentionGraph != null)
            attentionGraph.gameObject.SetActive(false);

        if (attentionStageRoot != null)
            attentionStageRoot.SetActive(false);

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

        if (!isActive) return;

        if (!isInInputStage && Input.GetMouseButtonDown(0))
        {
            ShowNextMessage();
        }
    }

    bool CheckAllSlotsFull()
    {
        if (allSlots == null || allSlots.Length == 0) return false;

        foreach (UISymbolSlot slot in allSlots)
        {
            if (slot == null || !slot.IsFull)
                return false;
        }

        return true;
    }

    void ShowNextMessage()
    {
        if (messages == null || messages.Length == 0) return;

        currentIndex++;

        if (popupPanel != null)
            popupPanel.SetActive(true);

        if (currentIndex < messages.Length)
        {
            if (messageText != null)
                messageText.text = messages[currentIndex];

            if (hintText != null)
                hintText.text = "";

            if (attentionGraph != null)
                attentionGraph.gameObject.SetActive(false);

            if (attentionStageRoot != null)
                attentionStageRoot.SetActive(false);

            if (confirmButton != null)
                confirmButton.gameObject.SetActive(false);

            if (currentIndex == messages.Length - 1)
            {
                Invoke(nameof(EnterInputStage), 1f);
            }
        }
        else
        {
            EnterInputStage();
        }
    }

    void EnterInputStage()
    {
        isInInputStage = true;

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (hintText != null)
            hintText.text = "";

        if (wordPlatformRoot != null)
            wordPlatformRoot.SetActive(false);

        if (attentionStageRoot != null)
            attentionStageRoot.SetActive(true);

        if (attentionGraph != null)
        {
            attentionGraph.gameObject.SetActive(true);
            attentionGraph.Initialize();
        }

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);

        Debug.Log("Entered attention stage");
    }

    public void OnSubmitInput()
    {
        if (!isActive || !isInInputStage) return;

        if (attentionGraph == null)
        {
            Debug.LogError("Shift: attentionGraph is not assigned.");
            return;
        }

        float[,] weights = attentionGraph.GetAttentionWeights();

        if (attentionInputManager != null)
        {
            attentionInputManager.ValidateAttentionWeights(weights, this);
        }
        else
        {
            Debug.LogError("AttentionInputManager not assigned in Shift inspector!");
        }
    }

    public void RestartFromInputStage()
    {
        Debug.Log("RestartFromAttentionStage");

        hasStartedDialogue = true;
        isActive = true;
        isInInputStage = true;
        currentIndex = messages.Length;

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (hintText != null)
            hintText.text = "";

        if (wordPlatformRoot != null)
            wordPlatformRoot.SetActive(false);

        if (attentionStageRoot != null)
            attentionStageRoot.SetActive(true);

        if (attentionGraph != null)
        {
            attentionGraph.gameObject.SetActive(true);
            attentionGraph.ResetEdgesToDefault();
        }

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(true);
    }

    public void EndDialogue()
    {
        isActive = false;
        isInInputStage = false;

        if (confirmButton != null)
            confirmButton.gameObject.SetActive(false);

        if (popupPanel != null)
            popupPanel.SetActive(false);

        if (messageText != null)
            messageText.text = "";

        if (hintText != null)
            hintText.text = "";

        if (attentionGraph != null)
            attentionGraph.gameObject.SetActive(false);

        if (attentionStageRoot != null)
            attentionStageRoot.SetActive(false);

        Debug.Log("Dialogue finished");
    }
}
