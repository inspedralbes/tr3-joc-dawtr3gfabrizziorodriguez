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
    public AnimatedSpriteRenderer spriteRendererDeath;

    private AnimatedSpriteRenderer activeSpriteRenderer;
    public string currentDirName = "idle";

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

        if (spriteRendererUp != null) spriteRendererUp.gameObject.SetActive(false);
        if (spriteRendererLeft != null) spriteRendererLeft.gameObject.SetActive(false);
        if (spriteRendererRight != null) spriteRendererRight.gameObject.SetActive(false);
        if (spriteRendererDown != null) spriteRendererDown.gameObject.SetActive(false);
        if (spriteRendererDeath != null) spriteRendererDeath.gameObject.SetActive(false);

        SetDirection(Vector2.zero, spriteRendererDown);
    }

    public void SetRemoteState(string dirName)
    {
        currentDirName = dirName;
        if (dirName == "up") SetDirection(Vector2.up, spriteRendererUp);
        else if (dirName == "down") SetDirection(Vector2.down, spriteRendererDown);
        else if (dirName == "left") SetDirection(Vector2.left, spriteRendererLeft);
        else if (dirName == "right") SetDirection(Vector2.right, spriteRendererRight);
        else SetDirection(Vector2.zero, activeSpriteRenderer);
    }

    private void Update()
    {
        if (!enabled) return; 

        if (Input.GetKey(inputUp)) {
            SetDirection(Vector2.up, spriteRendererUp);
            currentDirName = "up";
        } else if (Input.GetKey(inputDown)) {
            SetDirection(Vector2.down, spriteRendererDown);
            currentDirName = "down";
        } else if (Input.GetKey(inputLeft)) {
            SetDirection(Vector2.left, spriteRendererLeft);
            currentDirName = "left";
        } else if (Input.GetKey(inputRight)) {
            SetDirection(Vector2.right, spriteRendererRight);
            currentDirName = "right";
        } else {
            SetDirection(Vector2.zero, activeSpriteRenderer);
            currentDirName = "idle";
        }

        transform.Translate(direction * speed * Time.deltaTime);
    }

    private void SetDirection(Vector2 newDirection, AnimatedSpriteRenderer spriteRenderer)
    {
        direction = newDirection;

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

        if (activeSpriteRenderer != null)
            activeSpriteRenderer.idle = (newDirection == Vector2.zero);
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion")) {
            DeathSequence();
        }
    }

    private void DeathSequence()
    {
        enabled = false;
        BombController bomb = GetComponent<BombController>();
        if (bomb != null) bomb.enabled = false;

        if (spriteRendererUp != null) spriteRendererUp.gameObject.SetActive(false);
        if (spriteRendererDown != null) spriteRendererDown.gameObject.SetActive(false);
        if (spriteRendererLeft != null) spriteRendererLeft.gameObject.SetActive(false);
        if (spriteRendererRight != null) spriteRendererRight.gameObject.SetActive(false);

        if (spriteRendererDeath != null)
            spriteRendererDeath.gameObject.SetActive(true);

        Invoke(nameof(OnDeathSequenceEnded), 1.25f);
    }

    private void OnDeathSequenceEnded()
    {
        gameObject.SetActive(false);
        if (GameManager.Instance != null) {
            GameManager.Instance.CheckWinState();
        }
    }   
}