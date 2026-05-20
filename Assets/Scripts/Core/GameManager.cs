using UnityEngine;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
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

    private void LoadLevel(int levelNumber)
    {
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
