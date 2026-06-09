using UnityEngine;
using TMPro;

public class SlotCheck : MonoBehaviour
{
    // 正确单词数组，Inspector填入
    public string[] correctWords;
    // 标记当前槽是否已经答对
    [HideInInspector] public bool slotPass = false;
    // 提示文本（可选）
    public TextMeshProUGUI tipText;

    // 点击add按钮触发检测
    public void CheckSlotWords()
    {
        // 获取槽内所有拖拽单词物体
        DragWord[] allWords = GetComponentsInChildren<DragWord>();

        // 数量不对直接报错
        if (allWords.Length != correctWords.Length)
        {
            if (tipText) tipText.text = $"需要放入{correctWords.Length}个单词";
            return;
        }

        // 逐个比对文字
        bool isRight = true;
        for (int i = 0; i < correctWords.Length; i++)
        {
            string wordText = allWords[i].GetComponentInChildren<TextMeshProUGUI>().text.Trim();
            if (wordText != correctWords[i])
            {
                isRight = false;
                break;
            }
        }

        if (isRight)
        {
            slotPass = true;
            if (tipText) tipText.text = "✅ 本句正确";
            CheckAllSlotPass();
        }
        else
        {
            slotPass = false;
            if (tipText) tipText.text = "❌ 单词顺序/内容错误";
        }
    }

    // 检查三个槽是否全部通关，全部通关则切场景
    void CheckAllSlotPass()
    {
        SlotCheck[] allSlots = FindObjectsOfType<SlotCheck>();
        int passCount = 0;
        foreach (var slot in allSlots)
        {
            if (slot.slotPass) passCount++;
        }
        // 3个槽全部答对，执行切换
        if (passCount >= 3)
        {
            SceneManagerHelper.Instance.LevelComplete();
        }
    }
}