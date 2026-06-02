using UnityEngine;
using System.Collections.Generic;

public class Level3Layer2Manager : MonoBehaviour
{
    private const float UiScale = 3f;
    private const float LayerY = 12f;
    private const float ColumnHalfWidth = 0.95f;

    [Header("Layer2 Words")]
    [SerializeField] private Level3WordBlock[] layer2WordBlocks;

    [Header("Initial Spawn")]
    [SerializeField] private bool randomizeInitialPositions = true;
    [SerializeField] private int randomSeed = -1;
    [SerializeField] private float spawnLeftX = -5.65f;
    [SerializeField] private float spawnRightX = 5.65f;
    [SerializeField] private float spawnTopY = 15.4f;
    [SerializeField] private float spawnYSpacing = 0.75f;
    [SerializeField] private float spawnSideColumnSpacing = 0.55f;
    [SerializeField] private int spawnRowsPerSideColumn = 8;

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
        CacheWordBlocks();

        if (randomizeInitialPositions)
        {
            ShuffleAndSpawnWordsOnSides();
        }
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || Mathf.Abs(camera.transform.position.y - LayerY) > 1.5f)
            return;

        int filled = CountOrderedStackPositions(out int total);

        GUIStyle countStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13 * UiScale),
            normal = { textColor = Color.black }
        };

        GUI.Label(
            new Rect(Screen.width - 560f, 96f, 520f, 72f),
            $"{filled}/{total} ordered blocks",
            countStyle
        );

        if (IsComplete)
        {
            GUIStyle doneStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15 * UiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 0.45f, 0.18f) }
            };

            GUI.Label(
                new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f),
                "Syntax stacks complete. Climb to the next layer.",
                doneStyle
            );
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

            GUI.Label(
                new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f),
                feedback,
                feedbackStyle
            );
        }
    }

    public void ShowBlockedMessage()
    {
        feedback = "Stack each column from bottom to top in the correct word order.";
        feedbackUntil = Time.time + 2.5f;
    }

    private void CacheWordBlocks()
    {
        if (layer2WordBlocks != null && layer2WordBlocks.Length > 0)
        {
            wordBlocks = layer2WordBlocks;
            return;
        }

        Level3WordBlock[] all = FindObjectsOfType<Level3WordBlock>();
        List<Level3WordBlock> result = new List<Level3WordBlock>();

        foreach (Level3WordBlock wordBlock in all)
        {
            if (wordBlock == null)
                continue;

            Vector3 position = wordBlock.transform.position;
            float roundedLayerY = Mathf.Round(position.y / 12f) * 12f;

            if (Mathf.Abs(roundedLayerY - LayerY) <= 1.5f)
            {
                result.Add(wordBlock);
            }
        }

        wordBlocks = result.ToArray();
    }

    private void ShuffleAndSpawnWordsOnSides()
    {
        CacheWordBlocks();

        if (wordBlocks == null || wordBlocks.Length == 0)
            return;

        List<Level3WordBlock> blocks = new List<Level3WordBlock>();

        foreach (Level3WordBlock wordBlock in wordBlocks)
        {
            if (wordBlock != null && wordBlock.gameObject.activeInHierarchy)
            {
                blocks.Add(wordBlock);
            }
        }

        Shuffle(blocks);

        int leftIndex = 0;
        int rightIndex = 0;

        for (int i = 0; i < blocks.Count; i++)
        {
            bool spawnOnLeft = i % 2 == 0;

            int sideIndex = spawnOnLeft ? leftIndex++ : rightIndex++;
            int row = sideIndex % Mathf.Max(1, spawnRowsPerSideColumn);
            int column = sideIndex / Mathf.Max(1, spawnRowsPerSideColumn);

            float x = spawnOnLeft
                ? spawnLeftX - column * spawnSideColumnSpacing
                : spawnRightX + column * spawnSideColumnSpacing;

            float y = spawnTopY - row * spawnYSpacing;

            blocks[i].PlaceAt(new Vector3(x, y, 0f), LayerY);
        }
    }

    private void Shuffle<T>(IList<T> list)
    {
        System.Random random = randomSeed >= 0
            ? new System.Random(randomSeed)
            : new System.Random();

        for (int i = list.Count - 1; i > 0; i--)
        {
            int j = random.Next(i + 1);

            T temp = list[i];
            list[i] = list[j];
            list[j] = temp;
        }
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
                {
                    filled++;
                }
            }
        }

        return filled;
    }

    private List<Level3WordBlock> GetWordsInColumn(float centerX)
    {
        if (wordBlocks == null || wordBlocks.Length == 0)
        {
            CacheWordBlocks();
        }

        List<Level3WordBlock> words = new List<Level3WordBlock>();

        if (wordBlocks == null)
            return words;

        foreach (Level3WordBlock wordBlock in wordBlocks)
        {
            if (wordBlock == null || !wordBlock.gameObject.activeInHierarchy)
                continue;

            Vector3 position = wordBlock.transform.position;

            if (Mathf.Abs(Mathf.Round(position.y / 12f) * 12f - LayerY) > 1.5f)
                continue;

            if (Mathf.Abs(position.x - centerX) <= ColumnHalfWidth)
            {
                words.Add(wordBlock);
            }
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
