using UnityEngine;

public class Level3RelationNode : MonoBehaviour
{
    private const float UiScale = 3f;

    [SerializeField] private string nodeId;
    [SerializeField] private string displayText;
    [SerializeField] private Color normalColor = new Color(1f, 1f, 1f, 0.88f);
    [SerializeField] private Color selectedColor = new Color(0.98f, 0.9f, 0.46f, 0.96f);
    [SerializeField] private Color matchedColor = new Color(0.62f, 0.92f, 0.72f, 0.96f);
    [SerializeField] private Vector2 guiSize = new Vector2(160f, 48f);

    private Camera mainCamera;
    private bool isSelected;
    private bool isMatched;

    public string NodeId => nodeId;
    public string DisplayText => displayText;
    public bool IsMatched => isMatched;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    private void OnGUI()
    {
        if (mainCamera == null || !Level3Layer1Manager.IsLayerOneActive(mainCamera))
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(transform.position);
        if (screen.z < 0f)
            return;

        Vector2 scaledSize = guiSize * UiScale;
        Rect rect = new Rect(screen.x - scaledSize.x * 0.5f, Screen.height - screen.y - scaledSize.y * 0.5f, scaledSize.x, scaledSize.y);

        Color previous = GUI.color;
        GUI.color = isMatched ? matchedColor : isSelected ? selectedColor : normalColor;

        GUIStyle buttonStyle = new GUIStyle(GUI.skin.button)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(16 * UiScale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = Color.black },
            hover = { textColor = Color.black },
            active = { textColor = Color.black }
        };

        if (GUI.Button(rect, displayText, buttonStyle))
            Level3Layer1Manager.Instance?.SelectNode(this);

        GUI.color = previous;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
    }

    public void SetMatched(bool matched)
    {
        isMatched = matched;
        if (matched)
            isSelected = false;
    }
}
