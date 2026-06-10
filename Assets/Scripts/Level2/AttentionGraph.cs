// Responsible team member: Hanyun Zhu, Zhiyu Huang; Description: Creates and manages the interactive attention graph with editable edge weights.
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AttentionGraph : MonoBehaviour
{
    [Header("Graph Root")]
    public RectTransform lineContainer;

    [Header("Nodes, order must be Who, Am, I")]
    public RectTransform[] nodeRects;
    public string[] nodeNames = { "Who", "Am", "I" };

    [Header("Path Points, see the documented order")]
    public RectTransform[] pathPointRects;

    [Header("Line Style, in UI pixels")]
    public float thinWidth = 6f;
    public float mediumWidth = 12f;
    public float thickWidth = 20f;
    public Color normalColor = Color.white;
    public Color selectedColor = new Color(1f, 0.9f, 0.2f, 1f);
    public Color errorColor = Color.red;

    [Header("UI")]
    public GameObject tooltipPanel;
    public TextMeshProUGUI tooltipText;
    public Button thinnerButton;
    public Button thickerButton;

    private readonly List<GraphEdge> edges = new List<GraphEdge>();
    private GraphEdge selectedEdge;
    private Canvas rootCanvas;

    [System.Serializable]
    public class GraphEdge
    {
        public List<Image> lineImages = new List<Image>();
        public int from;
        public int to;
        public int level;       // 0 = thin, 1 = medium, 2 = thick
        public float weight;    // 0.5, 1, 2
        public string displayName;
    }

    void Awake()
    {
        rootCanvas = GetComponentInParent<Canvas>();
    }

    void Start()
    {
        if (tooltipPanel != null)
            tooltipPanel.SetActive(false);

        if (thinnerButton != null)
        {
            thinnerButton.onClick.RemoveAllListeners();
            thinnerButton.onClick.AddListener(DecreaseThickness);
        }

        if (thickerButton != null)
        {
            thickerButton.onClick.RemoveAllListeners();
            thickerButton.onClick.AddListener(IncreaseThickness);
        }
    }

    public void Initialize()
    {
        ClearGraph();

        if (lineContainer == null)
        {
            Debug.LogError("AttentionGraph: lineContainer is not assigned.");
            return;
        }

        if (nodeRects == null || nodeRects.Length < 3)
        {
            Debug.LogError("AttentionGraph: nodeRects must contain 3 nodes in the order Who, Am, I.");
            return;
        }

        if (pathPointRects == null || pathPointRects.Length < 9)
        {
            Debug.LogError("AttentionGraph: pathPointRects must contain 9 transparent path points.");
            return;
        }

        CreateAllEdges();

        // Bind click events to the three intermediate path points used by curved edges.
        BindPathPointButtonsForBetweenEdges();
    }

    void CreateAllEdges()
    {
        // Node indices:
        // 0 = Who
        // 1 = Am
        // 2 = I

        // Path point indices:
        // 0 = Who-Am intermediate point
        // 1 = Am-I intermediate point
        // 2 = I-Who intermediate point
        // 3,4 = Who self-loop
        // 5,6 = Am self-loop
        // 7,8 = I self-loop

        // Self-loops.
        CreateLoopEdge(0, pathPointRects[3], pathPointRects[4]);  // Who -> Who
        CreateLoopEdge(1, pathPointRects[5], pathPointRects[6]);  // Am -> Am
        CreateLoopEdge(2, pathPointRects[7], pathPointRects[8]);  // I -> I

        // Bidirectional edges: one curved line through a midpoint and one direct line.
        // Who <-> Am
        CreateBetweenEdge(0, 1, pathPointRects[0]);   // Who -> Am curved line
        CreateDirectEdge(1, 0);                      // Am -> Who direct line

        // Who <-> I
        CreateBetweenEdge(0, 2, pathPointRects[2]);   // Who -> I curved line
        CreateDirectEdge(2, 0);                      // I -> Who direct line

        // Am <-> I
        CreateBetweenEdge(1, 2, pathPointRects[1]);   // Am -> I curved line
        CreateDirectEdge(2, 1);                      // I -> Am direct line
    }

    /// <summary>
    /// Creates a curved edge that passes through an intermediate point.
    /// </summary>
    void CreateBetweenEdge(int from, int to, RectTransform viaPoint)
    {
        Vector2 fromLocal = WorldToLocal(nodeRects[from].position);
        Vector2 toLocal = WorldToLocal(nodeRects[to].position);
        Vector2 viaLocal = WorldToLocal(viaPoint.position);
        CreateEdge(from, to, new Vector2[] { fromLocal, viaLocal, toLocal });
    }

    /// <summary>
    /// Creates a direct straight edge between two points.
    /// </summary>
    void CreateDirectEdge(int from, int to)
    {
        Vector2 fromLocal = WorldToLocal(nodeRects[from].position);
        Vector2 toLocal = WorldToLocal(nodeRects[to].position);
        CreateEdge(from, to, new Vector2[] { fromLocal, toLocal });
    }

    void CreateLoopEdge(int nodeIndex, RectTransform loopPointA, RectTransform loopPointB)
    {
        Vector2 p0 = WorldToLocal(nodeRects[nodeIndex].position);
        Vector2 p1 = WorldToLocal(loopPointA.position);
        Vector2 p2 = WorldToLocal(loopPointB.position);
        Vector2 p3 = p0;
        CreateEdge(nodeIndex, nodeIndex, new Vector2[] { p0, p1, p2, p3 });
    }

    void CreateEdge(int from, int to, Vector2[] points)
    {
        GraphEdge edge = new GraphEdge
        {
            from = from,
            to = to,
            level = 1,
            weight = 1f,
            displayName = $"{nodeNames[from]} → {nodeNames[to]}"
        };

        for (int i = 0; i < points.Length - 1; i++)
        {
            Image lineImage = CreateUILine(points[i], points[i + 1], $"Line_{from}_{to}_part{i + 1}");
            edge.lineImages.Add(lineImage);

            Button btn = lineImage.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;
            btn.onClick.AddListener(() => OnEdgeClicked(edge));
        }

        ApplyEdgeVisual(edge);
        edges.Add(edge);
    }

    Image CreateUILine(Vector2 start, Vector2 end, string lineName)
    {
        GameObject obj = new GameObject(lineName, typeof(RectTransform), typeof(Image));
        obj.transform.SetParent(lineContainer, false);
        obj.transform.SetAsLastSibling();   // Draw this line above earlier UI elements.

        RectTransform rect = obj.GetComponent<RectTransform>();
        Image img = obj.GetComponent<Image>();

        img.color = normalColor;
        img.raycastTarget = true;

        Vector2 dir = end - start;
        float length = dir.magnitude;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0f, 0.5f);
        rect.anchoredPosition = start;
        rect.sizeDelta = new Vector2(length, mediumWidth);
        rect.localRotation = Quaternion.Euler(0f, 0f, angle);
        rect.localScale = Vector3.one;

        return img;
    }

    Vector2 WorldToLocal(Vector3 worldPos)
    {
        Camera cam = null;
        if (rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay)
            cam = rootCanvas.worldCamera;

        Vector2 screenPoint = RectTransformUtility.WorldToScreenPoint(cam, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(lineContainer, screenPoint, cam, out Vector2 localPoint);
        return localPoint;
    }

    void OnEdgeClicked(GraphEdge edge)
    {
        selectedEdge = edge;

        foreach (var e in edges)
            ApplyEdgeVisual(e);

        ApplyEdgeVisual(edge, true);

        if (tooltipText != null)
        {
            tooltipText.text = $"Q({nodeNames[edge.from]})·K({nodeNames[edge.to]})\n" +
                               $"Thickness: {GetLevelName(edge.level)}\n" +
                               $"Value: {edge.weight}";
        }

        if (tooltipPanel != null)
            tooltipPanel.SetActive(true);
    }

    void DecreaseThickness()
    {
        if (selectedEdge == null) return;
        selectedEdge.level = Mathf.Max(0, selectedEdge.level - 1);
        UpdateWeightByLevel(selectedEdge);
        ApplyEdgeVisual(selectedEdge, true);
        RefreshTooltip();
    }

    void IncreaseThickness()
    {
        if (selectedEdge == null) return;
        selectedEdge.level = Mathf.Min(2, selectedEdge.level + 1);
        UpdateWeightByLevel(selectedEdge);
        ApplyEdgeVisual(selectedEdge, true);
        RefreshTooltip();
    }

    void UpdateWeightByLevel(GraphEdge edge)
    {
        if (edge.level == 0) edge.weight = 0.5f;
        else if (edge.level == 1) edge.weight = 1f;
        else edge.weight = 2f;
    }

    void ApplyEdgeVisual(GraphEdge edge, bool selected = false)
    {
        float width = mediumWidth;
        if (edge.level == 0) width = thinWidth;
        if (edge.level == 2) width = thickWidth;

        Color color = selected ? selectedColor : normalColor;

        foreach (Image img in edge.lineImages)
        {
            if (img == null) continue;
            img.color = color;
            RectTransform rect = img.GetComponent<RectTransform>();
            if (rect != null)
                rect.sizeDelta = new Vector2(rect.sizeDelta.x, width);
        }
    }

    void RefreshTooltip()
    {
        if (selectedEdge == null || tooltipText == null) return;
        tooltipText.text = $"Q({nodeNames[selectedEdge.from]})·K({nodeNames[selectedEdge.to]})\n" +
                           $"Thickness: {GetLevelName(selectedEdge.level)}\n" +
                           $"Value: {selectedEdge.weight}";
    }

    string GetLevelName(int level)
    {
        if (level == 0) return "thin";
        if (level == 1) return "medium";
        return "thick";
    }

    public float[,] GetAttentionWeights()
    {
        float[,] weights = new float[3, 3];
        // Default all weights to medium.
        for (int i = 0; i < 3; i++)
            for (int j = 0; j < 3; j++)
                weights[i, j] = 1f;

        foreach (GraphEdge edge in edges)
        {
            if (edge.from >= 0 && edge.from < 3 && edge.to >= 0 && edge.to < 3)
                weights[edge.from, edge.to] = edge.weight;
        }
        return weights;
    }

    public void ResetEdgesToWhite()
    {
        selectedEdge = null;
        foreach (GraphEdge edge in edges)
            foreach (Image img in edge.lineImages)
                if (img != null) img.color = normalColor;

        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public void ResetEdgesToDefault()
    {
        selectedEdge = null;
        foreach (GraphEdge edge in edges)
        {
            edge.level = 1;
            edge.weight = 1f;
            ApplyEdgeVisual(edge, false);
        }
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    public GraphEdge GetEdge(int from, int to)
    {
        foreach (GraphEdge edge in edges)
        {
            if (edge.from == from && edge.to == to)
                return edge;
        }

        return null;
    }

    public void HighlightEdges(List<GraphEdge> targetEdges, Color color)
    {
        if (targetEdges == null) return;

        foreach (GraphEdge edge in targetEdges)
        {
            if (edge == null) continue;

            foreach (Image img in edge.lineImages)
            {
                if (img != null)
                    img.color = color;
            }
        }
    }


    public void HighlightWrongEdges()
    {
        foreach (GraphEdge edge in edges)
            foreach (Image img in edge.lineImages)
                if (img != null) img.color = errorColor;
    }

    void ClearGraph()
    {
        selectedEdge = null;
        foreach (GraphEdge edge in edges)
            foreach (Image img in edge.lineImages)
                if (img != null) Destroy(img.gameObject);
        edges.Clear();
        if (tooltipPanel != null) tooltipPanel.SetActive(false);
    }

    /// <summary>
    /// Adds buttons to the three intermediate path points so clicking them selects the corresponding curved edge.
    /// </summary>
    void BindPathPointButtonsForBetweenEdges()
    {
        if (pathPointRects == null || pathPointRects.Length < 3)
        {
            Debug.LogWarning("Not enough path points to bind intermediate point buttons.");
            return;
        }

        // Mapping: path point index -> (from, to).
        // Index 0 -> Who->Am curved line (0->1)
        // Index 1 -> Am->I curved line (1->2)
        // Index 2 -> Who->I curved line (0->2)
        (int from, int to)[] mappings = new (int, int)[]
        {
            (0, 1),   // Who -> Am
            (1, 2),   // Am -> I
            (0, 2)    // Who -> I
        };

        for (int i = 0; i < 3; i++)
        {
            RectTransform point = pathPointRects[i];
            if (point == null) continue;

            // Remove an existing Button to avoid duplicate listeners.
            Button existingBtn = point.GetComponent<Button>();
            if (existingBtn != null)
                DestroyImmediate(existingBtn);

            Button btn = point.gameObject.AddComponent<Button>();
            btn.transition = Selectable.Transition.None;

            int from = mappings[i].from;
            int to = mappings[i].to;
            btn.onClick.AddListener(() => {
                GraphEdge targetEdge = edges.Find(e => e.from == from && e.to == to);
                if (targetEdge != null)
                    OnEdgeClicked(targetEdge);
            });
        }
    }
}
