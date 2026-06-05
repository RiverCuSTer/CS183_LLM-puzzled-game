using UnityEngine;//25125851  chuzhaoning
using UnityEngine.UI;
using System.Collections.Generic;
using System.Linq;

public class SlotChecker : MonoBehaviour
{
    [Tooltip("正确的单词（只填纯小写单词，不要加 Word_ 前缀！）")]
    public List<string> correctNames = new List<string>();

    [Header("UI 反馈（可选）")]
    public Text resultText;
    public Image slotBackground;
    public Color successColor = Color.green;

    private List<DragWord> currentWords = new List<DragWord>();
    private bool isCompleted = false;

    public void OnWordDropped(DragWord word)
    {
        if (isCompleted) return;

        currentWords.Add(word);

        // 满 3 个自动判定
        if (currentWords.Count >= correctNames.Count)
        {
            CheckAnswer();
        }
    }

    public void CheckAnswer()
    {
        if (isCompleted || currentWords.Count == 0) return;

        List<string> inputNames = new List<string>();
        foreach (var word in currentWords)
        {
            string objName = word.gameObject.name;

            // 清理可能存在的后缀
            if (objName.Contains("(Clone)")) objName = objName.Replace("(Clone)", "");

            // 【核心修复】自动剔除 "Word_" 前缀！
            if (objName.StartsWith("Word_"))
            {
                objName = objName.Replace("Word_", "");
            }

            inputNames.Add(objName.Trim().ToLower());
        }

        // 无序比对
        bool isCorrect = false;
        if (inputNames.Count == correctNames.Count)
        {
            var sortedInput = inputNames.OrderBy(x => x).ToList();
            var sortedCorrect = correctNames.Select(x => x.Trim().ToLower()).OrderBy(x => x).ToList();
            isCorrect = sortedInput.SequenceEqual(sortedCorrect);
        }

        if (isCorrect)
        {
            isCompleted = true;
            Debug.Log($"[{gameObject.name}] 判定结果: 通过！");
            if (resultText != null) resultText.text = "Correct!";
            if (slotBackground != null) slotBackground.color = successColor;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] 判定结果: 错误！收集到的词是: {string.Join(", ", inputNames)}");
            if (resultText != null) resultText.text = "Wrong!";

            // 判定错误：让所有卡片重新出现，并回到原位
            foreach (var word in currentWords)
            {
                word.ReturnToHome();
            }
            currentWords.Clear();
        }
    }
}
