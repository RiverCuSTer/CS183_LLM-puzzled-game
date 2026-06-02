using UnityEngine;

public class Level3Layer3Manager : MonoBehaviour
{
    private const float UiScale = 3f;

    [SerializeField] private Vector2 sceneCenter = new Vector2(0f, 23.85f);
    [SerializeField] private Vector2 sceneHalfSize = new Vector2(4.1f, 1.9f);

    public static Level3Layer3Manager Instance { get; private set; }

    private Level3SemanticPiece[] pieces;
    private string feedback = "";
    private float feedbackUntil;

    public bool IsComplete
    {
        get
        {
            return CountSolvedConditions(out int total) == total && total > 0;
        }
    }

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        EnsurePieces();
        ApplyPresentationLayout();
    }

    private void OnGUI()
    {
        Camera camera = Camera.main;
        if (camera == null || Mathf.Abs(camera.transform.position.y - 24f) > 1.5f)
            return;

        int solved = CountSolvedConditions(out int total);

        GUIStyle countStyle = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(13 * UiScale),
            normal = { textColor = Color.black }
        };
        GUI.Label(new Rect(Screen.width * 0.5f - 480f, Screen.height - 96f, 960f, 72f), $"{solved}/{total} scene relations", countStyle);

        if (IsComplete)
        {
            GUIStyle doneStyle = new GUIStyle(GUI.skin.label)
            {
                alignment = TextAnchor.MiddleCenter,
                fontSize = Mathf.RoundToInt(15 * UiScale),
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.1f, 0.45f, 0.18f) }
            };
            GUI.Label(new Rect(Screen.width * 0.5f - 840f, Screen.height - 180f, 1680f, 90f), "Scene meaning complete. Climb to the next layer.", doneStyle);
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
        feedback = "Place the scientist, table, and cat to match the sentence meaning.";
        feedbackUntil = Time.time + 2.5f;
    }

    private int CountSolvedConditions(out int total)
    {
        EnsurePieces();
        total = 5;

        Level3SemanticPiece scientist = FindPiece("scientist");
        Level3SemanticPiece cat = FindPiece("cat");
        Level3SemanticPiece dog = FindPiece("dog");
        Level3SemanticPiece table = FindPiece("table");
        Level3SemanticPiece carpet = FindPiece("carpet");

        int solved = 0;
        if (scientist != null && table != null && IsInScene(scientist) && scientist.transform.position.x < table.transform.position.x - 0.75f)
            solved++;

        if (table != null && IsInScene(table))
            solved++;

        if (cat != null && table != null && IsInScene(cat) && cat.transform.position.y < table.transform.position.y - 0.45f && Mathf.Abs(cat.transform.position.x - table.transform.position.x) < 1.45f)
            solved++;

        if (dog != null && !IsInScene(dog))
            solved++;

        if (carpet != null && !IsInScene(carpet))
            solved++;

        return solved;
    }

    private bool IsInScene(Level3SemanticPiece piece)
    {
        Vector3 position = piece.transform.position;
        return Mathf.Abs(position.x - sceneCenter.x) <= sceneHalfSize.x && Mathf.Abs(position.y - sceneCenter.y) <= sceneHalfSize.y;
    }

    private Level3SemanticPiece FindPiece(string pieceId)
    {
        foreach (Level3SemanticPiece piece in pieces)
        {
            if (piece != null && piece.PieceId == pieceId)
                return piece;
        }

        return null;
    }

    private void EnsurePieces()
    {
        if (pieces == null || pieces.Length == 0)
            pieces = FindObjectsOfType<Level3SemanticPiece>();
    }

    private void ApplyPresentationLayout()
    {
        ConfigureLabel("L3_Sentence", new Vector3(0f, 25.95f, 0f), "The careful scientist gently placed the cat under the table.", new Color(1f, 1f, 1f, 0.74f), new Vector2(760f, 58f), 22, true, 1.25f);
        ConfigureLabel("L3_SemanticWhiteArea", new Vector3(0f, 23.85f, 0f), "", new Color(1f, 1f, 1f, 0.9f), new Vector2(470f, 240f), 12, false, 1.8f);

        LayoutPiece("scientist", -4.25f);
        LayoutPiece("cat", -2.12f);
        LayoutPiece("dog", 0f);
        LayoutPiece("table", 2.12f);
        LayoutPiece("carpet", 4.25f);
    }

    private void ConfigureLabel(string objectName, Vector3 worldPosition, string label, Color panelColor, Vector2 guiSize, int fontSize, bool bold, float uiScale)
    {
        Transform target = transform.Find(objectName);
        if (target == null)
            return;

        target.position = worldPosition;
        Level3SceneLabel sceneLabel = target.GetComponent<Level3SceneLabel>();
        if (sceneLabel != null)
            sceneLabel.Configure(label, panelColor, guiSize, fontSize, bold, uiScale);
    }

    private void LayoutPiece(string pieceId, float x)
    {
        Level3SemanticPiece piece = FindPiece(pieceId);
        if (piece == null)
            return;

        piece.transform.position = new Vector3(x, 20.1f, 0f);
        piece.ConfigureInteractionSize(new Vector2(90f, 90f), 2.025f);
        piece.CaptureStartPosition();

        Level3SceneLabel sceneLabel = piece.GetComponent<Level3SceneLabel>();
        if (sceneLabel != null)
            sceneLabel.enabled = false;
    }

    private Color GetPieceColor(string pieceId)
    {
        switch (pieceId)
        {
            case "scientist":
                return new Color(0.84f, 0.77f, 0.93f, 1f);
            case "cat":
                return new Color(0.98f, 0.82f, 0.58f, 1f);
            case "dog":
                return new Color(0.85f, 0.64f, 0.44f, 1f);
            case "table":
                return new Color(0.64f, 0.49f, 0.36f, 1f);
            case "carpet":
                return new Color(0.59f, 0.82f, 0.88f, 1f);
            default:
                return Color.white;
        }
    }
}
