using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class EscapePauseOverlay : MonoBehaviour
{
    [SerializeField] private GameObject overlayRoot;
    [SerializeField] private Button resumeButton;
    [SerializeField] private Button restartButton;
    [SerializeField] private Button mainMenuButton;
    [SerializeField] private string mainMenuSceneName = "MainScene";

    private bool isOpen;
    private float previousTimeScale = 1f;

    private void Awake()
    {
        if (resumeButton != null)
        {
            resumeButton.onClick.AddListener(Hide);
        }

        if (restartButton != null)
        {
            restartButton.onClick.AddListener(RestartScene);
        }

        if (mainMenuButton != null)
        {
            mainMenuButton.onClick.AddListener(ReturnToMainMenu);
        }

        SetOverlayVisible(false);
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
    }

    public void Show()
    {
        previousTimeScale = Time.timeScale;
        Time.timeScale = 0f;
        SetOverlayVisible(true);
    }

    public void Hide()
    {
        Time.timeScale = previousTimeScale;
        SetOverlayVisible(false);
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
}
