// Responsible team member: Zhiyan Lin; Description: Switches main UI panels and refreshes level-select lock states.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject startMenu;
    public GameObject levelSelect;
    public GameObject pauseMenu;
    public GameObject knowledgeMenu;
    public GameObject settingsMenu;

    [Header("Level Select")]
    [SerializeField] private bool buildLevelButtonsAtRuntime;
    [SerializeField] private GameObject levelButtonTemplate;
    [SerializeField] private Transform levelButtonContent;
    [SerializeField] private Vector2 levelButtonStartPosition = new Vector2(0f, 0f);
    [SerializeField] private Vector2 levelButtonSpacing = new Vector2(920f, 0f);
    [SerializeField] private Vector2 levelButtonSize = new Vector2(840f, 220f);
    [SerializeField] private Vector2 levelSelectContentSize = new Vector2(4200f, 0f);
    [SerializeField] private Sprite levelLockSprite;
    [SerializeField] private Vector2 levelLockIconSize = new Vector2(120f, 120f);
    [SerializeField, Range(0f, 1f)] private float lockedLevelAlpha = 0.5f;

    private static readonly string[] LevelButtonLabels = { "LEVEL 1", "LEVEL 2", "LEVEL 3", "LEVEL 4" };
    private readonly Transform[] levelButtons = new Transform[LevelButtonLabels.Length];

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (buildLevelButtonsAtRuntime)
        {
            BuildLevelSelectButtons();
        }
    }

    public void UpdateUI(GameManager.GameState state)
    {
        startMenu.SetActive(false);
        levelSelect.SetActive(false);
        pauseMenu.SetActive(false);
        knowledgeMenu.SetActive(false);
        settingsMenu.SetActive(false);

        switch (state)
        {
            case GameManager.GameState.StartMenu:
                startMenu.SetActive(true);
                break;

            case GameManager.GameState.LevelSelect:
                levelSelect.SetActive(true);
                RefreshLevelSelectLocks();
                break;

            case GameManager.GameState.Pause:
                pauseMenu.SetActive(true);
                break;

            case GameManager.GameState.Database:
                knowledgeMenu.SetActive(true);
                break;

            case GameManager.GameState.Settings:
                settingsMenu.SetActive(true);
                break;
        }
    }

    private void BuildLevelSelectButtons()
    {
        ResolveLevelSelectReferences();

        if (levelButtonTemplate == null || levelButtonContent == null)
        {
            Debug.LogWarning("Level select buttons were not created because references are missing.");
            return;
        }

        for (int i = levelButtonContent.childCount - 1; i >= 0; i--)
        {
            Destroy(levelButtonContent.GetChild(i).gameObject);
        }

        RectTransform contentRect = levelButtonContent as RectTransform;
        if (contentRect != null)
        {
            contentRect.anchorMin = new Vector2(0f, 0.5f);
            contentRect.anchorMax = new Vector2(0f, 0.5f);
            contentRect.pivot = new Vector2(0f, 0.5f);
            contentRect.anchoredPosition = Vector2.zero;
            contentRect.sizeDelta = levelSelectContentSize;
        }

        for (int i = 0; i < LevelButtonLabels.Length; i++)
        {
            GameObject buttonObject = Instantiate(levelButtonTemplate, levelButtonContent);
            buttonObject.name = $"LevelSelectButton{i + 1}";
            buttonObject.SetActive(true);

            RectTransform rectTransform = buttonObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                rectTransform.anchorMin = new Vector2(0f, 0.5f);
                rectTransform.anchorMax = new Vector2(0f, 0.5f);
                rectTransform.pivot = new Vector2(0.5f, 0.5f);
                rectTransform.anchoredPosition = levelButtonStartPosition + levelButtonSpacing * i;
                rectTransform.sizeDelta = levelButtonSize;
            }

            TMP_Text text = buttonObject.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = LevelButtonLabels[i];
            }

            Button button = buttonObject.GetComponent<Button>();
            if (button != null && GameManager.Instance != null)
            {
                button.onClick.RemoveAllListeners();
                int levelNumber = i + 1;
                button.onClick.AddListener(() => GoToLevel(levelNumber));
            }

            levelButtons[i] = buttonObject.transform;
        }

        RefreshLevelSelectLocks();
    }

    public void RefreshLevelSelectLocks()
    {
        ResolveLevelSelectReferences();

        for (int i = 0; i < LevelButtonLabels.Length; i++)
        {
            Transform child = levelButtons[i];
            if (child == null)
            {
                continue;
            }

            bool unlocked = GameManager.IsLevelUnlocked(i + 1);

            Button button = child.GetComponent<Button>();
            if (button != null)
            {
                button.interactable = unlocked;
            }

            TMP_Text text = child.GetComponentInChildren<TMP_Text>(true);
            if (text != null)
            {
                text.text = unlocked ? LevelButtonLabels[i] : LevelButtonLabels[i] + " LOCKED";
            }

            Image levelImage = child.GetComponent<Image>();
            SetImageAlpha(levelImage, unlocked ? 1f : lockedLevelAlpha);

            Image lockImage = EnsureLockIcon(child, i + 1);
            if (lockImage != null)
            {
                lockImage.gameObject.SetActive(!unlocked);
                SetImageAlpha(lockImage, lockedLevelAlpha);
            }
        }
    }

    private void GoToLevel(int levelNumber)
    {
        switch (levelNumber)
        {
            case 1:
                GameManager.Instance.GoToLevel1();
                break;
            case 2:
                GameManager.Instance.GoToLevel2();
                break;
            case 3:
                GameManager.Instance.GoToLevel3();
                break;
            case 4:
                GameManager.Instance.GoToLevel4();
                break;
        }
    }

    private void ResolveLevelSelectReferences()
    {
        if (levelButtonTemplate == null && startMenu != null)
        {
            levelButtonTemplate = startMenu.transform.Find("StartBotton")?.gameObject;
        }

        if (levelButtonContent == null && levelSelect != null)
        {
            Transform content = levelSelect.transform.Find("Scroll View/Viewport/Content");
            levelButtonContent = content;
        }

        for (int i = 0; i < levelButtons.Length; i++)
        {
            if (levelButtons[i] != null)
            {
                continue;
            }

            string buttonName = buildLevelButtonsAtRuntime ? $"LevelSelectButton{i + 1}" : $"TurnToLevel{i + 1}";
            Transform buttonTransform = null;

            if (levelButtonContent != null)
            {
                buttonTransform = levelButtonContent.Find(buttonName);
            }

            if (buttonTransform == null && levelSelect != null)
            {
                buttonTransform = levelSelect.transform.Find(buttonName);
            }

            levelButtons[i] = buttonTransform;
        }
    }

    private Image EnsureLockIcon(Transform levelButton, int levelNumber)
    {
        if (levelNumber <= 1 || levelLockSprite == null)
        {
            return null;
        }

        const string lockIconName = "LockIcon";
        Transform existing = levelButton.Find(lockIconName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;

        if (image == null)
        {
            GameObject lockObject = new GameObject(lockIconName, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            lockObject.transform.SetParent(levelButton, false);
            image = lockObject.GetComponent<Image>();
            image.raycastTarget = false;
        }

        image.sprite = levelLockSprite;
        image.preserveAspect = true;

        RectTransform rectTransform = image.rectTransform;
        rectTransform.anchorMin = new Vector2(0.5f, 0.5f);
        rectTransform.anchorMax = new Vector2(0.5f, 0.5f);
        rectTransform.pivot = new Vector2(0.5f, 0.5f);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.sizeDelta = levelLockIconSize;
        rectTransform.SetAsLastSibling();

        return image;
    }

    private static void SetImageAlpha(Image image, float alpha)
    {
        if (image == null)
        {
            return;
        }

        Color color = image.color;
        color.a = alpha;
        image.color = color;
    }
}
