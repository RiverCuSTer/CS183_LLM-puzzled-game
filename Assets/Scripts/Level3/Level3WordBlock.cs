using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Level3WordBlock : MonoBehaviour
{
    private const float UiScale = 1.45f;
    private const float RestingGravityScale = 0.55f;
    private const float MaxHorizontalSpeed = 1.2f;
    private const float MaxVerticalSpeed = 3.2f;

    [SerializeField] private string word = "word";
    [SerializeField] private Level3WordRole role = Level3WordRole.Noun;
    [SerializeField] private Color blockColor = new Color(0.98f, 0.78f, 0.48f, 1f);
    [SerializeField] private Vector2 guiSize = new Vector2(132f, 44f);

    private Camera mainCamera;
    private Rigidbody2D body;
    private Vector3 dragOffset;
    private Vector3 startPosition;
    private float owningLayerY;
    private bool isDragging;

    public string Word => word;
    public Level3WordRole Role => role;

    private void Awake()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        owningLayerY = Mathf.Round(startPosition.y / 12f) * 12f;
        body = GetComponent<Rigidbody2D>();
        body.gravityScale = RestingGravityScale;
        body.drag = 7f;
        body.angularDrag = 25f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.mass = 1.4f;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.size = new Vector2(1.65f, 0.55f);
        PhysicsMaterial2D material = new PhysicsMaterial2D("Level3_Block_Material")
        {
            friction = 1.15f,
            bounciness = 0f
        };
        box.sharedMaterial = material;
    }

    private void Update()
    {
        if (!isDragging)
            return;

        if (Input.GetMouseButton(0))
        {
            body.MovePosition(GetMouseWorldPosition() + dragOffset);
            return;
        }

        EndDrag();
    }

    private void OnMouseDown()
    {
        BeginDrag();
    }

    private void OnMouseDrag()
    {
        if (!isDragging)
            return;

        body.MovePosition(GetMouseWorldPosition() + dragOffset);
    }

    private void OnMouseUp()
    {
        EndDrag();
    }

    private void OnGUI()
    {
        if (mainCamera == null)
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(transform.position);
        if (screen.z < 0f)
            return;

        Vector2 scaledSize = guiSize * UiScale;
        Rect rect = new Rect(screen.x - scaledSize.x * 0.5f, Screen.height - screen.y - scaledSize.y * 0.5f, scaledSize.x, scaledSize.y);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
        {
            BeginDrag();
            current.Use();
        }

        Color previous = GUI.color;
        GUI.color = blockColor;
        GUI.Box(rect, GUIContent.none);
        GUI.color = previous;

        GUIStyle style = new GUIStyle(GUI.skin.label)
        {
            alignment = TextAnchor.MiddleCenter,
            fontSize = Mathf.RoundToInt(18 * UiScale),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.black }
        };

        GUI.Label(rect, word, style);
    }

    private void FixedUpdate()
    {
        if (mainCamera == null || isDragging)
            return;

        if (Mathf.Abs(mainCamera.transform.position.y - owningLayerY) > 1.5f)
            return;

        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;
        if (transform.position.y < cameraBottom - 1f)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.MovePosition(new Vector2(Mathf.Clamp(transform.position.x, -4.7f, 4.7f), cameraBottom + 0.6f));
            transform.rotation = Quaternion.identity;
        }

        if (Mathf.Abs(transform.position.x) > 5.4f)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            body.MovePosition(new Vector2(Mathf.Clamp(transform.position.x, -4.7f, 4.7f), Mathf.Max(transform.position.y, cameraBottom + 0.6f)));
        }

        ClampVelocity();
    }

    private void OnCollisionEnter2D(Collision2D collision)
    {
        ClampVelocity();
    }

    private void OnCollisionStay2D(Collision2D collision)
    {
        ClampVelocity();
    }

    public void StopMotion()
    {
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        isDragging = false;
    }

    public void SnapTo(Vector3 position)
    {
        StopMotion();
    }

    private void BeginDrag()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || Mathf.Abs(mainCamera.transform.position.y - owningLayerY) > 1.5f)
            return;

        isDragging = true;
        body.gravityScale = 0f;
        body.drag = 12f;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
        dragOffset = transform.position - GetMouseWorldPosition();
    }

    private void EndDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;
        body.gravityScale = RestingGravityScale;
        body.drag = 7f;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    private void ClampVelocity()
    {
        if (body == null)
            return;

        Vector2 velocity = body.velocity;
        velocity.x = Mathf.Clamp(velocity.x, -MaxHorizontalSpeed, MaxHorizontalSpeed);
        velocity.y = Mathf.Clamp(velocity.y, -MaxVerticalSpeed, MaxVerticalSpeed);
        body.velocity = velocity;
        body.angularVelocity = 0f;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }
}

public enum Level3WordRole
{
    Noun,
    Verb,
    Modifier,
    Preposition,
    Article
}
