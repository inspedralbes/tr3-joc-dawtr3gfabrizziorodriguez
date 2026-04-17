using UnityEngine;

public class MovementController : MonoBehaviour
{
    public new Rigidbody2D rigidbody { get; private set; }
    private Vector2 direction = Vector2.zero;
    public float speed = 5f;
    public GameObject lastKiller;

    // Guard síncrona per evitar múltiples morts en el mateix frame de física
    private bool _isDead = false;

    // Posició inicial per al respawn en mode entrenament
    private Vector3 _startingPosition;

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
        _startingPosition = transform.localPosition; // Guardar posició inicial

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

    public bool isBot = false;

    private void Update()
    {
        if (!enabled) return;
        if (isBot) return; // El bot es mou via SetRemoteState + FixedUpdate

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
        // NO fem transform.Translate aqui: el moviment es fa al FixedUpdate via rigidbody
    }

    private void FixedUpdate()
    {
        if (!enabled) return;
        // Tant el jugador humà com els bots mouen via rigidbody per respectar col·lisions
        if (rigidbody != null)
        {
            Vector2 newPos = rigidbody.position + direction * speed * Time.fixedDeltaTime;
            rigidbody.MovePosition(newPos);
        }
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

    private void OnEnable()
    {
        _isDead = false; // Reset al activar (respawn o inici de partida)
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (_isDead || !enabled) return;

        if (other.gameObject.layer == LayerMask.NameToLayer("Explosion")) {
            Debug.Log($"[DEBUG] {gameObject.name} tocat per explosió: {other.name}");
            // GetComponentInParent per si el collider és en un fill del prefab Explosion
            Explosion exp = other.GetComponentInParent<Explosion>();
            if (exp != null) {
                lastKiller = exp.owner;
                Debug.Log($"[DEBUG] lastKiller = {(lastKiller != null ? lastKiller.name : "NULL")}");
            } else {
                Debug.LogWarning("[MovementController] Explosió sense owner! " + other.name);
            }
            DeathSequence();
            if (GameManager.Instance != null) {
                GameManager.Instance.NotifyLocalPlayerDied(lastKiller);
            }
        }
    }

    public void RemoteDeathSequence()
    {
        DeathSequence();
    }

    private void DeathSequence()
    {
        Debug.Log($"[DEBUG] DeathSequence - {gameObject.name} - lastKiller={lastKiller?.name}");
        _isDead = true;  // Primer de tot: bloquejar qualsevol altre trigger simultani
        enabled = false;
        BombController bomb = GetComponent<BombController>();
        if (bomb != null) bomb.enabled = false;

        BotAgent bot = GetComponent<BotAgent>();
        if (bot != null)
        {
            if (lastKiller == gameObject)
                bot.MarkSelfKill();

            bot.OnBotDeath();

            // En mode ENTRENAMENT: BotAgent fa Respawn() immediatament → sortim
            // En mode NORMAL: continuem amb la seqüència de mort visual
            if (GameManager.Instance != null && GameManager.Instance.isTrainingMode)
                return;
        }

        // Seqüència de mort estàndard per jugadors humans
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
        Debug.Log($"[DEBUG] OnDeathSequenceEnded - {gameObject.name} - trainingMode={GameManager.Instance?.isTrainingMode} - killer={lastKiller?.name}");
        // En mode entrenament: respawn en lloc de desaparèixer
        if (GameManager.Instance != null && GameManager.Instance.isTrainingMode)
        {
            Debug.Log($"[DEBUG] RESPAWN {gameObject.name} a {_startingPosition}");
            transform.localPosition = _startingPosition;
            _isDead = false;
            enabled = true;
            BombController bomb = GetComponent<BombController>();
            if (bomb != null) bomb.enabled = true;
            GameManager.Instance.OnPlayerDied(gameObject, lastKiller);
            return;
        }

        gameObject.SetActive(false);
        if (GameManager.Instance != null) {
            GameManager.Instance.OnPlayerDied(gameObject, lastKiller);
        }
    }

    /// <summary>
    /// Cancel·la la seqüència de mort pendent. Usat per BotAgent al inici de cada episodi.
    /// </summary>
    public void CancelDeathSequence()
    {
        CancelInvoke(nameof(OnDeathSequenceEnded));
        if (spriteRendererDeath != null) spriteRendererDeath.gameObject.SetActive(false);
    }
}