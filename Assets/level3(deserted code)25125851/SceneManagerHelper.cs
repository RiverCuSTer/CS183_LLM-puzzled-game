using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneManagerHelper : MonoBehaviour
{
    // 下一关场景名称，在Inspector填写
    public string nextSceneName;
    // 单例全局调用
    public static SceneManagerHelper Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // 第一关全部通关调用此方法切场景
    public void LevelComplete()
    {
        Debug.Log("第一阶段全部完成，切换场景");
        SceneManager.LoadScene(nextSceneName);
    }
}