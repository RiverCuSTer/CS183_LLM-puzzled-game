using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Part1Script : MonoBehaviour
{
    [Header("Level Flow")]
    [SerializeField] private Level1Manager level1Manager;

    [Header("Correct Slots")]
    public int[] correctGapSiblingIndices;

    [Header("Sentence Setup")]
    public string sentence = "Move the red block";
    public GameObject letterPrefab;
    public Transform textContainer;

    [Header("Token Setup")]
    public GameObject tokenPrefab;
    public Transform tokenPool;
    public int tokenCount = 3;

    [Header("UI")]
    public Button submitButton;

    [Header("Part1 UI")]
    public GameObject Part1;

    [Header("Next Part")]
    public GameObject Part2;

    private readonly List<LetterSlot> letterSlots = new List<LetterSlot>();
    private bool initialized;
    private bool needsRebuild = true;

    void Awake()
    {
        if (level1Manager == null)
        {
            level1Manager = GetComponent<Level1Manager>();
        }

        if (level1Manager == null)
        {
            level1Manager = FindFirstObjectByType<Level1Manager>();
        }
    }

    void Start()
    {
        InitializeLayout();

        if (submitButton != null)
        {
            submitButton.onClick.RemoveListener(CheckAnswer);
            submitButton.onClick.AddListener(CheckAnswer);
        }

        if (needsRebuild)
        {
            BuildRound();
            needsRebuild = false;
        }
    }

    void OnEnable()
    {
        if (!initialized)
        {
            return;
        }

        if (needsRebuild)
        {
            BuildRound();
            needsRebuild = false;
        }
    }

    public void ConfigureRound(string newSentence, int newTokenCount)
    {
        sentence = newSentence;
        tokenCount = newTokenCount;
        needsRebuild = true;

        if (initialized && gameObject.activeInHierarchy)
        {
            RebuildNow();
        }
    }

    public void RebuildNow()
    {
        if (!initialized)
        {
            needsRebuild = true;
            return;
        }

        BuildRound();
        needsRebuild = false;
    }

    private void InitializeLayout()
    {
        RectTransform containerRect = textContainer.GetComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.pivot = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0f, 80f);
        containerRect.sizeDelta = new Vector2(1500, 120);

        RectTransform tokenPoolRect = tokenPool.GetComponent<RectTransform>();
        tokenPoolRect.anchorMin = new Vector2(0.5f, 0.5f);
        tokenPoolRect.anchorMax = new Vector2(0.5f, 0.5f);
        tokenPoolRect.pivot = new Vector2(0.5f, 0.5f);
        tokenPoolRect.anchoredPosition = new Vector2(0f, -90f);
        tokenPoolRect.sizeDelta = new Vector2(600, 120);

        HorizontalLayoutGroup layout = textContainer.GetComponent<HorizontalLayoutGroup>();
        if (layout == null)
        {
            layout = textContainer.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = 0f;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;

        ContentSizeFitter fitter = textContainer.GetComponent<ContentSizeFitter>();
        if (fitter != null)
        {
            Destroy(fitter);
        }

        initialized = true;
    }

    private void BuildRound()
    {
        ClearChildren(textContainer);
        ClearChildren(tokenPool);
        letterSlots.Clear();

        string[] words = sentence.Split(new[] { ' ' }, System.StringSplitOptions.RemoveEmptyEntries);
        tokenCount = Mathf.Max(0, words.Length - 1);

        HashSet<int> targetGapIndexes = new HashSet<int>();
        int visibleCharCount = 0;
        for (int i = 0; i < sentence.Length; i++)
        {
            if (char.IsWhiteSpace(sentence[i]))
            {
                targetGapIndexes.Add(visibleCharCount);
                continue;
            }

            visibleCharCount++;
        }

        int gapIndex = 0;
        CreateGapSlot(targetGapIndexes.Contains(gapIndex));

        for (int i = 0; i < sentence.Length; i++)
        {
            if (char.IsWhiteSpace(sentence[i]))
            {
                continue;
            }

            CreateLetterObject(sentence[i]);
            gapIndex++;
            CreateGapSlot(targetGapIndexes.Contains(gapIndex));
        }

        for (int i = 0; i < tokenCount; i++)
        {
            GameObject marker = Instantiate(tokenPrefab, tokenPool);
            Draggable draggable = marker.GetComponent<Draggable>();
            if (draggable != null)
            {
                draggable.enabled = true;
                draggable.textContainer = textContainer;
                draggable.letterSlots = letterSlots;
            }
        }

        Debug.Log("Part1 sentence generated. Target gaps: " + tokenCount + ", total gaps: " + letterSlots.Count);
    }

    private void CreateLetterObject(char letter)
    {
        GameObject letterObj = Instantiate(letterPrefab, textContainer);

        LayoutElement layoutElement = letterObj.GetComponent<LayoutElement>();
        if (layoutElement == null)
        {
            layoutElement = letterObj.AddComponent<LayoutElement>();
        }

        ContentSizeFitter contentSizeFitter = letterObj.GetComponent<ContentSizeFitter>();
        if (contentSizeFitter != null)
        {
            Destroy(contentSizeFitter);
        }

        RectTransform letterRect = letterObj.GetComponent<RectTransform>();
        TMP_Text tmp = letterObj.GetComponentInChildren<TMP_Text>();
        if (tmp == null) return;

        tmp.text = letter.ToString();
        tmp.fontSize = 30;
        ApplyWhiteText(tmp);
        tmp.margin = Vector4.zero;
        tmp.alignment = TextAlignmentOptions.Center;
        tmp.ForceMeshUpdate();

        float letterWidth = tmp.preferredWidth + 2.4f;
        layoutElement.minWidth = letterWidth;
        layoutElement.preferredWidth = letterWidth;
        layoutElement.flexibleWidth = 0f;
        letterRect.sizeDelta = new Vector2(letterWidth, 30);
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

    private void CreateGapSlot(bool isTarget)
    {
        GameObject gap = new GameObject("Gap", typeof(RectTransform), typeof(LayoutElement), typeof(LetterSlot));
        gap.transform.SetParent(textContainer, false);
        gap.GetComponent<RectTransform>().sizeDelta = new Vector2(0, 36);
        LayoutElement layoutElement = gap.GetComponent<LayoutElement>();
        layoutElement.minWidth = 0f;
        layoutElement.preferredWidth = 0f;
        layoutElement.flexibleWidth = 0f;
        LetterSlot slot = gap.GetComponent<LetterSlot>();
        slot.isTarget = isTarget;
        letterSlots.Add(slot);
    }

    private void CheckAnswer()
    {
        int placedCount = 0;
        int correctCount = 0;
        foreach (LetterSlot slot in letterSlots)
        {
            if (slot.isOccupied)
            {
                placedCount++;
                if (slot.isTarget)
                {
                    correctCount++;
                }
            }
        }

        if (placedCount == tokenCount && correctCount == tokenCount)
        {
            Debug.Log("Part1 Completed! All tokens placed in word gaps.");
            ProceedToNextPart();
        }
        else
        {
            Debug.Log("Place each token only between two words.");
        }
    }

    private void ProceedToNextPart()
    {
        Debug.Log("Enter Part2");

        if (level1Manager != null)
        {
            level1Manager.GoToPart2();
            return;
        }

        if (Part2 != null)
        {
            Part2.SetActive(true);
        }

        if (Part1 != null)
        {
            Part1.SetActive(false);
        }
    }

    private void ClearChildren(Transform parent)
    {
        if (parent == null) return;

        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            GameObject child = parent.GetChild(i).gameObject;
            child.SetActive(false);
            child.transform.SetParent(null, false);
            Destroy(child);
        }
    }
}
