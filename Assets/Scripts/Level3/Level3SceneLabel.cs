using UnityEngine;

public class Level3SceneLabel : MonoBehaviour
{
    [SerializeField] private string label = "label";
    [SerializeField] private Color panelColor = new Color(1f, 1f, 1f, 0.8f);
    [SerializeField] private Vector2 guiSize = new Vector2(180f, 44f);
    [SerializeField] private int fontSize = 16;
    [SerializeField] private bool bold;
    [SerializeField] private float uiScale = 3f;

    private Camera mainCamera;

    private void Awake()
    {
        mainCamera = Camera.main;
    }

    public void Configure(string newLabel, Color newPanelColor, Vector2 newGuiSize, int newFontSize, bool newBold, float newUiScale)
    {
        label = newLabel;
        panelColor = newPanelColor;
        guiSize = newGuiSize;
        fontSize = newFontSize;
        bold = newBold;
        uiScale = newUiScale;
    }

    private void OnGUI()
    {
        if (mainCamera == null)
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(transform.position);
        if (screen.z < 0f)
            return;

        Vector2 scaledSize = guiSize * uiScale;
        Rect rect = new Rect(screen.x - scaledSize.x * 0.5f, Screen.height - screen.y - scaledSize.y * 0.5f, scaledSize.x, scaledSize.y);
        Color previous = GUI.color;
        GUI.color = panelColor;
        GUI.Box(rect, GUIContent.none);
        GUI.color = previous;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(fontSize * uiScale),
            fontStyle = bold ? FontStyle.Bold : FontStyle.Normal,
            wordWrap = true,
            normal = { textColor = Color.black }
        };
        GUI.Label(rect, label, style);
    }
}
