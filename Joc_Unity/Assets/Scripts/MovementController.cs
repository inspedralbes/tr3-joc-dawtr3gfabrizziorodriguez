using UnityEngine;

public class MovementController : MonoBehaviour
{
    public new Rigidbody2D rigidbody { get; private set; }
    private Vector2 direction = Vector2.zero;
    public float speed = 5f;

    public KeyCode inputUp    = KeyCode.W;
    public KeyCode inputDown  = KeyCode.S;
    public KeyCode inputLeft  = KeyCode.A;
    public KeyCode inputRight = KeyCode.D;

    public AnimatedSpriteRenderer spriteRendererUp;
    public AnimatedSpriteRenderer spriteRendererDown;
    public AnimatedSpriteRenderer spriteRendererLeft;
    public AnimatedSpriteRenderer spriteRendererRight;
    private AnimatedSpriteRenderer activeSpriteRenderer;

    private void Awake()
    {
        rigidbody = GetComponent<Rigidbody2D>();

        if (rigidbody == null)
        {
            Debug.LogError("MovementController: NO RIGIDBODY2D en " + gameObject.name);
            return;
        }

        rigidbody.sleepMode = RigidbodySleepMode2D.NeverSleep;
        rigidbody.constraints = RigidbodyConstraints2D.FreezeRotation;
        Debug.Log("MovementController OK: " + gameObject.name);

        // Arranca con el renderer de abajo activo (idle), rest desactivados
        SetDirection(Vector2.zero, spriteRendererDown);
    }

    private void Update()
    {
        if (Input.GetKey(inputUp)) {
            SetDirection(Vector2.up, spriteRendererUp);
        } else if (Input.GetKey(inputDown)) {
            SetDirection(Vector2.down, spriteRendererDown);
        } else if (Input.GetKey(inputLeft)) {
            SetDirection(Vector2.left, spriteRendererLeft);
        } else if (Input.GetKey(inputRight)) {
            SetDirection(Vector2.right, spriteRendererRight);
        } else {
            SetDirection(Vector2.zero, activeSpriteRenderer);
        }

        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
    {
        direction = newDirection;

        // Si cambia el renderer activo, desactiva el anterior y activa el nuevo
        if (activeSpriteRenderer != spriteRenderer)
        {
            if (activeSpriteRenderer != null)
            {
                activeSpriteRenderer.idle = true;
                activeSpriteRenderer.gameObject.SetActive(false);
            }

            activeSpriteRenderer = spriteRenderer;

            if (activeSpriteRenderer != null)
                activeSpriteRenderer.gameObject.SetActive(true);
        }

        // Idle si no hay dirección, animación si la hay
        if (activeSpriteRenderer != null)
            activeSpriteRenderer.idle = (newDirection == Vector2.zero);
    }
}
