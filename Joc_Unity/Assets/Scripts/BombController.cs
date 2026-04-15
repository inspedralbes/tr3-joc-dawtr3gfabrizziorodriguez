using System.Collections;
using UnityEngine;
using UnityEngine.Tilemaps;

public class BombController : MonoBehaviour
{
    [Header("Bomb")]
    public KeyCode inputKey = KeyCode.Space;
    public GameObject bombPrefab;
    public float bombFuseTime = 3f;
    public int bombAmount = 1;
    private int bombsRemaining;

    [Header("Explosion")]
    public Explosion explosionPrefab;
    public LayerMask explosionLayerMask;
    public float explosionDuration = 1f;
    public int explosionRadius = 1;

    [Header("Destructible")]
    public Tilemap destructibleTiles;
    public Destructible destructiblePrefab;

    private GameObject _currentBomb; // Referència a la bomba activa actual

    private void OnEnable()
    {
        bombsRemaining = bombAmount;
    }

    // Cridat externament pel GameManager (si rep el packet de network)
    public void RemotePlaceBomb()
    {
        if (bombsRemaining > 0)
        {
            StartCoroutine(PlaceBomb());
        }
    }

    /// <summary>
    /// Cancel·la totes les bombes actives. Usat per BotAgent al inici de cada episodi.
    /// </summary>
    public void CancelBombs()
    {
        StopAllCoroutines();
        if (_currentBomb != null)
        {
            Destroy(_currentBomb);
            _currentBomb = null;
        }
        bombsRemaining = bombAmount;
    }

    private void Update()
    {
        if (bombsRemaining > 0 && Input.GetKeyDown(inputKey)) 
        {
            StartCoroutine(PlaceBomb());

            // Si hi ha GameManager habilitat a la mateixa scene i soc local, notificar
            if (GameManager.Instance != null && enabled)
            {
                GameManager.Instance.NotifyBombPlaced();
            }
        }
    }

    private IEnumerator PlaceBomb()
    {
        Vector2 position = transform.position;
        position.x = Mathf.Round(position.x) + 0.5f;
        position.y = Mathf.Round(position.y) + 0.5f;

        _currentBomb = Instantiate(bombPrefab, position, Quaternion.identity);
        bombsRemaining--;

        yield return new WaitForSeconds(bombFuseTime);

        position = _currentBomb != null ? _currentBomb.transform.position : (Vector2)transform.position;

        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.owner = gameObject;
        explosion.SetActiveRenderer(explosion.start);
        explosion.DestroyAfter(explosionDuration);

        Explode(position, Vector2.up, explosionRadius);
        Explode(position, Vector2.down, explosionRadius);
        Explode(position, Vector2.left, explosionRadius);
        Explode(position, Vector2.right, explosionRadius);

        if (_currentBomb != null) Destroy(_currentBomb);
        _currentBomb = null;
        bombsRemaining++;
    }

    private void Explode(Vector2 position, Vector2 direction, int length)
    {
        if (length <= 0) return;

        position += direction;

        if (Physics2D.OverlapBox(position, Vector2.one / 2f, 0f, explosionLayerMask))
        {
            ClearDestructible(position);
            return;
        }

        Explosion explosion = Instantiate(explosionPrefab, position, Quaternion.identity);
        explosion.owner = gameObject;
        explosion.SetActiveRenderer(length > 1 ? explosion.middle : explosion.end);
        explosion.SetDirection(direction);
        explosion.DestroyAfter(explosionDuration);

        Explode(position, direction, length - 1);
    }

    private void ClearDestructible(Vector2 position)
    {
        Vector3Int cell = destructibleTiles.WorldToCell(position);
        TileBase tile = destructibleTiles.GetTile(cell);

        if (tile != null)
        {
            Instantiate(destructiblePrefab, position, Quaternion.identity);
            destructibleTiles.SetTile(cell, null);
        }
    }

    public void AddBomb()
    {
        bombAmount++;
        bombsRemaining++;
    }

    private void OnTriggerExit2D(Collider2D other)
    {
        if (other.gameObject.layer == LayerMask.NameToLayer("Bomb")) {
            other.isTrigger = false;
        }
    }
}