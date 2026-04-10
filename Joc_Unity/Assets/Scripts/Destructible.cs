using UnityEngine;

public class Destructible : MonoBehaviour
{
    public float destructionTime = 1f;
    [Range(0f, 1f)]
    public float itemSpawnChance = 0.2f;
    public GameObject[] spawnableItems;

    private void Start()
    {
        Destroy(gameObject, destructionTime);
    }

    private void OnDestroy()
    {
        if (spawnableItems.Length > 0)
        {
            int seed = 0;
            if (GameManager.Instance != null && !string.IsNullOrEmpty(GameManager.Instance.CurrentLobbyId)) {
                seed = GameManager.Instance.CurrentLobbyId.GetHashCode();
            }
            seed ^= (int)(transform.position.x * 100) ^ (int)(transform.position.y * 10000);
            
            System.Random prng = new System.Random(seed);

            if (prng.NextDouble() < itemSpawnChance)
            {
                int randomIndex = prng.Next(0, spawnableItems.Length);
                Instantiate(spawnableItems[randomIndex], transform.position, Quaternion.identity);
            }
        }
    }

}