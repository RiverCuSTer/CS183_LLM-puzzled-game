using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Part3Script : MonoBehaviour
{
    private const float FontScale = 0.15f;

    [System.Serializable]
    private class EmbeddingToken
    {
        public string text;
        public string group;
        public Vector3 target;
    }

    [SerializeField] private Level1Manager level1Manager;

    private readonly List<EmbeddingToken> tokens = new List<EmbeddingToken>();
    private readonly List<EmbeddingTokenCard> tokenCards = new List<EmbeddingTokenCard>();
    private readonly List<RectTransform> regionPanels = new List<RectTransform>();

    private RectTransform rectTransform;
    private RectTransform spaceRoot;
    private RectTransform tokenRoot;
    private TMP_Text feedbackText;
    private Button submitButton;
    private bool initialized;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        if (level1Manager == null)
        {
            level1Manager = FindFirstObjectByType<Level1Manager>();
        }
    }

    void OnEnable()
    {
        BuildTokens();
        BuildUI();
        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        UpdateTokenFeedback();
    }

    private void BuildTokens()
    {
        tokens.Clear();
        AddToken("Move", "Action", new Vector3(-2.4f, -1.8f, 0f));
        AddToken("Open", "Action", new Vector3(-1.4f, -1.8f, 0f));
        AddToken("Find", "Action", new Vector3(-2.0f, -2.6f, 0f));

        AddToken("red", "Attribute", new Vector3(1.4f, 1.8f, 0f));
        AddToken("blue", "Attribute", new Vector3(2.4f, 1.8f, 0f));
        AddToken("hidden", "Attribute", new Vector3(1.9f, 2.6f, 0f));

        AddToken("block", "Object", new Vector3(1.4f, -1.8f, 0f));
        AddToken("door", "Object", new Vector3(2.4f, -1.8f, 0f));
        AddToken("key", "Object", new Vector3(1.9f, -2.6f, 0f));

        AddToken("the", "Function", new Vector3(-2.4f, 1.8f, 0f));
        AddToken("the", "Function", new Vector3(-1.4f, 1.8f, 0f));
        AddToken("a", "Function", new Vector3(-1.9f, 2.6f, 0f));
    }

    private void AddToken(string text, string group, Vector3 target)
    {
        tokens.Add(new EmbeddingToken { text = text, group = group, target = target });
    }

    private void BuildUI()
    {
        ClearChildren(transform);
        tokenCards.Clear();
        regionPanels.Clear();

        Image panelImage = GetComponent<Image>();
        if (panelImage != null)
        {
            panelImage.color = new Color(1f, 1f, 1f, 0f);
            panelImage.raycastTarget = true;
        }

        spaceRoot = CreatePanel("EmbeddingSpace", transform, new Vector2(0.08f, 0.08f), new Vector2(0.92f, 0.88f));
        Image spaceHitArea = spaceRoot.gameObject.AddComponent<Image>();
        spaceHitArea.color = new Color(0f, 0f, 0f, 0.01f);
        spaceHitArea.raycastTarget = true;

        tokenRoot = CreatePanel("TokenRoot", transform, Vector2.zero, Vector2.one);

        TMP_Text title = CreateText("Title", transform, "Embedding Space", 180, Color.white, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.22f, 0.9f), new Vector2(0.78f, 0.99f));

        feedbackText = CreateText("Feedback", transform, "Drag every token into its semantic area.", 90, new Color(0.78f, 0.92f, 1f, 1f), TextAlignmentOptions.Center);
        SetRect(feedbackText.rectTransform, new Vector2(0.18f, 0.02f), new Vector2(0.78f, 0.1f));

        submitButton = CreateButton("SubmitButton", transform, "Submit");
        SetSubmitButtonRect(submitButton.GetComponent<RectTransform>());
        submitButton.onClick.AddListener(CheckCompletion);

        CreateRegions();
        CreateTokenCards();
    }

    private void CreateRegions()
    {
        CreateRegion("Function", new Color(1f, 0.85f, 0.35f, 0.16f), new Vector3(-4.55f, 0.35f, 0f), new Vector3(-0.35f, 4.23f, 0f));
        CreateRegion("Attribute", new Color(0.35f, 0.65f, 1f, 0.16f), new Vector3(0.35f, 0.35f, 0f), new Vector3(4.55f, 4.23f, 0f));
        CreateRegion("Action", new Color(1f, 0.35f, 0.35f, 0.16f), new Vector3(-4.55f, -4.23f, 0f), new Vector3(-0.35f, -0.35f, 0f));
        CreateRegion("Object", new Color(0.35f, 1f, 0.45f, 0.16f), new Vector3(0.35f, -4.23f, 0f), new Vector3(4.55f, -0.35f, 0f));
    }

    private void CreateRegion(string label, Color color, Vector3 minPoint, Vector3 maxPoint)
    {
        GameObject obj = new GameObject("Region_" + label, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        obj.transform.SetParent(spaceRoot, false);
        obj.layer = gameObject.layer;

        Image image = obj.GetComponent<Image>();
        image.color = color;
        image.raycastTarget = false;

        Rect area = RectFromPoints(Project2D(minPoint), Project2D(maxPoint));
        RectTransform panel = obj.GetComponent<RectTransform>();
        panel.anchorMin = new Vector2(0.5f, 0.5f);
        panel.anchorMax = new Vector2(0.5f, 0.5f);
        panel.pivot = new Vector2(0.5f, 0.5f);
        panel.anchoredPosition = area.center;
        panel.sizeDelta = area.size;
        regionPanels.Add(panel);

        TMP_Text text = CreateText("Label_" + label, panel, label, 104, new Color(1f, 1f, 1f, 0.35f), TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);
    }

    private void CreateTokenCards()
    {
        Vector2[] positions =
        {
            new Vector2(0.01f, 0.82f), new Vector2(0.01f, 0.68f), new Vector2(0.01f, 0.54f),
            new Vector2(0.01f, 0.4f), new Vector2(0.01f, 0.26f), new Vector2(0.01f, 0.12f),
            new Vector2(0.83f, 0.82f), new Vector2(0.83f, 0.68f), new Vector2(0.83f, 0.54f),
            new Vector2(0.83f, 0.4f), new Vector2(0.83f, 0.26f), new Vector2(0.83f, 0.12f)
        };

        List<EmbeddingToken> shuffledTokens = GetShuffledTokens();
        for (int i = 0; i < shuffledTokens.Count; i++)
        {
            EmbeddingTokenCard card = CreateTokenCard(shuffledTokens[i], i);
            RectTransform rect = card.GetComponent<RectTransform>();
            Vector2 anchor = positions[i % positions.Length];
            rect.anchorMin = anchor;
            rect.anchorMax = anchor + new Vector2(0.16f, 0.105f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            card.StoreHomeRect();
            tokenCards.Add(card);
        }
    }

    private List<EmbeddingToken> GetShuffledTokens()
    {
        List<EmbeddingToken> shuffledTokens = new List<EmbeddingToken>(tokens);
        for (int i = shuffledTokens.Count - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (shuffledTokens[i], shuffledTokens[swapIndex]) = (shuffledTokens[swapIndex], shuffledTokens[i]);
        }

        return shuffledTokens;
    }

    private EmbeddingTokenCard CreateTokenCard(EmbeddingToken token, int index)
    {
        GameObject obj = new GameObject("TokenWord_" + index + "_" + token.text, typeof(RectTransform), typeof(CanvasGroup), typeof(EmbeddingTokenCard));
        obj.transform.SetParent(tokenRoot, false);
        obj.layer = gameObject.layer;

        TMP_Text label = CreateText("Text", obj.transform, token.text, 102, new Color(0.75f, 0.9f, 1f, 1f), TextAlignmentOptions.Center);
        label.raycastTarget = true;
        SetRect(label.rectTransform, Vector2.zero, Vector2.one);

        EmbeddingTokenCard card = obj.GetComponent<EmbeddingTokenCard>();
        card.Initialize(this, index, token.text, token.group, token.target, label);
        return card;
    }

    private void UpdateTokenFeedback()
    {
        for (int i = 0; i < tokenCards.Count; i++)
        {
            tokenCards[i].UpdateVisual(Time.time);
        }

        for (int i = 0; i < tokenCards.Count; i++)
        {
            for (int j = i + 1; j < tokenCards.Count; j++)
            {
                if (!tokenCards[i].IsPlaced || !tokenCards[j].IsPlaced) continue;
                if (tokenCards[i].Group != tokenCards[j].Group) continue;

                float distance = Vector2.Distance(tokenCards[i].Rect.anchoredPosition, tokenCards[j].Rect.anchoredPosition);
                if (distance < 170f)
                {
                    tokenCards[i].SetSemanticFlash(true);
                    tokenCards[j].SetSemanticFlash(true);
                }
            }
        }
    }

    public bool TryPlaceCard(EmbeddingTokenCard card, Vector2 screenPosition, Camera eventCamera)
    {
        Vector2 localPoint;
        if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(spaceRoot, screenPosition, eventCamera, out localPoint))
        {
            return false;
        }

        Rect rect = spaceRoot.rect;
        if (!rect.Contains(localPoint))
        {
            return false;
        }

        Rect semanticArea = GetSemanticArea(card.Group);
        bool isCorrect = semanticArea.Contains(localPoint);
        float distanceToArea = GetDistanceToRect(localPoint, semanticArea);
        card.PlaceInSpace(spaceRoot, localPoint);
        card.SetTargetScore(Mathf.Clamp01(1f - distanceToArea / 260f), isCorrect);
        return true;
    }

    private Rect GetSemanticArea(string group)
    {
        Vector2 min;
        Vector2 max;

        switch (group)
        {
            case "Action":
                min = Project2D(new Vector3(-4.55f, -4.23f, 0f));
                max = Project2D(new Vector3(-0.35f, -0.35f, 0f));
                break;
            case "Attribute":
                min = Project2D(new Vector3(0.35f, 0.35f, 0f));
                max = Project2D(new Vector3(4.55f, 4.23f, 0f));
                break;
            case "Object":
                min = Project2D(new Vector3(0.35f, -4.23f, 0f));
                max = Project2D(new Vector3(4.55f, -0.35f, 0f));
                break;
            case "Function":
                min = Project2D(new Vector3(-4.55f, 0.35f, 0f));
                max = Project2D(new Vector3(-0.35f, 4.23f, 0f));
                break;
            default:
                min = Project2D(new Vector3(-0.5f, -0.5f, 0f));
                max = Project2D(new Vector3(0.5f, 0.5f, 0f));
                break;
        }

        return Rect.MinMaxRect(
            Mathf.Min(min.x, max.x),
            Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x),
            Mathf.Max(min.y, max.y)
        );
    }

    private Rect RectFromPoints(Vector2 min, Vector2 max)
    {
        return Rect.MinMaxRect(
            Mathf.Min(min.x, max.x),
            Mathf.Min(min.y, max.y),
            Mathf.Max(min.x, max.x),
            Mathf.Max(min.y, max.y)
        );
    }

    private float GetDistanceToRect(Vector2 point, Rect rect)
    {
        float dx = Mathf.Max(rect.xMin - point.x, 0f, point.x - rect.xMax);
        float dy = Mathf.Max(rect.yMin - point.y, 0f, point.y - rect.yMax);
        return new Vector2(dx, dy).magnitude;
    }

    private void CheckCompletion()
    {
        foreach (EmbeddingTokenCard card in tokenCards)
        {
            if (!card.IsCorrect)
            {
                feedbackText.text = "Some tokens are still far from their embedding areas.";
                ApplyWhiteText(feedbackText);
                return;
            }
        }

        feedbackText.text = "Embedding complete.";
        ApplyWhiteText(feedbackText);
        if (level1Manager != null)
        {
            level1Manager.CompleteLevel();
        }
    }

    private Vector2 Project2D(Vector3 point)
    {
        return new Vector2(point.x, point.y) * 145f;
    }

    private RectTransform CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        panel.layer = gameObject.layer;
        RectTransform rect = panel.GetComponent<RectTransform>();
        SetRect(rect, anchorMin, anchorMax);
        return rect;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        obj.transform.SetParent(parent, false);
        obj.layer = gameObject.layer;

        TMP_Text tmp = obj.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize * FontScale;
        ApplyTextColor(tmp, color);
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private static void ApplyWhiteText(TMP_Text text)
    {
        ApplyTextColor(text, Color.white);
    }

    private static void ApplyTextColor(TMP_Text text, Color color)
    {
        if (text == null) return;

        text.enableVertexGradient = false;
        text.color = color;
        text.faceColor = color;
        if (text.fontSharedMaterial != null)
        {
            text.fontMaterial = new Material(text.fontSharedMaterial);
            text.fontMaterial.SetColor("_FaceColor", color);
        }

        text.SetAllDirty();
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject obj = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        obj.transform.SetParent(parent, false);
        obj.layer = gameObject.layer;

        Image image = obj.GetComponent<Image>();
        image.color = Color.white;
        image.type = Image.Type.Sliced;

        Button button = obj.GetComponent<Button>();
        TMP_Text text = CreateText("Text", obj.transform, label, 72, Color.white, TextAlignmentOptions.Center);
        SetRect(text.rectTransform, Vector2.zero, Vector2.one);
        return button;
    }

    private static void SetSubmitButtonRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-100f, 80f);
        rect.sizeDelta = new Vector2(240f, 80f);
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void ClearChildren(Transform target)
    {
        for (int i = target.childCount - 1; i >= 0; i--)
        {
            GameObject child = target.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Destroy(child);
        }
    }
}

public class EmbeddingTokenCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public RectTransform Rect { get; private set; }
    public string Group { get; private set; }
    public Vector3 Target { get; private set; }
    public bool IsPlaced { get; private set; }
    public bool IsCorrect { get; private set; }

    private Part3Script owner;
    private TMP_Text label;
    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private RectTransform homeParent;
    private Vector2 homeAnchorMin;
    private Vector2 homeAnchorMax;
    private float brightness;
    private bool semanticFlash;

    public void Initialize(Part3Script script, int index, string text, string group, Vector3 target, TMP_Text textLabel)
    {
        owner = script;
        Group = group;
        Target = target;
        label = textLabel;
        Rect = GetComponent<RectTransform>();
        canvas = GetComponentInParent<Canvas>();
        canvasGroup = GetComponent<CanvasGroup>();
    }

    public void StoreHomeRect()
    {
        homeParent = transform.parent as RectTransform;
        homeAnchorMin = Rect.anchorMin;
        homeAnchorMax = Rect.anchorMax;
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        transform.SetParent(canvas.transform, true);
        transform.SetAsLastSibling();
        canvasGroup.alpha = 0.78f;
        canvasGroup.blocksRaycasts = false;
        IsPlaced = false;
        IsCorrect = false;
    }

    public void OnDrag(PointerEventData eventData)
    {
        Rect.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (!owner.TryPlaceCard(this, eventData.position, eventData.pressEventCamera))
        {
            ReturnHome();
        }
    }

    public void PlaceInSpace(RectTransform parent, Vector2 localPoint)
    {
        transform.SetParent(parent, false);
        Rect.anchorMin = new Vector2(0.5f, 0.5f);
        Rect.anchorMax = new Vector2(0.5f, 0.5f);
        Rect.pivot = new Vector2(0.5f, 0.5f);
        Rect.sizeDelta = new Vector2(360f, 150f);
        Rect.anchoredPosition = localPoint;
        IsPlaced = true;
    }

    public void SetTargetScore(float score, bool correct)
    {
        brightness = score;
        IsCorrect = correct;
    }

    public void SetSemanticFlash(bool flashing)
    {
        semanticFlash = semanticFlash || flashing;
    }

    public void UpdateVisual(float time)
    {
        float flash = semanticFlash ? Mathf.PingPong(time * 3.5f, 1f) : 0f;
        float glow = Mathf.Max(brightness, flash);
        label.color = Color.white;
        label.faceColor = Color.white;
        semanticFlash = false;
    }

    private void ReturnHome()
    {
        transform.SetParent(homeParent, false);
        Rect.anchorMin = homeAnchorMin;
        Rect.anchorMax = homeAnchorMax;
        Rect.offsetMin = Vector2.zero;
        Rect.offsetMax = Vector2.zero;
        IsPlaced = false;
        IsCorrect = false;
        brightness = 0f;
    }
}
