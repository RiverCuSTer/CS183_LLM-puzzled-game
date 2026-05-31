using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using TMPro;
public class Level4Manager : MonoBehaviour
{
    [Header("Canvas")]
    public Canvas canvas;
    [Header("Token")]
    public Image tokenImage;
    public Sprite tokenNormalSprite;
    public Sprite tokenIntensifySprite;
    [Header("Expand")]
    public Button expandButton;
    [Header("Features")]
    public FeatureBlock[] features;
    [Header("Output Slots")]
    public OutputSlot[] outputSlots;
    [Header("Action Buttons")]
    public GlowButton[] actionButtons;
    [Header("State Light Sprites")]
    public Sprite saveLightSprite;
    public Sprite intensifyLightSprite;
    public Sprite refrainLightSprite;
    [Header("Vanish Animation Frames")]
    public Sprite[] vanishFrames;
    public float vanishFrameTime = 0.06f;
    [Header("Hint UI")]
    public Image hintIcon;
    public TextMeshProUGUI hintText;
    public Sprite rightSign;
    public Sprite wrongSign;
    public Sprite warningSign;
    [Header("Complete UI")]
    public GameObject completePanel;
    [Header("Backend Signal")]
    public UnityEvent onLevelComplete;
    public GameObject backendReceiver;
    public string backendMethodName = "OnLevelComplete";
    public string levelCompleteSignal = "Level4";
    private Level4Action selectedAction = Level4Action.None;
    private int phase = 1;
    private bool completed = false;
    private void Start()
    {
        InitLevel();
    }
    private void InitLevel()
    {
        completed = false;
        phase = 1;
        selectedAction = Level4Action.None;
        if (tokenImage != null && tokenNormalSprite != null)
        {
            tokenImage.sprite = tokenNormalSprite;
        }
        if (completePanel != null)

        {
            completePanel.SetActive(false);
        }
        if (features != null)
        {
            for (int i = 0; i < features.Length; i++)
            {
                if (features[i] != null)
                {
                    features[i].Init(this);
                    features[i].gameObject.SetActive(false);
                }
            }
        }
        if (outputSlots != null)
{
    for (int i = 0; i < outputSlots.Length; i++)
    {
        if (outputSlots[i] != null)
        {
            outputSlots[i].slotIndex = i + 1;
            outputSlots[i].Init(this);

            Debug.Log($"Init OutputSlot: arrayIndex={i}, name={outputSlots[i].name}, assignedSlotIndex={outputSlots[i].slotIndex}");
        }
    }
}

        if (actionButtons != null)
        {
            for (int i = 0; i < actionButtons.Length; i++)
            {
                if (actionButtons[i] != null)
                {
                    actionButtons[i].Init(this);
                    actionButtons[i].SetSelected(false);
                }
            }
        }
        ShowHint("Click EXPAND to expand the token into multiple features.", warningSign);
    Debug.Log("===== Level4 Init Check =====");

if (outputSlots != null)
{
    for (int i = 0; i < outputSlots.Length; i++)
    {
        if (outputSlots[i] == null)
        {
            Debug.LogWarning($"OutputSlots[{i}] = NULL");
        }
        else
        {
            RectTransform rt = outputSlots[i].GetRectTransform();
            Debug.Log($"OutputSlots[{i}] name={outputSlots[i].name}, slotIndex={outputSlots[i].slotIndex}, rectName={(rt != null ? rt.name : "NULL")}");
        }
    }
}

if (features != null)
{
    for (int i = 0; i < features.Length; i++)
    {
        if (features[i] == null)
        {
            Debug.LogWarning($"Features[{i}] = NULL");
        }
        else
        {
            Debug.Log($"Features[{i}] name={features[i].name}, outputOrder={features[i].outputOrder}, correctAction={features[i].correctAction}");
        }
    }
}

    }
    public void OnExpandClicked()
    {
        if (phase != 1)
        {
            return;
        }
        phase = 2;
        if (tokenImage != null && tokenIntensifySprite != null)
        {
            tokenImage.sprite = tokenIntensifySprite;
        }
        if (expandButton != null)
        {
            expandButton.interactable = false;
        }
        StartCoroutine(ShowFeaturesOneByOne());
        ShowHint("Select a feature, then click the intensify, refrain, or save button.", warningSign);
    }
    private IEnumerator ShowFeaturesOneByOne()
    {
        if (features == null)
        {
            yield break;
        }
        for (int i = 0; i < features.Length; i++)
        {
            if (features[i] != null)
            {
                features[i].gameObject.SetActive(true);
                features[i].PlayPopAnimation();
                yield return new WaitForSeconds(0.08f);
            }
        }
    }
    public void SelectAction(Level4Action action)
{
    if (phase != 2)
    {
        ShowHint("Cannot select activation status at the moment. Please click EXPAND first.", warningSign);
        return;
    }

    if (action == Level4Action.None)
    {
        selectedAction = Level4Action.None;
        ClearButtonSelection();
        ShowHint("Please select intensify, refrain, or save button.", warningSign);
        return;
    }

    if (selectedAction == action)
    {
        selectedAction = Level4Action.None;
    }
    else
    {
        selectedAction = action;
    }

    if (actionButtons != null)
    {
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] != null)
            {
                actionButtons[i].SetSelected(actionButtons[i].actionType == selectedAction);
            }
        }
    }

    if (selectedAction == Level4Action.Intensify)
    {
        ShowHint("Selected: Intensify. Click important features to make them brighter.", warningSign);
    }
    else if (selectedAction == Level4Action.Refrain)
    {
        ShowHint("Selected: Refrain. Click irrelevant features to make them darker.", warningSign);
    }
    else if (selectedAction == Level4Action.Save)
    {
        ShowHint("Selected: Save. Click neutral but useful features.", warningSign);
    }
    else
    {
        ShowHint("Selection cancelled. Please select intensify, refrain, or save button.", warningSign);
    }
}

    public void OnFeatureClicked(FeatureBlock feature)
    {
        if (feature == null)
        {
            return;
        }
        if (phase == 2)
        {
            HandleActivation(feature);
            return;
        }
        if (phase == 3)
        {
            HandleDeleteFeature(feature);
            return;
        }
    }
    private void HandleActivation(FeatureBlock feature)
    {
        if (selectedAction == Level4Action.None)
        {
            ShowHint("Please select intensify, refrain, or save button first.", warningSign);
            feature.PlayShakeAnimation();
            return;
        }
        if (feature.IsActivated())
        {
            ShowHint("This feature has already been processed.", warningSign);
            feature.PlayShakeAnimation();
            return;
        }
        if (selectedAction != feature.correctAction)
        {
            ShowHint("This activation choice is not quite right. Think again about whether this feature is important.", wrongSign);
            feature.PlayShakeAnimation();
            return;
        }
        feature.ApplyAction(selectedAction, GetLightSprite(selectedAction));
        ShowHint("Correct! Continue filtering other features.", rightSign);
        if (AllFeaturesActivated())
        {
            phase = 3;
            selectedAction = Level4Action.None;
            ClearButtonSelection();
            ShowHint("Activation complete! Click the refrained irrelevant features to make them disappear, then drag effective features to output slots.", rightSign);
        }
    }
    private void HandleDeleteFeature(FeatureBlock feature)
    {
        if (feature.IsPlaced())
        {
            return;
        }
        if (feature.GetAppliedAction() == Level4Action.Refrain)
        {
            if (!feature.IsVanished())
            {
                StartCoroutine(feature.PlayVanishAnimation(vanishFrames, vanishFrameTime));
                ShowHint("Irrelevant feature deleted. Now drag effective features to output slots.", rightSign);
            }
        }
        else
        {
            ShowHint("This feature is useful and should not be deleted. Please drag it to an output slot.", warningSign);
            feature.PlayShakeAnimation();
        }
    }
    public bool TryPlaceFeatureInSlot(FeatureBlock feature, OutputSlot slot)
    {
        if (phase != 3)
        {
            return false;
        }
        if (feature == null || slot == null)
        {
            return false;
        }
        if (feature.IsVanished())
        {
            return false;
        }
        if (feature.GetAppliedAction() == Level4Action.Refrain)
        {
            ShowHint("Refrained irrelevant features do not need to be placed in output slots.", warningSign);
            return false;
        }
        if (slot.IsOccupied())
        {
            ShowHint("This slot is already occupied.", warningSign);
            return false;
        }
        Debug.Log($"Try place: feature={feature.name}, outputOrder={feature.outputOrder}, slot={slot.name}, slotIndex={slot.slotIndex}");

        if (feature.outputOrder != slot.slotIndex)
        {
            ShowHint("Order is wrong. FFN must compress output in the correct order at the end.", wrongSign);
            feature.PlayShakeAnimation();
            return false;
        }
        slot.PlaceFeature(feature);
        feature.SetPlaced(true);
        ShowHint("Correct placement!", rightSign);
        if (AllRequiredFeaturesPlaced())
        {
            CompleteLevel();
        }
        return true;
    }
    public bool IsPointerOverAnySlot(Vector2 screenPosition, out OutputSlot resultSlot)
    {
        resultSlot = null;
        if (outputSlots == null)
        {
            return false;
        }
        Camera uiCamera = null;
        if (canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
        {
            uiCamera = canvas.worldCamera;
        }
        for (int i = 0; i < outputSlots.Length; i++)
        {
            if (outputSlots[i] == null)
            {
                continue;
            }
            RectTransform rect = outputSlots[i].GetRectTransform();
            if (RectTransformUtility.RectangleContainsScreenPoint(rect, screenPosition, uiCamera))
            {
                resultSlot = outputSlots[i];
                return true;
            }
        }
        return false;
    }
    private Sprite GetLightSprite(Level4Action action)
    {
        if (action == Level4Action.Intensify)
        {
            return intensifyLightSprite;
        }
        if (action == Level4Action.Refrain)
        {
            return refrainLightSprite;
        }
        if (action == Level4Action.Save)
        {
            return saveLightSprite;
        }
        return null;
    }
    private bool AllFeaturesActivated()
    {
        if (features == null)
        {
            return false;
        }
        for (int i = 0; i < features.Length; i++)
        {
            if (features[i] != null && !features[i].IsActivated())
            {
                return false;
            }
        }
        return true;
    }
    private bool AllRequiredFeaturesPlaced()
    {
        if (features == null)
        {
            return false;
        }
        for (int i = 0; i < features.Length; i++)
        {
            if (features[i] == null)
            {
                continue;
            }
            if (features[i].outputOrder > 0 && !features[i].IsPlaced())
            {
                return false;
            }
        }
        return true;
    }
    private void ClearButtonSelection()
    {
        if (actionButtons == null)
        {
            return;
        }
        for (int i = 0; i < actionButtons.Length; i++)
        {
            if (actionButtons[i] != null)
            {
                actionButtons[i].SetSelected(false);
            }
        }
    }
    private void CompleteLevel()
    {
        if (completed)
        {
            return;
        }
        completed = true;
        phase = 4;
        if (tokenImage != null && tokenIntensifySprite != null)
        {
            tokenImage.sprite = tokenIntensifySprite;
        }
        if (completePanel != null)
        {
            completePanel.SetActive(true);
        }
        ShowHint("FFN Complete: Features have been expanded, activated, compressed, and output token generated!", rightSign);
        PlayerPrefs.SetInt("Level4_Completed", 1);
        PlayerPrefs.Save();
        Debug.Log("LEVEL_COMPLETE:" + levelCompleteSignal);
        if (backendReceiver != null && !string.IsNullOrEmpty(backendMethodName))
        {
            backendReceiver.SendMessage(backendMethodName, levelCompleteSignal, SendMessageOptions.DontRequireReceiver);
        }
        if (onLevelComplete != null)
        {
            onLevelComplete.Invoke();
        }
    }
    private void ShowHint(string message, Sprite icon)
    {
        if (hintText != null)
        {
            hintText.text = message;
        }
        if (hintIcon != null && icon != null)
        {
            hintIcon.sprite = icon;
        }
    }
}
