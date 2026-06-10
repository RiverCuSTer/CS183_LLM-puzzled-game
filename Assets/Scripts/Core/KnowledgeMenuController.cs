// Responsible team member: Zhiyan Lin; Description: Displays level-specific knowledge text inside the knowledge menu.
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class KnowledgeMenuController : MonoBehaviour
{
    private const float ContentVerticalPadding = 32f;

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
            ResizeKnowledgeContent();
        }

        if (scrollRect != null)
        {
            scrollRect.verticalNormalizedPosition = 1f;
        }
    }

    private void ResizeKnowledgeContent()
    {
        RectTransform textRect = knowledgeText.rectTransform;
        if (textRect == null)
        {
            return;
        }

        if (scrollRect == null)
        {
            scrollRect = knowledgeText.GetComponentInParent<ScrollRect>();
        }

        knowledgeText.ForceMeshUpdate();

        float viewportHeight = scrollRect != null && scrollRect.viewport != null
            ? scrollRect.viewport.rect.height
            : 0f;
        float targetHeight = Mathf.Max(knowledgeText.preferredHeight + ContentVerticalPadding, viewportHeight);

        textRect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, targetHeight);
        textRect.anchoredPosition = new Vector2(textRect.anchoredPosition.x, 0f);
    }
}
