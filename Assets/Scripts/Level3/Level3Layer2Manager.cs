using UnityEngine;
using System.Collections.Generic;

public class Level3Layer2Manager : MonoBehaviour
{
    private const float UiScale = 3f;
    private const float LayerY = 12f;
    private const float ColumnHalfWidth = 0.95f;

    public static Level3Layer2Manager Instance { get; private set; }

    private Level3WordBlock[] wordBlocks;
    private readonly List<StackColumn> stackColumns = new List<StackColumn>();
    private string feedback = "";
    private float feedbackUntil;

    public bool IsComplete
    {
        get
        {
            return CountOrderedStackPositions(out int total) == total && total > 0;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnsureStackTargets();
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || Mathf.Abs(camera.transform.position.y - 12f) > 1.5f)
            return;

        int filled = CountOrderedStackPositions(out int total);

        GUIStyle countStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13 * UiScale),
            normal = { textColor = Color.black }
        };
        GUI.Label(new Rect(Screen.width * 0.5f - 480f, Screen.height - 96f, 960f, 72f), $"{filled}/{total} ordered blocks", countStyle);

        if (IsComplete)
        {
            GUIStyle doneStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15 * UiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 0.45f, 0.18f) }
            };
            GUI.Label(new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f), "Syntax stacks complete. Climb to the next layer.", doneStyle);
        }
        else if (!string.IsNullOrEmpty(feedback) && Time.time < feedbackUntil)
        {
            GUIStyle feedbackStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(14 * UiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = Color.black }
            };
            GUI.Label(new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f), feedback, feedbackStyle);
        }
    }

    public void ShowBlockedMessage()
    {
        feedback = "Stack each column from bottom to top in the correct word order.";
        feedbackUntil = Time.time + 2.5f;
    }

    private int CountOrderedStackPositions(out int total)
    {
        EnsureStackTargets();
        int filled = 0;
        total = 0;

        foreach (StackColumn column in stackColumns)
        {
            total += column.ExpectedWords.Count;
            List<Level3WordBlock> wordsInColumn = GetWordsInColumn(column.CenterX);
            int count = Mathf.Min(wordsInColumn.Count, column.ExpectedWords.Count);

            for (int index = 0; index < count; index++)
            {
                if (wordsInColumn[index].Word == column.ExpectedWords[index])
                    filled++;
            }
        }

        return filled;
    }

    private List<Level3WordBlock> GetWordsInColumn(float centerX)
    {
        if (wordBlocks == null || wordBlocks.Length == 0)
            wordBlocks = FindObjectsOfType<Level3WordBlock>();

        List<Level3WordBlock> words = new List<Level3WordBlock>();
        foreach (Level3WordBlock wordBlock in wordBlocks)
        {
            if (wordBlock == null || !wordBlock.gameObject.activeInHierarchy)
                continue;

            Vector3 position = wordBlock.transform.position;
            if (Mathf.Abs(Mathf.Round(position.y / 12f) * 12f - LayerY) > 1.5f)
                continue;

            if (Mathf.Abs(position.x - centerX) <= ColumnHalfWidth)
                words.Add(wordBlock);
        }

        words.Sort((a, b) => a.transform.position.y.CompareTo(b.transform.position.y));
        return words;
    }

    private void EnsureStackTargets()
    {
        if (stackColumns.Count > 0)
            return;

        stackColumns.Add(new StackColumn(-3.2f, "scientist", "careful", "The"));
        stackColumns.Add(new StackColumn(0f, "placed", "gently"));
        stackColumns.Add(new StackColumn(2.35f, "cat", "the"));
        stackColumns.Add(new StackColumn(4.35f, "table", "under"));
    }

    private sealed class StackColumn
    {
        public float CenterX;
        public readonly List<string> ExpectedWords = new List<string>();

        public StackColumn(float centerX, params string[] expectedWords)
        {
            CenterX = centerX;
            ExpectedWords.AddRange(expectedWords);
        }
    }
}
