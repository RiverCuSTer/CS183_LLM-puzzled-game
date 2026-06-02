using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class Level3PhysicsPlatform : MonoBehaviour
{
    [SerializeField] private Vector2 colliderSize = new Vector2(12f, 1.2f);
    [SerializeField] private Vector2 colliderOffset = new Vector2(0f, -0.35f);

    private void Awake()
    {
        BoxCollider2D box = GetComponent<BoxCollider2D>();
        box.size = colliderSize;
        box.offset = colliderOffset;
        box.sharedMaterial = new PhysicsMaterial2D("Level3_Platform_Material")
        {
            friction = 2.4f,
            bounciness = 0f
        };
    }
}
