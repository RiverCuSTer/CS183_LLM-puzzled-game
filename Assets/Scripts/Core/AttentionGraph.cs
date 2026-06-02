using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;

public class AttentionGraph : MonoBehaviour
{
    [Header("Graph Root")]
    public RectTransform lineContainer;

    [Header("Nodes，顺序必须是 Who, Am, I")]
    public RectTransform[] nodeRects;
    public string[] nodeNames = { "Who", "Am", "I" };

    [Header("Path Points，顺序见说明")]
    public RectTransform[] pathPointRects;

    [Header("Line Style，单位是 UI 像素")]
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
        public int level;       // 0=细, 1=中, 2=粗
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
            Debug.LogError("AttentionGraph: lineContainer 没有赋值。");
            return;
        }

        if (nodeRects == null || nodeRects.Length < 3)
        {
            Debug.LogError("AttentionGraph: nodeRects 必须拖入 3 个节点，顺序是 Who, Am, I。");
            return;
        }

        if (pathPointRects == null || pathPointRects.Length < 9)
        {
            Debug.LogError("AttentionGraph: pathPointRects 必须拖入 9 个透明路径点。");
            return;
        }

        CreateAllEdges();

        // 为三个中间路径点（曲线经过的点）绑定点击事件
        BindPathPointButtonsForBetweenEdges();
    }

    void CreateAllEdges()
    {
        // 节点索引：
        // 0 = Who
        // 1 = Am
        // 2 = I

        // 路径点索引：
        // 0 = Who-Am 中间点
        // 1 = Am-I 中间点
        // 2 = I-Who 中间点
        // 3,4 = Who 自环
        // 5,6 = Am 自环
        // 7,8 = I 自环

        // 自环（3条）
        CreateLoopEdge(0, pathPointRects[3], pathPointRects[4]);  // Who -> Who
        CreateLoopEdge(1, pathPointRects[5], pathPointRects[6]);  // Am -> Am
        CreateLoopEdge(2, pathPointRects[7], pathPointRects[8]);  // I -> I

        // 双向边：一条曲线（经过中间点），一条直线（直接连接）
        // Who <-> Am
        CreateBetweenEdge(0, 1, pathPointRects[0]);   // Who -> Am 曲线
        CreateDirectEdge(1, 0);                      // Am -> Who 直线

        // Who <-> I
        CreateBetweenEdge(0, 2, pathPointRects[2]);   // Who -> I 曲线
        CreateDirectEdge(2, 0);                      // I -> Who 直线

        // Am <-> I
        CreateBetweenEdge(1, 2, pathPointRects[1]);   // Am -> I 曲线
        CreateDirectEdge(2, 1);                      // I -> Am 直线
    }

    /// <summary>
    /// 创建经过中间点的曲线（起点 → 中间点 → 终点）
    /// </summary>
    void CreateBetweenEdge(int from, int to, RectTransform viaPoint)
    {
        Vector2 fromLocal = WorldToLocal(nodeRects[from].position);
        Vector2 toLocal = WorldToLocal(nodeRects[to].position);
        Vector2 viaLocal = WorldToLocal(viaPoint.position);
        CreateEdge(from, to, new Vector2[] { fromLocal, viaLocal, toLocal });
    }

    /// <summary>
    /// 创建直接连接两点的直线（起点 → 终点）
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
        obj.transform.SetAsLastSibling();   // 改为放在最上层

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
        // 默认全部中等
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
    /// 为三个中间路径点（0,1,2）添加 Button，点击时选中对应的曲线边
    /// </summary>
    void BindPathPointButtonsForBetweenEdges()
    {
        if (pathPointRects == null || pathPointRects.Length < 3)
        {
            Debug.LogWarning("路径点数量不足，无法绑定中间点按钮。");
            return;
        }

        // 映射：路径点索引 -> (from, to)
        // 索引 0 -> Who->Am 曲线 (0->1)
        // 索引 1 -> Am->I 曲线 (1->2)
        // 索引 2 -> Who->I 曲线 (0->2)
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

            // 移除已有的Button（避免重复）
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