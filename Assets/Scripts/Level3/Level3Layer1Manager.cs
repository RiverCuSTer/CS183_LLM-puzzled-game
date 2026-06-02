using System.Collections.Generic;
using UnityEngine;

public class Level3Layer1Manager : MonoBehaviour
{
    private const float UiScale = 3f;
    private const float PendingLineWidth = 6f;

    public static Level3Layer1Manager Instance { get; private set; }

    private readonly List<Level3RelationNode> nodes = new List<Level3RelationNode>();
    private readonly HashSet<string> completedPairs = new HashSet<string>();
    private Level3RelationNode selectedNode;
    private string feedback = "Click two related word cards.";
    private float feedbackUntil;

    private static readonly string[,] Relations =
    {
        { "careful", "scientist", "careful -> scientist" },
        { "scientist", "placed", "scientist -> placed" },
        { "placed", "cat", "placed -> cat" },
        { "cat", "under_table", "cat -> under table" },
        { "dog", "sat", "dog -> sat" },
        { "dog", "on_carpet", "dog -> on carpet" }
    };

    public bool IsComplete => completedPairs.Count >= Relations.GetLength(0);

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        nodes.Clear();
        nodes.AddRange(FindObjectsOfType<Level3RelationNode>());
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || !IsLayerOneActive(camera))
            return;

        DrawPendingConnectionLine(camera);

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(15 * UiScale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = IsComplete ? new Color(0.1f, 0.45f, 0.18f) : Color.black }
        };

        if (IsComplete)
            GUI.Label(new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f), "All word relations are connected. Climb to the next layer.", style);

        GUIStyle countStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13 * UiScale),
            normal = { textColor = Color.black }
        };
        GUI.Label(new Rect(Screen.width * 0.5f - 420f, Screen.height - 96f, 840f, 72f), $"{completedPairs.Count}/{Relations.GetLength(0)} relations", countStyle);

        if (Time.time > feedbackUntil && !IsComplete)
            feedback = selectedNode == null ? "" : $"Selected: {selectedNode.DisplayText}";
    }

    public void SelectNode(Level3RelationNode node)
    {
        if (node == null)
            return;

        if (selectedNode == null)
        {
            selectedNode = node;
            selectedNode.SetSelected(true);
            SetFeedback($"Selected: {node.DisplayText}");
            return;
        }

        if (selectedNode == node)
        {
            selectedNode.SetSelected(false);
            selectedNode = null;
            SetFeedback("Selection cleared.");
            return;
        }

        int relationIndex = FindRelation(selectedNode.NodeId, node.NodeId);
        if (relationIndex >= 0)
        {
            string key = GetRelationKey(relationIndex);
            if (completedPairs.Add(key))
                SetFeedback($"Connected: {Relations[relationIndex, 2]}");
            else
                SetFeedback($"{Relations[relationIndex, 2]} is already connected.");

            RefreshNodeStates();
        }
        else
        {
            selectedNode.SetSelected(false);
            SetFeedback($"{selectedNode.DisplayText} and {node.DisplayText} are not directly connected.");
        }

        selectedNode = null;
    }

    public void ShowBlockedMessage()
    {
        SetFeedback("Complete all word relations before climbing upward.");
    }

    public static bool IsLayerOneActive(Camera camera)
    {
        return Mathf.Abs(camera.transform.position.y) < 1.5f;
    }

    private int FindRelation(string a, string b)
    {
        for (int i = 0; i < Relations.GetLength(0); i++)
        {
            if ((Relations[i, 0] == a && Relations[i, 1] == b) || (Relations[i, 0] == b && Relations[i, 1] == a))
                return i;
        }

        return -1;
    }

    private static string GetRelationKey(int index)
    {
        return Relations[index, 0] + "|" + Relations[index, 1];
    }

    private void RefreshNodeStates()
    {
        foreach (Level3RelationNode node in nodes)
        {
            if (node == null)
                continue;

            node.SetMatched(IsNodeComplete(node.NodeId));
        }
    }

    private bool IsNodeComplete(string nodeId)
    {
        bool hasRelation = false;

        for (int i = 0; i < Relations.GetLength(0); i++)
        {
            if (Relations[i, 0] != nodeId && Relations[i, 1] != nodeId)
                continue;

            hasRelation = true;
            if (!completedPairs.Contains(GetRelationKey(i)))
                return false;
        }

        return hasRelation;
    }

    private void DrawPendingConnectionLine(Camera camera)
    {
        if (selectedNode == null)
            return;

        Vector3 startScreen = camera.WorldToScreenPoint(selectedNode.transform.position);
        if (startScreen.z < 0f)
            return;

        Vector2 start = new Vector2(startScreen.x, Screen.height - startScreen.y);
        Vector2 end = Event.current.mousePosition;

        DrawLine(start, end, new Color(0f, 0f, 0f, 0.42f), PendingLineWidth);
    }

    private static void DrawLine(Vector2 start, Vector2 end, Color color, float width)
    {
        Vector2 direction = end - start;
        float length = direction.magnitude;
        if (length <= 0.01f)
            return;

        Matrix4x4 previousMatrix = GUI.matrix;
        Color previousColor = GUI.color;
        int previousDepth = GUI.depth;

        GUI.depth = 20;
        GUI.color = color;
        GUIUtility.RotateAroundPivot(Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg, start);
        GUI.DrawTexture(new Rect(start.x, start.y - width * 0.5f, length, width), Texture2D.whiteTexture);

        GUI.depth = previousDepth;
        GUI.color = previousColor;
        GUI.matrix = previousMatrix;
    }

    private void SetFeedback(string text)
    {
        feedback = text;
        feedbackUntil = Time.time + 2.5f;
    }
}
