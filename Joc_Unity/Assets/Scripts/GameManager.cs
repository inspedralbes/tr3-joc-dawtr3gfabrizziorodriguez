using UnityEngine;
using UnityEngine.SceneManagement;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Jugadors (assignar des de l'Inspector)")]
    public GameObject player1GO;
    public GameObject player2GO;
    public GameObject player3GO;
    public GameObject player4GO;

    private GameObject[] _allPlayers;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        _allPlayers = new GameObject[] { player1GO, player2GO, player3GO, player4GO };

        int myIndex  = PlayerPrefs.GetInt("PlayerIndex", 1);  // 1-4
        int maxPlayers = PlayerPrefs.GetInt("MaxPlayers", 4); // quants jugadors hi ha a la partida

        Debug.Log($"[GameManager] Soc Player{myIndex}, maxPlayers={maxPlayers}");

        for (int i = 0; i < _allPlayers.Length; i++)
        {
            GameObject go = _allPlayers[i];
            if (go == null) continue;

            int playerNumber = i + 1; // Player1=1, Player2=2...

            // Desactivar personatges que superen el màxim de jugadors
            if (playerNumber > maxPlayers)
            {
                go.SetActive(false);
                continue;
            }

            // Desactivar el MovementController i BombController dels personatges que NO soc jo
            MovementController mc = go.GetComponent<MovementController>();
            if (mc != null)
            {
                mc.enabled = (playerNumber == myIndex);
            }

            BombController bc = go.GetComponent<BombController>();
            if (bc != null)
            {
                bc.enabled = (playerNumber == myIndex);
            }
        }
    }

    public void CheckWinState()
    {
        int aliveCount = 0;
        foreach (var go in _allPlayers)
        {
            if (go != null && go.activeSelf) aliveCount++;
        }

        if (aliveCount <= 1)
            Invoke(nameof(NewRound), 3f);
    }

    private void NewRound()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}