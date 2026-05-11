using UnityEngine;
using UnityEngine.SceneManagement; // 用于切换场景

public class GameManager : MonoBehaviour
{
    // 单例模式：方便其他脚本访问
    public static GameManager Instance;

    public GameState currentState;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        DontDestroyOnLoad(gameObject); // 切换场景不销毁这个物体
    }

    void Start()
    {
        // 初始状态
        ChangeState(GameState.StartMenu);
    }

    public void ChangeState(GameState newState)
    {
        currentState = newState;
        Debug.Log("状态切换至: " + newState);

        // 这里后续可以添加逻辑，比如切换状态时暂停游戏时间等
    }

    // 退出游戏的方法
    public void QuitGame()
    {
        Application.Quit();
        Debug.Log("退出游戏");
    }
}