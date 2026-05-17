using UnityEngine;

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

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        ChangeState(GameState.StartMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("Current State: " + newState);
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
}