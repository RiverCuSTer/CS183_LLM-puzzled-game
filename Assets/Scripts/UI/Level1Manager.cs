using UnityEngine;

public class Level1Manager : MonoBehaviour
{
    [System.Serializable]
    public class LevelRoundData
    {
        public string sentence;
        public Part2Script.TokenIdPair[] vocabulary;
        public int[] mappingPoolOrder;
        public int[] sequencePoolOrder;
    }

    public GameObject Panel_Part1;
    public GameObject Panel_Part2;
    public GameObject Panel_Part3;

    [Header("Debug")]
    public bool startAtPart3ForTesting;

    [Header("Rounds")]
    public LevelRoundData[] rounds =
    {
        new LevelRoundData
        {
            sentence = "Move the red block",
            vocabulary = new[]
            {
                new Part2Script.TokenIdPair { token = "Move", id = 7421 },
                new Part2Script.TokenIdPair { token = "the", id = 464 },
                new Part2Script.TokenIdPair { token = "red", id = 3152 },
                new Part2Script.TokenIdPair { token = "block", id = 1801 }
            },
            mappingPoolOrder = new[] { 3152, 1801, 7421, 464 },
            sequencePoolOrder = new[] { 1801, 3152, 464, 7421 }
        },
        new LevelRoundData
        {
            sentence = "Open the blue door",
            vocabulary = new[]
            {
                new Part2Script.TokenIdPair { token = "Open", id = 5108 },
                new Part2Script.TokenIdPair { token = "the", id = 464 },
                new Part2Script.TokenIdPair { token = "blue", id = 4171 },
                new Part2Script.TokenIdPair { token = "door", id = 3294 }
            },
            mappingPoolOrder = new[] { 4171, 5108, 3294, 464 },
            sequencePoolOrder = new[] { 3294, 464, 5108, 4171 }
        },
        new LevelRoundData
        {
            sentence = "Find a hidden key",
            vocabulary = new[]
            {
                new Part2Script.TokenIdPair { token = "Find", id = 9932 },
                new Part2Script.TokenIdPair { token = "a", id = 64 },
                new Part2Script.TokenIdPair { token = "hidden", id = 12552 },
                new Part2Script.TokenIdPair { token = "key", id = 2539 }
            },
            mappingPoolOrder = new[] { 2539, 12552, 64, 9932 },
            sequencePoolOrder = new[] { 12552, 2539, 9932, 64 }
        }
    };

    [HideInInspector] public string[] tokens;
    [HideInInspector] public int[] tokenIDs;
    [HideInInspector] public Vector3[] embeddings;

    private int currentRoundIndex;

    void Start()
    {
        EnsureDefaultRounds();
        currentRoundIndex = 0;

        if (startAtPart3ForTesting)
        {
            GoToPart3();
            return;
        }

        ConfigurePart1ForCurrentRound();

        Panel_Part1.SetActive(true);
        Panel_Part2.SetActive(false);
        Panel_Part3.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Alpha3))
        {
            GoToPart3();
        }
    }

    public void GoToPart2()
    {
        Part2Script part2 = GetPart2();
        if (part2 != null)
        {
            part2.ConfigureRound(GetCurrentRound());
            tokens = part2.GetTokens();
            tokenIDs = part2.GetTokenIDs();
        }

        Panel_Part1.SetActive(false);
        Panel_Part2.SetActive(true);
    }

    public void CompletePart2Round()
    {
        Part2Script part2 = GetPart2();
        if (part2 != null)
        {
            tokens = part2.GetTokens();
            tokenIDs = part2.GetTokenIDs();
        }

        currentRoundIndex++;
        if (currentRoundIndex >= rounds.Length)
        {
            GoToPart3();
            return;
        }

        ConfigurePart1ForCurrentRound();
        Panel_Part2.SetActive(false);
        Panel_Part1.SetActive(true);
    }

    public void GoToPart3()
    {
        Panel_Part1.SetActive(false);
        Panel_Part2.SetActive(false);
        Panel_Part3.SetActive(true);
    }

    public void CompleteLevel()
    {
        Debug.Log("Level1 completed!");
    }

    private LevelRoundData GetCurrentRound()
    {
        EnsureDefaultRounds();
        if (rounds == null || rounds.Length == 0)
        {
            return null;
        }

        int index = Mathf.Clamp(currentRoundIndex, 0, rounds.Length - 1);
        return rounds[index];
    }

    private Part1Script ConfigurePart1ForCurrentRound()
    {
        Part1Script part1 = GetPart1();
        LevelRoundData round = GetCurrentRound();
        if (part1 == null || round == null)
        {
            return part1;
        }

        int markerCount = round.vocabulary == null ? 0 : Mathf.Max(0, round.vocabulary.Length - 1);
        part1.ConfigureRound(round.sentence, markerCount);
        part1.RebuildNow();
        return part1;
    }

    private Part1Script GetPart1()
    {
        Part1Script part1 = Panel_Part1.GetComponent<Part1Script>();
        if (part1 == null)
        {
            part1 = Panel_Part1.GetComponentInChildren<Part1Script>(true);
        }

        if (part1 == null)
        {
            part1 = GetComponent<Part1Script>();
        }

        if (part1 == null)
        {
            part1 = FindFirstObjectByType<Part1Script>();
        }

        return part1;
    }

    private Part2Script GetPart2()
    {
        Part2Script part2 = Panel_Part2.GetComponent<Part2Script>();
        if (part2 == null)
        {
            part2 = Panel_Part2.GetComponentInChildren<Part2Script>(true);
        }

        return part2;
    }

    private void EnsureDefaultRounds()
    {
        if (rounds != null && rounds.Length > 0)
        {
            return;
        }

        rounds = new[]
        {
            new LevelRoundData
            {
                sentence = "Move the red block",
                vocabulary = new[]
                {
                    new Part2Script.TokenIdPair { token = "Move", id = 7421 },
                    new Part2Script.TokenIdPair { token = "the", id = 464 },
                    new Part2Script.TokenIdPair { token = "red", id = 3152 },
                    new Part2Script.TokenIdPair { token = "block", id = 1801 }
                },
                mappingPoolOrder = new[] { 3152, 1801, 7421, 464 },
                sequencePoolOrder = new[] { 1801, 3152, 464, 7421 }
            },
            new LevelRoundData
            {
                sentence = "Open the blue door",
                vocabulary = new[]
                {
                    new Part2Script.TokenIdPair { token = "Open", id = 5108 },
                    new Part2Script.TokenIdPair { token = "the", id = 464 },
                    new Part2Script.TokenIdPair { token = "blue", id = 4171 },
                    new Part2Script.TokenIdPair { token = "door", id = 3294 }
                },
                mappingPoolOrder = new[] { 4171, 5108, 3294, 464 },
                sequencePoolOrder = new[] { 3294, 464, 5108, 4171 }
            },
            new LevelRoundData
            {
                sentence = "Find a hidden key",
                vocabulary = new[]
                {
                    new Part2Script.TokenIdPair { token = "Find", id = 9932 },
                    new Part2Script.TokenIdPair { token = "a", id = 64 },
                    new Part2Script.TokenIdPair { token = "hidden", id = 12552 },
                    new Part2Script.TokenIdPair { token = "key", id = 2539 }
                },
                mappingPoolOrder = new[] { 2539, 12552, 64, 9932 },
                sequencePoolOrder = new[] { 12552, 2539, 9932, 64 }
            }
        };
    }
}
