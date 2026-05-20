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

    private static readonly string[] LevelButtonLabels = { "LEVEL 1", "LEVEL 2", "LEVEL 3", "LEVEL 4" };

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
    }
}
