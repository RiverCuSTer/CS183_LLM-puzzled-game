using UnityEngine;

public class UIManager : MonoBehaviour
{
    public static UIManager Instance;

    public GameObject startMenu;
    public GameObject levelSelect;
    public GameObject pauseMenu;
    public GameObject knowledgeMenu;
    public GameObject settingsMenu;

    void Awake()
    {
        Instance = this;
    }

    public void UpdateUI(GameState state)
    {
        startMenu.SetActive(false);
        levelSelect.SetActive(false);
        pauseMenu.SetActive(false);
        knowledgeMenu.SetActive(false);
        settingsMenu.SetActive(false);

        switch (state)
        {
            case GameState.StartMenu:
                startMenu.SetActive(true);
                break;

            case GameState.LevelSelect:
                levelSelect.SetActive(true);
                break;

            case GameState.Pause:
                pauseMenu.SetActive(true);
                break;

            case GameState.Knowledge:
                knowledgeMenu.SetActive(true);
                break;

            case GameState.Settings:
                settingsMenu.SetActive(true);
                break;
        }
    }
}
