using UnityEngine;
using System.Collections.Generic;

public class Level3Layer2Manager : MonoBehaviour
{
    private const float UiScale = 3f;
    private const float LayerY = 12f;
    private const float ColumnHalfWidth = 0.95f;
    private const float LayerCameraOrthographicSize = 4.75f;
    private const float FallingResetYOffset = 0.6f;
    private const float DefaultColumnBottomY = LayerY - LayerCameraOrthographicSize + FallingResetYOffset;
    private const float OriginalColumnTopY = 14.2f;
    private const float ReducedColumnTopY = DefaultColumnBottomY + (OriginalColumnTopY - DefaultColumnBottomY) * 2f / 3f;
    private const string Layer2Prompt = "Stack the words into syntax columns from bottom to top.";

    [Header("Layer2 Words")]
    [SerializeField] private Level3WordBlock[] layer2WordBlocks;

    [Header("Column Judge Areas")]
    [SerializeField] private bool showColumnJudgeAreas = true;
    [SerializeField] private float columnBottomY = DefaultColumnBottomY;
    [SerializeField] private float columnTopY = ReducedColumnTopY;
    [SerializeField] private float guideAreaTopY = OriginalColumnTopY;
    [SerializeField] private float roleLabelWorldY = LayerY - LayerCameraOrthographicSize + 0.25f;
    [SerializeField] private Color columnAreaColor = new Color(0.2f, 0.45f, 0.85f, 0.18f);
    [SerializeField] private Color columnBorderColor = new Color(0.05f, 0.18f, 0.42f, 0.85f);
    [SerializeField] private Color roleLabelColor = new Color(0.12f, 0.12f, 0.12f, 1f);

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

        DrawColumnJudgeAreas(camera);

        int filled = CountOrderedStackPositions(out int total);
        DrawUpperGuideArea(camera);

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
                GetGuideMessageRect(camera),
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
                GetGuideMessageRect(camera),
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

            if (position.y < columnBottomY || position.y > columnTopY)
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

        stackColumns.Add(new StackColumn(-3.75f, "Noun / Modifier / Article", "scientist", "careful", "The"));
        stackColumns.Add(new StackColumn(-1.25f, "Verb / Modifier", "placed", "gently"));
        stackColumns.Add(new StackColumn(1.25f, "Noun / Article", "cat", "the"));
        stackColumns.Add(new StackColumn(3.75f, "Noun / Preposition", "table", "under"));
    }

    private void DrawColumnJudgeAreas(Camera camera)
    {
        EnsureStackTargets();

        if (showColumnJudgeAreas)
        {
            foreach (StackColumn column in stackColumns)
            {
                Rect areaRect = GetWorldRectOnScreen(
                    camera,
                    column.CenterX - ColumnHalfWidth,
                    columnBottomY,
                    column.CenterX + ColumnHalfWidth,
                    columnTopY
                );

                DrawFilledRect(areaRect, columnAreaColor);
                DrawRectOutline(areaRect, columnBorderColor, 3f);
            }
        }

        GUIStyle roleStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(7 * UiScale),
            fontStyle = FontStyle.Bold,
            wordWrap = true,
            normal = { textColor = roleLabelColor }
        };

        foreach (StackColumn column in stackColumns)
        {
            Vector3 screen = camera.WorldToScreenPoint(new Vector3(column.CenterX, roleLabelWorldY, 0f));
            if (screen.z < 0f)
                continue;

            Rect rect = new Rect(screen.x - 145f, Screen.height - screen.y - 36f, 290f, 72f);
            GUI.Label(rect, column.RoleLabel, roleStyle);
        }
    }

    private void DrawUpperGuideArea(Camera camera)
    {
        Rect guideRect = GetWorldRectOnScreen(camera, -4.9f, columnTopY, 4.9f, guideAreaTopY);

        GUIStyle promptStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(16 * UiScale),
            wordWrap = false,
            normal = { textColor = new Color(0.12f, 0.12f, 0.12f, 1f) }
        };

        GUI.Label(new Rect(Screen.width * 0.5f - 900f, guideRect.y + 132f, 1800f, 96f), Layer2Prompt, promptStyle);
    }

    private Rect GetGuideMessageRect(Camera camera)
    {
        Rect guideRect = GetWorldRectOnScreen(camera, -4.9f, columnTopY, 4.9f, guideAreaTopY);
        return new Rect(guideRect.x, guideRect.y + guideRect.height - 72f, guideRect.width, 60f);
    }

    private Rect GetWorldRectOnScreen(Camera camera, float minX, float minY, float maxX, float maxY)
    {
        Vector3 bottomLeft = camera.WorldToScreenPoint(new Vector3(minX, minY, 0f));
        Vector3 topRight = camera.WorldToScreenPoint(new Vector3(maxX, maxY, 0f));

        float x = bottomLeft.x;
        float y = Screen.height - topRight.y;
        float width = topRight.x - bottomLeft.x;
        float height = topRight.y - bottomLeft.y;

        return new Rect(x, y, width, height);
    }

    private void DrawFilledRect(Rect rect, Color color)
    {
        Color previousColor = GUI.color;
        GUI.color = color;
        GUI.DrawTexture(rect, Texture2D.whiteTexture);
        GUI.color = previousColor;
    }

    private void DrawRectOutline(Rect rect, Color color, float thickness)
    {
        DrawFilledRect(new Rect(rect.xMin, rect.yMin, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.xMin, rect.yMax - thickness, rect.width, thickness), color);
        DrawFilledRect(new Rect(rect.xMin, rect.yMin, thickness, rect.height), color);
        DrawFilledRect(new Rect(rect.xMax - thickness, rect.yMin, thickness, rect.height), color);
    }

    private sealed class StackColumn
    {
        public float CenterX;
        public string RoleLabel;
        public readonly List<string> ExpectedWords = new List<string>();

        public StackColumn(float centerX, string roleLabel, params string[] expectedWords)
        {
            CenterX = centerX;
            RoleLabel = roleLabel;
            ExpectedWords.AddRange(expectedWords);
        }
    }
}
