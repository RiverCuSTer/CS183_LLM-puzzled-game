using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Part2Script : MonoBehaviour
{
    private const float FontScale = 0.15f;

    private enum Part2Phase
    {
        Mapping,
        Sequencing
    }

    [System.Serializable]
    public class TokenIdPair
    {
        public string token;
        public int id;
    }

    [Header("Level Flow")]
    [SerializeField] private Level1Manager level1Manager;

    [Header("Vocabulary")]
    [SerializeField] private Sprite wordColumnSprite;
    [SerializeField] private Sprite vocabularyCardSprite;
    [SerializeField] private GameObject idCardPrefab;
    [SerializeField] private GameObject placeholderPrefab;
    [SerializeField] private TokenIdPair[] vocabulary =
    {
        new TokenIdPair { token = "Move", id = 7421 },
        new TokenIdPair { token = "the", id = 464 },
        new TokenIdPair { token = "red", id = 3152 },
        new TokenIdPair { token = "block", id = 1801 }
    };
    [SerializeField] private int[] mappingPoolOrder = { 3152, 1801, 7421, 464 };
    [SerializeField] private int[] sequencePoolOrder = { 1801, 3152, 464, 7421 };

    [Header("UI")]
    [SerializeField] private float cardWidth = 250f;
    [SerializeField] private float cardHeight = 140f;

    private readonly List<TokenIdSlot> mappingSlots = new List<TokenIdSlot>();
    private readonly List<TokenIdSlot> sequenceSlots = new List<TokenIdSlot>();
    private readonly List<IdDragCard> activeCards = new List<IdDragCard>();

    private Canvas canvas;
    private Transform mappingArea;
    private Transform idPool;
    private Transform sequenceArea;
    private TMP_Text feedbackText;
    private Button submitButton;
    private Part2Phase phase;
    private bool initialized;
    private bool needsRebuild;

    void Awake()
    {
        if (level1Manager == null)
        {
            level1Manager = FindFirstObjectByType<Level1Manager>();
        }

        canvas = GetComponentInParent<Canvas>();
    }

    void OnEnable()
    {
        if (!initialized)
        {
            BuildUI();
            initialized = true;
        }
        else if (needsRebuild)
        {
            RebuildUI();
            needsRebuild = false;
        }

        StartMappingPhase();
    }

    public void ConfigureRound(Level1Manager.LevelRoundData round)
    {
        if (round == null)
        {
            return;
        }

        vocabulary = round.vocabulary;
        mappingPoolOrder = round.mappingPoolOrder;
        sequencePoolOrder = round.sequencePoolOrder;
        needsRebuild = true;

        if (initialized && gameObject.activeInHierarchy)
        {
            RebuildUI();
            needsRebuild = false;
            StartMappingPhase();
        }
    }

    public string[] GetTokens()
    {
        string[] tokens = new string[vocabulary.Length];
        for (int i = 0; i < vocabulary.Length; i++)
        {
            tokens[i] = vocabulary[i].token;
        }

        return tokens;
    }

    public int[] GetTokenIDs()
    {
        int[] ids = new int[vocabulary.Length];
        for (int i = 0; i < vocabulary.Length; i++)
        {
            ids[i] = vocabulary[i].id;
        }

        return ids;
    }

    private void BuildUI()
    {
        ClearChildren(transform);

        Image background = gameObject.GetComponent<Image>();
        if (background != null)
        {
            background.color = new Color(1f, 1f, 1f, 0f);
            background.raycastTarget = false;
        }

        GameObject leftPanel = CreatePanel("VocabPanel_Left", transform, new Vector2(0f, 0f), new Vector2(0.24f, 1f), Vector2.zero, Vector2.zero);
        Image wordColumn = CreateImage("WordColumnImage", leftPanel.transform, wordColumnSprite, new Color(1f, 0.83f, 0.04f, 1f));
        SetRect(wordColumn.rectTransform, new Vector2(0.08f, 0.1f), new Vector2(0.92f, 0.9f), Vector2.zero, Vector2.zero);

        BuildVocabularyRows(leftPanel.transform);

        GameObject centerPanel = CreatePanel("MappingArea_Center", transform, new Vector2(0.25f, 0.18f), new Vector2(0.76f, 0.9f), Vector2.zero, Vector2.zero);
        mappingArea = centerPanel.transform;

        GameObject rightPanel = CreatePanel("IdPool_Right", transform, new Vector2(0.78f, 0.18f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);
        idPool = rightPanel.transform;

        TMP_Text poolTitle = CreateText("IdPoolTitle", rightPanel.transform, "ID Pool", 90, Color.white, TextAlignmentOptions.Center);
        SetRect(poolTitle.rectTransform, new Vector2(0f, 0.84f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);

        sequenceArea = CreatePanel("SequenceArea", transform, new Vector2(0.25f, 0.02f), new Vector2(0.98f, 0.24f), Vector2.zero, Vector2.zero).transform;

        feedbackText = CreateText("FeedbackText", transform, "", 72, new Color(0.8f, 0.95f, 1f, 1f), TextAlignmentOptions.Center);
        SetRect(feedbackText.rectTransform, new Vector2(0.25f, 0.9f), new Vector2(0.98f, 0.98f), Vector2.zero, Vector2.zero);

        submitButton = CreateButton("SubmitButton", transform, "Submit");
        SetSubmitButtonRect(submitButton.GetComponent<RectTransform>());
        submitButton.onClick.AddListener(CheckCurrentPhase);
    }

    private void RebuildUI()
    {
        BuildUI();
        initialized = true;
    }

    private void StartMappingPhase()
    {
        phase = Part2Phase.Mapping;
        ClearPhaseObjects();
        mappingSlots.Clear();
        sequenceSlots.Clear();

        feedbackText.text = "Use the left vocabulary table. Drag each ID to its token.";

        TMP_Text title = CreateText("MappingTitle", mappingArea, "Token ID Mapping", 120, Color.white, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 0.82f), new Vector2(1f, 0.98f), Vector2.zero, Vector2.zero);

        float step = 1f / vocabulary.Length;
        for (int i = 0; i < vocabulary.Length; i++)
        {
            float minX = i * step;
            float maxX = (i + 1) * step;

            TMP_Text tokenText = CreateText("Token_" + vocabulary[i].token, mappingArea, vocabulary[i].token, 126, Color.white, TextAlignmentOptions.Center);
            SetRect(tokenText.rectTransform, new Vector2(minX, 0.48f), new Vector2(maxX, 0.72f), Vector2.zero, Vector2.zero);

            TokenIdSlot slot = CreateSlot("MapSlot_" + vocabulary[i].token, mappingArea, vocabulary[i].id, cardWidth, cardHeight);
            SetRect(slot.GetComponent<RectTransform>(), new Vector2(minX + 0.08f, 0.26f), new Vector2(maxX - 0.08f, 0.42f), Vector2.zero, Vector2.zero);
            mappingSlots.Add(slot);
        }

        SpawnIdPoolCards(GetShuffledTokenIds());
    }

    private void StartSequencingPhase()
    {
        phase = Part2Phase.Sequencing;
        ClearPhaseObjects();
        sequenceSlots.Clear();

        feedbackText.text = "Now sort the IDs into the model input sequence.";

        TMP_Text title = CreateText("SequenceTitle", mappingArea, "Input ID Sequence", 120, Color.white, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0f, 0.72f), new Vector2(1f, 0.9f), Vector2.zero, Vector2.zero);

        float step = 1f / vocabulary.Length;
        for (int i = 0; i < vocabulary.Length; i++)
        {
            TokenIdSlot slot = CreateSlot("SequenceSlot_" + i, mappingArea, vocabulary[i].id, cardWidth, cardHeight);
            SetRect(slot.GetComponent<RectTransform>(), new Vector2(i * step + 0.08f, 0.42f), new Vector2((i + 1) * step - 0.08f, 0.58f), Vector2.zero, Vector2.zero);
            sequenceSlots.Add(slot);

            TMP_Text indexText = CreateText("SequenceIndex_" + i, mappingArea, (i + 1).ToString(), 84, new Color(0.65f, 0.85f, 1f, 1f), TextAlignmentOptions.Center);
            SetRect(indexText.rectTransform, new Vector2(i * step, 0.28f), new Vector2((i + 1) * step, 0.38f), Vector2.zero, Vector2.zero);
        }

        SpawnIdPoolCards(GetShuffledTokenIds());
    }

    private void CheckCurrentPhase()
    {
        List<TokenIdSlot> slotsToCheck = phase == Part2Phase.Mapping ? mappingSlots : sequenceSlots;
        foreach (TokenIdSlot slot in slotsToCheck)
        {
            if (!slot.HasCorrectCard())
            {
                feedbackText.text = phase == Part2Phase.Mapping
                    ? "Some IDs do not match the vocabulary table yet."
                    : "The integer sequence is not in token order yet.";
                feedbackText.color = new Color(1f, 0.45f, 0.35f, 1f);
                return;
            }
        }

        feedbackText.color = new Color(0.55f, 1f, 0.75f, 1f);

        if (phase == Part2Phase.Mapping)
        {
            feedbackText.text = "Token IDs matched.";
            if (level1Manager != null)
            {
                level1Manager.tokens = GetTokens();
                level1Manager.tokenIDs = GetTokenIDs();
            }

            StartSequencingPhase();
            return;
        }

        feedbackText.text = "Input IDs: [" + string.Join(", ", GetTokenIDs()) + "]";
        if (level1Manager != null)
        {
            level1Manager.tokenIDs = GetTokenIDs();
            level1Manager.CompletePart2Round();
        }
    }

    private void SpawnIdPoolCards(int[] ids)
    {
        activeCards.Clear();

        for (int i = 0; i < ids.Length; i++)
        {
            IdDragCard card = CreateIdCard("IdCard_" + ids[i], idPool, ids[i]);
            RectTransform rect = card.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.08f, 0.62f - i * 0.16f);
            rect.anchorMax = new Vector2(0.92f, 0.76f - i * 0.16f);
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            card.StorePoolRect();
            activeCards.Add(card);
        }
    }

    private int[] GetShuffledTokenIds()
    {
        int[] ids = new int[vocabulary.Length];
        for (int i = 0; i < vocabulary.Length; i++)
        {
            ids[i] = vocabulary[i].id;
        }

        for (int i = ids.Length - 1; i > 0; i--)
        {
            int swapIndex = Random.Range(0, i + 1);
            (ids[i], ids[swapIndex]) = (ids[swapIndex], ids[i]);
        }

        return ids;
    }

    private TokenIdSlot CreateSlot(string name, Transform parent, int expectedId, float width, float height)
    {
        GameObject slotObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(TokenIdSlot));
        slotObject.transform.SetParent(parent, false);
        slotObject.layer = gameObject.layer;

        Image image = slotObject.GetComponent<Image>();
        image.color = new Color(1f, 1f, 1f, 0f);
        image.raycastTarget = true;

        TokenIdSlot slot = slotObject.GetComponent<TokenIdSlot>();
        slot.Initialize(expectedId, canvas, idPool);

        GameObject placeholderObject = CreateCardTemplate("Placeholder", slotObject.transform, placeholderPrefab);
        placeholderObject.layer = gameObject.layer;
        SetRect(placeholderObject.GetComponent<RectTransform>(), Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        TMP_Text label = placeholderObject.transform.Find("PlaceholderText")?.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = CreateText("PlaceholderText", placeholderObject.transform, "?", 114, new Color(0.7f, 0.85f, 1f, 1f), TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        label.text = "?";
        ApplyWhiteText(label);
        slot.SetPlaceholder(placeholderObject);

        RectTransform rect = slotObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(width, height);
        return slot;
    }

    private IdDragCard CreateIdCard(string name, Transform parent, int id)
    {
        GameObject cardObject = CreateCardTemplate(name, parent, idCardPrefab);
        if (cardObject.GetComponent<CanvasGroup>() == null)
        {
            cardObject.AddComponent<CanvasGroup>();
        }

        if (cardObject.GetComponent<IdDragCard>() == null)
        {
            cardObject.AddComponent<IdDragCard>();
        }

        cardObject.layer = gameObject.layer;

        Image image = cardObject.GetComponent<Image>();
        ConfigureVocabularyFrameImage(image);

        TMP_Text label = cardObject.transform.Find("IdText")?.GetComponent<TMP_Text>();
        if (label == null)
        {
            label = CreateText("IdText", cardObject.transform, id.ToString(), 114, new Color(0.7f, 0.85f, 1f, 1f), TextAlignmentOptions.Center);
            SetRect(label.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }

        label.text = id.ToString();
        ApplyWhiteText(label);

        IdDragCard card = cardObject.GetComponent<IdDragCard>();
        card.Initialize(id, canvas, idPool);

        RectTransform rect = cardObject.GetComponent<RectTransform>();
        rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        return card;
    }

    private void BuildVocabularyRows(Transform parent)
    {
        TMP_Text title = CreateText("VocabTitle", parent, "VOCAB", 84, Color.black, TextAlignmentOptions.Center);
        SetRect(title.rectTransform, new Vector2(0.16f, 0.76f), new Vector2(0.9f, 0.86f), Vector2.zero, Vector2.zero);

        float top = 0.68f;
        float rowHeight = 0.13f;
        float gap = 0.025f;

        for (int i = 0; i < vocabulary.Length; i++)
        {
            float rowMax = top - i * (rowHeight + gap);
            float rowMin = rowMax - rowHeight;

            GameObject row = CreatePanel("VocabRow_" + vocabulary[i].token, parent, new Vector2(0.16f, rowMin), new Vector2(0.9f, rowMax), Vector2.zero, Vector2.zero);

            TMP_Text rowText = CreateText("VocabPairText", row.transform, vocabulary[i].token + " " + vocabulary[i].id, 72, Color.black, TextAlignmentOptions.Center);
            SetRect(rowText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);
        }
    }

    private GameObject CreatePanel(string name, Transform parent, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        GameObject panel = new GameObject(name, typeof(RectTransform));
        panel.transform.SetParent(parent, false);
        panel.layer = gameObject.layer;
        SetRect(panel.GetComponent<RectTransform>(), anchorMin, anchorMax, offsetMin, offsetMax);
        return panel;
    }

    private Image CreateImage(string name, Transform parent, Sprite sprite, Color fallbackColor)
    {
        GameObject imageObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
        imageObject.transform.SetParent(parent, false);
        imageObject.layer = gameObject.layer;

        Image image = imageObject.GetComponent<Image>();
        image.sprite = sprite;
        image.color = sprite == null ? fallbackColor : Color.white;
        image.type = Image.Type.Sliced;
        return image;
    }

    private TMP_Text CreateText(string name, Transform parent, string text, float fontSize, Color color, TextAlignmentOptions alignment)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
        textObject.transform.SetParent(parent, false);
        textObject.layer = gameObject.layer;

        TMP_Text tmp = textObject.GetComponent<TMP_Text>();
        tmp.text = text;
        tmp.fontSize = fontSize * FontScale;
        ApplyWhiteText(tmp);
        tmp.alignment = alignment;
        tmp.enableWordWrapping = false;
        tmp.raycastTarget = false;
        return tmp;
    }

    private GameObject CreateCardTemplate(string name, Transform parent, GameObject prefab)
    {
        GameObject frame = prefab != null
            ? Instantiate(prefab, parent)
            : new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));

        if (prefab == null)
        {
            frame.transform.SetParent(parent, false);
        }

        frame.name = name;
        frame.layer = gameObject.layer;
        RectTransform rect = frame.GetComponent<RectTransform>();
        if (rect != null && prefab == null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.sizeDelta = new Vector2(cardWidth, cardHeight);
        }

        ConfigureVocabularyFrameImage(frame.GetComponent<Image>());
        return frame;
    }

    private void ConfigureVocabularyFrameImage(Image image)
    {
        if (image == null) return;

        if (vocabularyCardSprite != null)
        {
            image.sprite = vocabularyCardSprite;
        }

        image.color = Color.white;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = true;
    }

    private void ApplyWhiteText(TMP_Text text)
    {
        if (text == null) return;

        text.enableVertexGradient = false;
        text.color = Color.white;
        text.faceColor = Color.white;
        if (text.fontSharedMaterial != null)
        {
            text.fontMaterial = new Material(text.fontSharedMaterial);
            text.fontMaterial.SetColor("_FaceColor", Color.white);
        }

        text.SetAllDirty();
    }

    private Button CreateButton(string name, Transform parent, string label)
    {
        GameObject buttonObject = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(parent, false);
        buttonObject.layer = gameObject.layer;

        Image image = buttonObject.GetComponent<Image>();
        image.color = Color.white;
        image.type = Image.Type.Sliced;

        Button button = buttonObject.GetComponent<Button>();
        TMP_Text buttonText = CreateText("Text", buttonObject.transform, label, 36, Color.white, TextAlignmentOptions.Center);
        SetRect(buttonText.rectTransform, Vector2.zero, Vector2.one, Vector2.zero, Vector2.zero);

        return button;
    }

    private void ClearPhaseObjects()
    {
        ClearChildren(mappingArea);
        ClearChildren(idPool, "IdPoolTitle");
        ClearChildren(sequenceArea);
        activeCards.Clear();
    }

    private static void ClearChildren(Transform target, string keepName = null)
    {
        if (target == null) return;

        for (int i = target.childCount - 1; i >= 0; i--)
        {
            Transform child = target.GetChild(i);
            if (!string.IsNullOrEmpty(keepName) && child.name == keepName)
            {
                continue;
            }

            Destroy(child.gameObject);
        }
    }

    private static void SetRect(RectTransform rect, Vector2 anchorMin, Vector2 anchorMax, Vector2 offsetMin, Vector2 offsetMax)
    {
        rect.anchorMin = anchorMin;
        rect.anchorMax = anchorMax;
        rect.offsetMin = offsetMin;
        rect.offsetMax = offsetMax;
    }

    private static void SetSubmitButtonRect(RectTransform rect)
    {
        rect.anchorMin = new Vector2(1f, 0f);
        rect.anchorMax = new Vector2(1f, 0f);
        rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-100f, 80f);
        rect.sizeDelta = new Vector2(240f, 80f);
    }
}

public class IdDragCard : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    public int Id { get; private set; }

    private Canvas canvas;
    private CanvasGroup canvasGroup;
    private Image image;
    private RectTransform rectTransform;
    private Transform poolParent;
    private TokenIdSlot currentSlot;
    private Vector2 poolAnchorMin;
    private Vector2 poolAnchorMax;
    private Vector2 poolOffsetMin;
    private Vector2 poolOffsetMax;

    public void Initialize(int id, Canvas parentCanvas, Transform pool)
    {
        Id = id;
        canvas = parentCanvas;
        poolParent = pool;
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        image = GetComponent<Image>();
        RestoreVisual();
    }

    public void StorePoolRect()
    {
        poolAnchorMin = rectTransform.anchorMin;
        poolAnchorMax = rectTransform.anchorMax;
        poolOffsetMin = rectTransform.offsetMin;
        poolOffsetMax = rectTransform.offsetMax;
        RestoreVisual();
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (currentSlot != null)
        {
            currentSlot.ClearCard(this);
            currentSlot = null;
        }

        Vector3 worldPosition = transform.position;
        transform.SetParent(canvas.transform, true);
        transform.position = worldPosition;
        transform.SetAsLastSibling();
        canvasGroup.alpha = 0.75f;
        canvasGroup.blocksRaycasts = false;
        RestoreVisual();
    }

    public void OnDrag(PointerEventData eventData)
    {
        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        canvasGroup.alpha = 1f;
        canvasGroup.blocksRaycasts = true;

        if (currentSlot == null)
        {
            ReturnToPool();
        }
    }

    public void PlaceInSlot(TokenIdSlot slot)
    {
        currentSlot = slot;
        transform.SetParent(slot.transform, false);
        rectTransform.anchorMin = Vector2.zero;
        rectTransform.anchorMax = Vector2.one;
        rectTransform.offsetMin = Vector2.zero;
        rectTransform.offsetMax = Vector2.zero;
        RestoreVisual();
    }

    public void ReturnToPool()
    {
        transform.SetParent(poolParent, false);
        rectTransform.anchorMin = poolAnchorMin;
        rectTransform.anchorMax = poolAnchorMax;
        rectTransform.offsetMin = poolOffsetMin;
        rectTransform.offsetMax = poolOffsetMax;
        RestoreVisual();
    }

    private void RestoreVisual()
    {
        if (image != null)
        {
            image.color = Color.white;
        }
    }
}

public class TokenIdSlot : MonoBehaviour, IDropHandler
{
    private int expectedId;
    private Canvas canvas;
    private Transform poolParent;
    private IdDragCard currentCard;
    private GameObject placeholder;
    private Image image;

    public void Initialize(int id, Canvas parentCanvas, Transform pool)
    {
        expectedId = id;
        canvas = parentCanvas;
        poolParent = pool;
        image = GetComponent<Image>();
        RestoreVisual();
    }

    public void SetPlaceholder(GameObject placeholderObject)
    {
        placeholder = placeholderObject;
    }

    public bool HasCorrectCard()
    {
        bool correct = currentCard != null && currentCard.Id == expectedId;
        RestoreVisual();

        return correct;
    }

    public void ClearCard(IdDragCard card)
    {
        if (currentCard != card) return;
        currentCard = null;
        if (placeholder != null)
        {
            placeholder.SetActive(true);
        }

        if (image != null)
        {
            RestoreVisual();
        }
    }

    public void OnDrop(PointerEventData eventData)
    {
        IdDragCard card = eventData.pointerDrag == null ? null : eventData.pointerDrag.GetComponent<IdDragCard>();
        if (card == null) return;

        if (currentCard != null)
        {
            currentCard.ReturnToPool();
        }

        currentCard = card;
        currentCard.PlaceInSlot(this);

        if (placeholder != null)
        {
            placeholder.SetActive(false);
        }

        if (image != null)
        {
            RestoreVisual();
        }
    }

    private void RestoreVisual()
    {
        if (image == null) return;

        image.color = new Color(1f, 1f, 1f, 0f);
    }
}
