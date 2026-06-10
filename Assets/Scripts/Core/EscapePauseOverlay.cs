// Responsible team member: Zhiyan Lin; Description: Handles the in-level pause overlay, restart/main-menu actions, and knowledge popup navigation.
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class EscapePauseOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button knowledgeButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainScene";
    [TextArea(8, 30)]
    [SerializeField] private string level1Knowledge = "";
    [TextArea(8, 30)]
    [SerializeField] private string level2Knowledge = "";
    [TextArea(8, 30)]
    [SerializeField] private string level3Knowledge = "";
    [TextArea(8, 30)]
    [SerializeField] private string level4Knowledge = "";

    private bool isOpen;
    private GameObject knowledgeRoot;
    private TMP_Text knowledgeTitleText;
    private TMP_Text knowledgeBodyText;
    private RectTransform knowledgeBodyRect;
    private float previousTimeScale = 1f;

    private const float KnowledgeBodyFontSize = 16.8f;
    private const float KnowledgeBodyViewportHeight = 680f;
    private const float KnowledgeScrollStep = 60f;
    private float knowledgeScrollOffset;
    private float knowledgeMaxScrollOffset;

    private void Awake()
    {
        EnsureKnowledgeButton();
        LayoutPauseButtons();
        EnsureKnowledgeOverlay();

        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Hide);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartScene);
        }

        if (knowledgeButton != null)
        {
            knowledgeButton.onClick.AddListener(ShowKnowledge);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        SetOverlayVisible(false);
        SetKnowledgeVisible(false);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (isOpen)
            {
                Hide();
            }
            else
            {
                Show();
            }
        }

        if (knowledgeRoot != null && knowledgeRoot.activeSelf)
        {
            float scrollDelta = Input.mouseScrollDelta.y;
            if (!Mathf.Approximately(scrollDelta, 0f))
            {
                ScrollKnowledge(-scrollDelta * KnowledgeScrollStep);
            }
        }
    }

    public void Show()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetOverlayVisible(true);
        SetKnowledgeVisible(false);
    }

    public void Hide()
    {
        Time.timeScale = previousTimeScale;
        SetOverlayVisible(false);
        SetKnowledgeVisible(false);
    }

    public void RestartScene()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SetOverlayVisible(bool visible)
    {
        isOpen = visible;

        if (overlayRoot != null)
        {
            overlayRoot.SetActive(visible);
        }
    }

    private void ShowKnowledge()
    {
        int levelNumber = GetCurrentLevelNumber();
        string title = $"Level {levelNumber}: Knowledge";
        string body = GetKnowledgeText(levelNumber);

        if (knowledgeTitleText != null)
        {
            knowledgeTitleText.text = title;
        }

        if (knowledgeBodyText != null)
        {
            knowledgeBodyText.text = body;
            knowledgeBodyText.color = Color.white;
            ResizeKnowledgeContent();
            knowledgeBodyText.ForceMeshUpdate();
        }

        SetOverlayVisible(false);
        SetKnowledgeVisible(true);
    }

    private void ReturnFromKnowledgeToLevel()
    {
        Time.timeScale = previousTimeScale;
        SetKnowledgeVisible(false);
        SetOverlayVisible(false);
    }

    private void SetKnowledgeVisible(bool visible)
    {
        if (knowledgeRoot != null)
        {
            knowledgeRoot.SetActive(visible);
        }
    }

    private void EnsureKnowledgeButton()
    {
        if (knowledgeButton != null || restartButton == null || overlayRoot == null)
        {
            return;
        }

        GameObject buttonObject = Instantiate(restartButton.gameObject, overlayRoot.transform);
        buttonObject.name = "KnowledgeButton";
        knowledgeButton = buttonObject.GetComponent<Button>();
        knowledgeButton.onClick.RemoveAllListeners();
        SetButtonLabel(knowledgeButton, "Knowledge");
    }

    private void LayoutPauseButtons()
    {
        SetButtonY(resumeButton, 150f);
        SetButtonY(restartButton, 50f);
        SetButtonY(knowledgeButton, -50f);
        SetButtonY(mainMenuButton, -150f);

        if (knowledgeButton != null)
        {
            knowledgeButton.transform.SetSiblingIndex(mainMenuButton != null ? mainMenuButton.transform.GetSiblingIndex() : knowledgeButton.transform.GetSiblingIndex());
        }
    }

    private void EnsureKnowledgeOverlay()
    {
        if (knowledgeRoot != null)
        {
            return;
        }

        Transform canvasTransform = transform;
        TMP_Text templateText = restartButton != null ? restartButton.GetComponentInChildren<TMP_Text>(true) : null;
        Button templateButton = restartButton != null ? restartButton : resumeButton;

        knowledgeRoot = CreateRectObject("KnowledgeOverlay", canvasTransform, true);
        Image background = knowledgeRoot.AddComponent<Image>();
        background.color = new Color(0f, 0f, 0f, 0.92f);

        GameObject titleObject = CreateRectObject("KnowledgeTitle", knowledgeRoot.transform, false);
        RectTransform titleRect = titleObject.GetComponent<RectTransform>();
        titleRect.anchorMin = new Vector2(0.5f, 1f);
        titleRect.anchorMax = new Vector2(0.5f, 1f);
        titleRect.anchoredPosition = new Vector2(0f, -88f);
        titleRect.sizeDelta = new Vector2(1120f, 72f);
        knowledgeTitleText = titleObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(templateText, knowledgeTitleText, 30f, FontStyles.Bold);
        knowledgeTitleText.alignment = TextAlignmentOptions.Center;

        GameObject bodyObject = CreateRectObject("KnowledgeBody", knowledgeRoot.transform, false);
        knowledgeBodyRect = bodyObject.GetComponent<RectTransform>();
        knowledgeBodyRect.anchorMin = new Vector2(0.5f, 0.5f);
        knowledgeBodyRect.anchorMax = new Vector2(0.5f, 0.5f);
        knowledgeBodyRect.pivot = new Vector2(0.5f, 1f);
        knowledgeBodyRect.anchoredPosition = new Vector2(0f, 320f);
        knowledgeBodyRect.sizeDelta = new Vector2(1200f, KnowledgeBodyViewportHeight);
        knowledgeBodyText = bodyObject.AddComponent<TextMeshProUGUI>();
        CopyTextStyle(templateText, knowledgeBodyText, KnowledgeBodyFontSize, FontStyles.Normal);
        knowledgeBodyText.alignment = TextAlignmentOptions.TopLeft;
        knowledgeBodyText.enableWordWrapping = true;
        knowledgeBodyText.overflowMode = TextOverflowModes.Overflow;

        Button backButton = CreateKnowledgeBackButton(templateButton);
        backButton.onClick.AddListener(ReturnFromKnowledgeToLevel);
    }

    private Button CreateKnowledgeBackButton(Button templateButton)
    {
        GameObject buttonObject;
        if (templateButton != null)
        {
            buttonObject = Instantiate(templateButton.gameObject, knowledgeRoot.transform);
        }
        else
        {
            buttonObject = CreateRectObject("BackButton", knowledgeRoot.transform, false);
            buttonObject.AddComponent<Image>().color = new Color(0f, 0f, 0f, 0.95f);
            buttonObject.AddComponent<Button>();
        }

        buttonObject.name = "KnowledgeBackButton";
        Button button = buttonObject.GetComponent<Button>();
        button.onClick.RemoveAllListeners();
        SetButtonLabel(button, "Back");

        RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
        rectTransform.anchorMin = new Vector2(0.5f, 0f);
        rectTransform.anchorMax = new Vector2(0.5f, 0f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = new Vector2(0f, 76f);
        rectTransform.sizeDelta = new Vector2(360f, 72f);
        rectTransform.SetAsLastSibling();

        return button;
    }

    private void ResizeKnowledgeContent()
    {
        if (knowledgeBodyText == null || knowledgeBodyRect == null)
        {
            return;
        }

        knowledgeBodyText.fontSize = KnowledgeBodyFontSize;
        knowledgeBodyText.ForceMeshUpdate();

        knowledgeMaxScrollOffset = Mathf.Max(0f, knowledgeBodyText.preferredHeight - KnowledgeBodyViewportHeight);
        knowledgeScrollOffset = 0f;
        ApplyKnowledgeScrollOffset();
    }

    private void ScrollKnowledge(float delta)
    {
        knowledgeScrollOffset = Mathf.Clamp(knowledgeScrollOffset + delta, 0f, knowledgeMaxScrollOffset);
        ApplyKnowledgeScrollOffset();
    }

    private void ApplyKnowledgeScrollOffset()
    {
        if (knowledgeBodyRect != null)
        {
            knowledgeBodyRect.anchoredPosition = new Vector2(0f, 320f + knowledgeScrollOffset);
        }
    }

    private static GameObject CreateRectObject(string name, Transform parent, bool stretchToParent)
    {
        GameObject gameObject = new GameObject(name, typeof(RectTransform));
        gameObject.transform.SetParent(parent, false);

        RectTransform rectTransform = gameObject.GetComponent<RectTransform>();
        if (stretchToParent)
        {
            rectTransform.anchorMin = Vector2.zero;
            rectTransform.anchorMax = Vector2.one;
            rectTransform.offsetMin = Vector2.zero;
            rectTransform.offsetMax = Vector2.zero;
        }

        return gameObject;
    }

    private static void SetButtonY(Button button, float y)
    {
        if (button == null)
        {
            return;
        }

        RectTransform rectTransform = button.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.anchoredPosition = new Vector2(rectTransform.anchoredPosition.x, y);
        }
    }

    private static void SetButtonLabel(Button button, string label)
    {
        if (button == null)
        {
            return;
        }

        TMP_Text text = button.GetComponentInChildren<TMP_Text>(true);
        if (text != null)
        {
            text.text = label;
        }
    }

    private static void CopyTextStyle(TMP_Text source, TMP_Text target, float fontSize, FontStyles fontStyle)
    {
        if (source != null)
        {
            target.font = source.font;
            target.fontSharedMaterial = source.fontSharedMaterial;
            target.color = source.color;
        }
        else
        {
            target.color = Color.white;
        }

        target.fontSize = fontSize;
        target.fontStyle = fontStyle;
    }

    private static int GetCurrentLevelNumber()
    {
        string sceneName = SceneManager.GetActiveScene().name;
        if (sceneName.StartsWith("Level") && int.TryParse(sceneName.Substring(5), out int levelNumber))
        {
            return Mathf.Clamp(levelNumber, 1, 4);
        }

        return 1;
    }

    private string GetKnowledgeText(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                return string.IsNullOrWhiteSpace(level1Knowledge) ? GetDefaultKnowledgeText(1) : level1Knowledge;
            case 2:
                return string.IsNullOrWhiteSpace(level2Knowledge) ? GetDefaultKnowledgeText(2) : level2Knowledge;
            case 3:
                return string.IsNullOrWhiteSpace(level3Knowledge) ? GetDefaultKnowledgeText(3) : level3Knowledge;
            case 4:
                return string.IsNullOrWhiteSpace(level4Knowledge) ? GetDefaultKnowledgeText(4) : level4Knowledge;
            default:
                return "";
        }
    }

    private static string GetDefaultKnowledgeText(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                return "Level 1: Text Digitization\n\nA language model cannot read words directly. It first turns text into data.\n\nTokenization splits a sentence into smaller units called tokens. A token can be a word, a character, or part of a word. In this level, each word is treated as one token.\n\nAfter tokenization, each token is matched with a number from a vocabulary. This number is called a token ID. The sentence then becomes an ordered list of IDs, such as [7421, 464, 3152, 1801].\n\nToken IDs are useful for storage, but they do not carry meaning by themselves. The model therefore converts IDs into embeddings. An embedding is a vector that places a token in a semantic space.\n\nTokens with related meanings or roles often appear closer together in this space. This lets the model use math to compare words, find relationships, and prepare the sentence for later neural network layers.";
            case 2:
                return "Level 2: Self-Attention\n\nSelf-attention helps a model decide which words should influence each other.\n\nFor each token, the model compares it with every other token, including itself. A stronger connection means the token should pay more attention to that word when building its meaning.\n\nIn this level, line thickness represents attention strength. Thick lines mean high attention, medium lines mean normal attention, and thin lines mean low attention.\n\nAfter the attention strengths are chosen, the values are combined into weights. These raw weights show how much information each token receives from the whole sentence.\n\nThe weights are then normalized, so they become easier to compare and add up in a stable way. Normalization keeps the model from treating large raw numbers as more important only because of scale.";
            case 3:
                return "Level 3: From Words to Meaning\n\nA model does more than read words one by one. It builds structure from the sentence.\n\nFirst, it detects word relations. For example, an adjective connects to the noun it describes, and a verb connects to the object of the action.\n\nNext, it builds syntax. Words form groups, and those groups explain how the sentence is organized. Nouns, verbs, modifiers, and prepositions each play different roles.\n\nThen the model connects syntax to meaning. The sentence becomes a scene: who is acting, what object is affected, and where things are located.\n\nThe final layer shows that meaning can transfer to a new sentence. Once the model understands roles such as actor, object, and location, it can apply the same logic to different words and situations.";
            case 4:
                return "Level 4: Feed-Forward Network\n\nAfter attention, each token still needs more processing. A feed-forward network, or FFN, refines the information inside each token.\n\nThe FFN expands a token into many features. A feature is a small piece of information, such as whether the token is important, useful, or unrelated to the current context.\n\nUseful features are kept. Important features are intensified, so they have a stronger effect. Irrelevant features are refrained or removed, so they do not distract the model.\n\nAfter filtering, the FFN compresses the useful features back into an output representation. This output token carries clearer information than the original input token.\n\nIn short, attention gathers context, and the FFN cleans and strengthens each token based on that context.";
            default:
                return "";
        }
    }
}
