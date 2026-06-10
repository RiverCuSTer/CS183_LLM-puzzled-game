// Responsible team member: Zhiyan Lin; Description: Implements draggable semantic scene pieces constrained to their Level 3 layer.
using UnityEngine;

public class Level3SemanticPiece : MonoBehaviour
{
    [SerializeField] private string pieceId = "asset";
    [SerializeField] private Vector2 guiSize = new Vector2(105f, 48f);
    [SerializeField] private float uiScale = 1.35f;
    [SerializeField] private Sprite imageSprite;
    [SerializeField] private Color imageTint = Color.white;

    private Camera mainCamera;
    private Vector3 dragOffset;
    private Vector3 startPosition;
    private float owningLayerY;
    private bool isDragging;

    public string PieceId => pieceId;
    public Vector3 StartPosition => startPosition;

    private void Awake()
    {
        mainCamera = Camera.main;
        startPosition = transform.position;
        owningLayerY = Mathf.Round(startPosition.y / 12f) * 12f;
    }

    private void Update()
    {
        if (!isDragging)
            return;

        if (Input.GetMouseButton(0))
        {
            transform.position = ClampToLayer(GetMouseWorldPosition() + dragOffset);
            return;
        }

        isDragging = false;
    }

    private void OnGUI()
    {
        if (mainCamera == null)
            mainCamera = Camera.main;

        if (mainCamera == null || Mathf.Abs(mainCamera.transform.position.y - owningLayerY) > 1.5f)
            return;

        Rect rect = GetScreenRect();
        DrawImage(rect);

        Event current = Event.current;
        if (current.type == EventType.MouseDown && current.button == 0 && rect.Contains(current.mousePosition))
        {
            BeginDrag();
            current.Use();
        }

        if (current.type == EventType.MouseUp && isDragging)
        {
            isDragging = false;
            current.Use();
        }
    }

    public void ResetToStart()
    {
        transform.position = startPosition;
        isDragging = false;
    }

    public void ConfigureInteractionSize(Vector2 newGuiSize, float newUiScale)
    {
        guiSize = newGuiSize;
        uiScale = newUiScale;
    }

    public void ConfigureImage(Sprite newImageSprite, Color newImageTint)
    {
        imageSprite = newImageSprite;
        imageTint = newImageTint;
    }

    public void CaptureStartPosition()
    {
        startPosition = transform.position;
        owningLayerY = Mathf.Round(startPosition.y / 12f) * 12f;
    }

    private void BeginDrag()
    {
        isDragging = true;
        dragOffset = transform.position - GetMouseWorldPosition();
    }

    private Rect GetScreenRect()
    {
        Vector3 screen = mainCamera.WorldToScreenPoint(transform.position);
        Vector2 scaledSize = guiSize * uiScale;
        return new Rect(screen.x - scaledSize.x * 0.5f, Screen.height - screen.y - scaledSize.y * 0.5f, scaledSize.x, scaledSize.y);
    }

    private void DrawImage(Rect rect)
    {
        if (imageSprite == null || imageSprite.texture == null)
            return;

        int previousDepth = GUI.depth;
        Color previousColor = GUI.color;

        GUI.depth = -20;
        GUI.color = imageTint;

        Rect textureRect = imageSprite.textureRect;
        Rect texCoords = new Rect(
            textureRect.x / imageSprite.texture.width,
            textureRect.y / imageSprite.texture.height,
            textureRect.width / imageSprite.texture.width,
            textureRect.height / imageSprite.texture.height
        );

        GUI.DrawTextureWithTexCoords(rect, imageSprite.texture, texCoords, true);

        GUI.color = previousColor;
        GUI.depth = previousDepth;
    }

    private Vector3 GetMouseWorldPosition()
    {
        Vector3 mouse = Input.mousePosition;
        mouse.z = -mainCamera.transform.position.z;
        Vector3 world = mainCamera.ScreenToWorldPoint(mouse);
        world.z = 0f;
        return world;
    }

    private Vector3 ClampToLayer(Vector3 position)
    {
        position.x = Mathf.Clamp(position.x, -4.7f, 4.7f);
        position.y = Mathf.Clamp(position.y, owningLayerY - 4.15f, owningLayerY + 3.25f);
        position.z = 0f;
        return position;
    }
}
