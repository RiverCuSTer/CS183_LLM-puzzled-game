// Responsible team member: Zhiyan Lin; Description: Manages global game state, scene loading, level navigation, and level unlock progress.
using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    private const string OpenLevelSelectOnLoadKey = "OpenLevelSelectOnMainSceneLoad";
    private const string MainSceneName = "MainScene";
    private const int MaxLevel = 4;

    public static GameManager Instance;

    public enum GameState
    {
        StartMenu,
        LevelSelect,
        Game,
        Pause,
        Database,
        Settings
    }

    public GameState currentState;
    [SerializeField] private GameState debugStartState = GameState.StartMenu;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PlayerPrefs.GetInt(OpenLevelSelectOnLoadKey, 0) == 1)
        {
            PlayerPrefs.DeleteKey(OpenLevelSelectOnLoadKey);
            PlayerPrefs.Save();
            ChangeState(GameState.LevelSelect);
            return;
        }

        ChangeState(debugStartState);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Current State: " + newState);
        UIManager.Instance?.UpdateUI(newState);
    }

    // Button functions

    public void GoToStartMenu()
    {
        ChangeState(GameState.StartMenu);
    }

    public void GoToLevelSelect()
    {
        ChangeState(GameState.LevelSelect);
    }

    public void GoToGame()
    {
        ChangeState(GameState.Game);
    }

    public void GoToLevel1()
    {
        LoadLevel(1);
    }

    public void GoToLevel2()
    {
        LoadLevel(2);
    }

    public void GoToLevel3()
    {
        LoadLevel(3);
    }

    public void GoToLevel4()
    {
        LoadLevel(4);
    }

    public void PauseGame()
    {
        ChangeState(GameState.Pause);
    }

    public void OpenDatabase()
    {
        ChangeState(GameState.Database);
    }

    public void OpenSettings()
    {
        ChangeState(GameState.Settings);
    }

    public static void ReturnToLevelSelect()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt(OpenLevelSelectOnLoadKey, 1);
        PlayerPrefs.Save();
        SceneManager.LoadScene(MainSceneName);
    }

    public static bool IsLevelUnlocked(int levelNumber)
    {
        if (levelNumber <= 1)
        {
            return true;
        }

        if (levelNumber > MaxLevel)
        {
            return false;
        }

        return PlayerPrefs.GetInt(GetLevelCompletedKey(levelNumber - 1), 0) == 1;
    }

    public static void MarkLevelCompleted(int levelNumber)
    {
        if (levelNumber < 1 || levelNumber > MaxLevel)
        {
            return;
        }

        PlayerPrefs.SetInt(GetLevelCompletedKey(levelNumber), 1);
        PlayerPrefs.Save();
    }

    private static string GetLevelCompletedKey(int levelNumber)
    {
        return $"Level{levelNumber}_Completed";
    }

    private void LoadLevel(int levelNumber)
    {
        if (!IsLevelUnlocked(levelNumber))
        {
            Debug.LogWarning($"Level {levelNumber} is locked. Complete Level {levelNumber - 1} first.");
            UIManager.Instance?.RefreshLevelSelectLocks();
            return;
        }

        string sceneName = $"Level{levelNumber}";
        if (!Application.CanStreamedLevelBeLoaded(sceneName))
        {
            Debug.LogWarning($"Scene '{sceneName}' is not in Build Settings or has not been created yet.");
            return;
        }

        ChangeState(GameState.Game);
        SceneManager.LoadScene(sceneName);
    }
}
