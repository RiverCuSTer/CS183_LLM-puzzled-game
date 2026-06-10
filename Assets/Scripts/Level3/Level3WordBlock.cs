// Responsible team member: Zhiyan Lin, Zhiyu Huang; Description: Implements draggable 2D physics word blocks for Level 3 syntax stacking.
using UnityEngine;

[RequireComponent(typeof(Rigidbody2D), typeof(BoxCollider2D))]
public class Level3WordBlock : MonoBehaviour
{
    private const float UiScale = 1.45f;

    private const float RestingGravityScale = 0.55f;
    private const float RestingDrag = 7f;
    private const float DraggingDrag = 12f;

    private const float MaxHorizontalSpeed = 1.2f;
    private const float MaxVerticalSpeed = 3.2f;

    private const float MinX = -6.4f;
    private const float MaxX = 6.4f;
    private const float SafeMinX = -6.0f;
    private const float SafeMaxX = 6.0f;

    [SerializeField] private string word = "word";
    [SerializeField] private Level3WordRole role = Level3WordRole.Noun;
    [SerializeField] private Color blockColor = new Color(0.98f, 0.78f, 0.48f, 1f);
    [SerializeField] private Vector2 guiSize = new Vector2(132f, 44f);

    private Camera mainCamera;
    private Rigidbody2D body;
    private Vector3 dragOffset;
    private Vector2 dragTarget;
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
        body.drag = RestingDrag;
        body.angularDrag = 25f;
        body.collisionDetectionMode = CollisionDetectionMode2D.Continuous;
        body.constraints = RigidbodyConstraints2D.FreezeRotation;
        body.mass = 1.4f;

        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.isTrigger = false;
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
            dragTarget = GetMouseWorldPosition() + dragOffset;
        }
        else
        {
            EndDrag();
        }
    }

    private void FixedUpdate()
    {
        if (body == null)
            return;

        if (isDragging)
        {
            body.MovePosition(dragTarget);
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;
            return;
        }

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
            return;

        if (Mathf.Abs(mainCamera.transform.position.y - owningLayerY) > 1.5f)
            return;

        float cameraBottom = mainCamera.transform.position.y - mainCamera.orthographicSize;

        if (transform.position.y < cameraBottom - 1f)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;

            body.MovePosition(new Vector2(
                Mathf.Clamp(transform.position.x, SafeMinX, SafeMaxX),
                cameraBottom + 0.6f
            ));

            transform.rotation = Quaternion.identity;
        }

        if (transform.position.x < MinX || transform.position.x > MaxX)
        {
            body.velocity = Vector2.zero;
            body.angularVelocity = 0f;

            body.MovePosition(new Vector2(
                Mathf.Clamp(transform.position.x, SafeMinX, SafeMaxX),
                Mathf.Max(transform.position.y, cameraBottom + 0.6f)
            ));
        }

        ClampVelocity();
    }

    private void OnMouseDown()
    {
        BeginDrag();
    }

    private void OnMouseDrag()
    {
        if (!isDragging)
            return;

        dragTarget = GetMouseWorldPosition() + dragOffset;
    }

    private void OnMouseUp()
    {
        EndDrag();
    }

    private void OnGUI()
    {
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
            return;

        Vector3 screen = mainCamera.WorldToScreenPoint(transform.position);
        if (screen.z < 0f)
            return;

        Vector2 scaledSize = guiSize * UiScale;

        Rect rect = new Rect(
            screen.x - scaledSize.x * 0.5f,
            Screen.height - screen.y - scaledSize.y * 0.5f,
            scaledSize.x,
            scaledSize.y
        );

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
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        isDragging = false;

        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;
    }

    public void SnapTo(Vector3 position)
    {
        PlaceAt(position, owningLayerY);
    }

    public void PlaceAt(Vector3 position, float layerY)
    {
        if (body == null)
        {
            body = GetComponent<Rigidbody2D>();
        }

        owningLayerY = layerY;
        startPosition = position;

        position.z = 0f;

        transform.position = position;
        body.position = new Vector2(position.x, position.y);

        isDragging = false;

        body.gravityScale = RestingGravityScale;
        body.drag = RestingDrag;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;

        transform.rotation = Quaternion.identity;
    }

    private void BeginDrag()
    {
        if (isDragging)
            return;

        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
            return;

        if (Mathf.Abs(mainCamera.transform.position.y - owningLayerY) > 1.5f)
            return;

        isDragging = true;

        body.gravityScale = 0f;
        body.drag = DraggingDrag;
        body.velocity = Vector2.zero;
        body.angularVelocity = 0f;

        Vector3 mouseWorldPosition = GetMouseWorldPosition();
        dragOffset = transform.position - mouseWorldPosition;
        dragTarget = mouseWorldPosition + dragOffset;
    }

    private void EndDrag()
    {
        if (!isDragging)
            return;

        isDragging = false;

        body.gravityScale = RestingGravityScale;
        body.drag = RestingDrag;
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
        if (mainCamera == null)
        {
            mainCamera = Camera.main;
        }

        if (mainCamera == null)
        {
            return transform.position;
        }

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
