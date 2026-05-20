using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KnowledgeMenuController : MonoBehaviour
{
    [TextArea(8, 30)]
    public string level1Knowledge = "";

    [TextArea(8, 30)]
    public string level2Knowledge = "";

    [TextArea(8, 30)]
    public string level3Knowledge = "";

    [TextArea(8, 30)]
    public string level4Knowledge = "";

    [SerializeField] private TMP_Text knowledgeText;
    [SerializeField] private ScrollRect scrollRect;

    void Start()
    {
        ShowLevel1Knowledge();
    }

    public void ShowLevel1Knowledge()
    {
        ShowKnowledge(level1Knowledge);
    }

    public void ShowLevel2Knowledge()
    {
        ShowKnowledge(level2Knowledge);
    }

    public void ShowLevel3Knowledge()
    {
        ShowKnowledge(level3Knowledge);
    }

    public void ShowLevel4Knowledge()
    {
        ShowKnowledge(level4Knowledge);
    }

    private void ShowKnowledge(string text)
    {
        if (knowledgeText != null)
        {
            knowledgeText.text = text;
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }
}
