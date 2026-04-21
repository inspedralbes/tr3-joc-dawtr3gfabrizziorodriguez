using UnityEngine;
using UnityEngine.SceneManagement;
using System;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Net.WebSockets;
using System.Collections.Generic;

[DefaultExecutionOrder(-1)]
public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("Mode Offline")]
    public bool isOfflineMode = false;

    [Header("Mode Entrenament ML-Agents")]
    [Tooltip("S'activa automaticament si hi ha BotAgents a l'escena")]
    public bool isTrainingMode = false;

    [Header("Jugadors (assignar des de l'Inspector)")]
    public GameObject player1GO;
    public GameObject player2GO;
    public GameObject player3GO;
    public GameObject player4GO;

    private GameObject[] _allPlayers;
    private int _myIndex;
    private GameObject _myPlayerGO;
    private MovementController _myMovement;

    // --- WebSocket ---
    private ClientWebSocket _ws;
    private CancellationTokenSource _cts;
    private string _lobbyId;
    public string CurrentLobbyId => _lobbyId;

    public static int[] playerLives = { 3, 3, 3, 3 };
    public static int[] playerKills = { 0, 0, 0, 0 };
    
    // Hem d'encuar missatges del WS fora del main thread per no cridar l'API de Unity
    private Queue<string> _messageQueue = new Queue<string>();
    private readonly object _queueLock = new object();

    // Sincronització
    private Vector3 _lastPos;
    private string _lastDir = "idle";
    private float _syncTimer;

    private void Awake()
    {
        if (Instance != null) {
            DestroyImmediate(gameObject);
        } else {
            Instance = this;
            if (SceneManager.GetActiveScene().name.Contains("Sol")) {
                isOfflineMode = true;
            }
            // isTrainingMode es controla des de l'Inspector de Unity.
            // Si està activat manualment, també és offline.
            if (isTrainingMode)
            {
                isOfflineMode = true;
                Debug.Log("[GameManager] isTrainingMode = true (activat manualment des de l'Inspector)");
            }
            PlayerPrefs.SetInt("LastMatchOffline", isOfflineMode ? 1 : 0);
        }
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
        _cts?.Cancel();
        if (_ws != null && _ws.State == WebSocketState.Open)
        {
            _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
            _ws.Dispose();
        }
    }

    private void Start()
    {
        _allPlayers = new GameObject[] { player1GO, player2GO, player3GO, player4GO };

        _myIndex   = PlayerPrefs.GetInt("PlayerIndex", 1);
        int maxP   = PlayerPrefs.GetInt("MaxPlayers", 4);
        _lobbyId   = PlayerPrefs.GetString("CurrentLobbyId", "");

        if (isOfflineMode) {
            maxP = 4; // Asegurarse de que aparezcan los 4 si estamos en Jugar Sol
            _myIndex = 1; // El jugador principal siempre será el P1
        }

        for (int i = 0; i < _allPlayers.Length; i++)
        {
            GameObject go = _allPlayers[i];
            if (go == null) continue;

            int playerNumber = i + 1; 

            if (playerNumber > maxP)
            {
                go.SetActive(false);
                continue;
            }

            MovementController mc = go.GetComponent<MovementController>();
            BombController bc = go.GetComponent<BombController>();

            if (playerNumber == _myIndex)
            {
                // Aquest és el jugador humà: activar control
                _myPlayerGO = go;
                _myMovement = mc;
                if (mc != null) { mc.enabled = true; mc.isBot = false; }
                if (bc != null) bc.enabled = true;
                _lastPos = go.transform.position;
            }
            else
            {
                // Bot: mantenir mc i bc ACTIVATS perquè la física i els triggers funcionin.
                // El flag isBot a MovementController i _isBot a BombController
                // ja impedeixen que llegeixin el teclat.
                if (mc != null) { mc.enabled = true; mc.isBot = true; }
                if (bc != null) bc.enabled = true;
            }
        }

        if (!isOfflineMode && !string.IsNullOrEmpty(_lobbyId))
        {
            _cts = new CancellationTokenSource();
            _ = ConnectToNetwork();
        }
    }

    private void Update()
    {
        if (isOfflineMode) return;

        // 1. Enviar moviments locals a la xarxa
        if (_myPlayerGO != null && _myMovement != null && _myMovement.enabled)
        {
            _syncTimer += Time.deltaTime;
            // Aprox 20 vegades per segon (cada 0.05s) enviarem missatges per xarxa si hi ha canvis
            if (_syncTimer >= 0.05f) 
            {
                _syncTimer = 0f;
                Vector3 currentPos = _myPlayerGO.transform.position;
                string currentDir = _myMovement.currentDirName;

                if (Vector3.Distance(currentPos, _lastPos) > 0.005f || currentDir != _lastDir)
                {
                    _lastPos = currentPos;
                    _lastDir = currentDir;
                    string xStr = _lastPos.x.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    string yStr = _lastPos.y.ToString(System.Globalization.CultureInfo.InvariantCulture);
                    
                    string json = $"{{\"type\":\"player_move\",\"playerIndex\":{_myIndex},\"x\":{xStr},\"y\":{yStr},\"dir\":\"{_lastDir}\"}}";
                    _ = SendMessageWS(json);
                }
            }
        }

        // 2. Processar tots els missatges rebuts a la xarxa amb el thread de Unity
        lock (_queueLock)
        {
            while (_messageQueue.Count > 0)
            {
                ProcessNetworkMessage(_messageQueue.Dequeue());
            }
        }
    }

    public void NotifyBombPlaced()
    {
        if (isOfflineMode) return;
        string json = $"{{\"type\":\"place_bomb\",\"playerIndex\":{_myIndex}}}";
        _ = SendMessageWS(json);
    }

    public void NotifyLocalPlayerDied(GameObject killerGO)
    {
        if (isOfflineMode) return;
        int killerIndex = -1;
        if (killerGO != null) 
        {
            for (int i = 0; i < _allPlayers.Length; i++) 
            {
                if (_allPlayers[i] == killerGO) 
                { 
                    killerIndex = i + 1; 
                    break; 
                }
            }
        }

        string json = $"{{\"type\":\"player_died\",\"victimIndex\":{_myIndex},\"killerIndex\":{killerIndex}}}";
        _ = SendMessageWS(json);
    }

    // ─── WebSocket ───────────────────────────────────────────────────────────

    private async Task ConnectToNetwork()
    {
        _ws = new ClientWebSocket();
        try
        {
            await _ws.ConnectAsync(new Uri("ws://204.168.212.178:3000"), _cts.Token);
            string joinJson = $"{{\"type\":\"game_join\",\"lobbyId\":\"{_lobbyId}\",\"playerIndex\":{_myIndex}}}";
            await SendMessageWS(joinJson);

            var buffer = new byte[4096];
            while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
            {
                var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);
                if (result.MessageType == WebSocketMessageType.Close) break;

                string raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                lock (_queueLock) _messageQueue.Enqueue(raw);
            }
        }
        catch { }
    }

    private async Task SendMessageWS(string json)
    {
        if (_ws == null || _ws.State != WebSocketState.Open) return;
        byte[] bytes = Encoding.UTF8.GetBytes(json);
        await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
    }

    // ─── Parsing i Processament de Xarxa ──────────────────────────────────────

    private void ProcessNetworkMessage(string raw)
    {
        if (raw.Contains("\"player_move\""))
        {
            int pIdx = ExtractInt(raw, "playerIndex");
            float x = ExtractFloat(raw, "x");
            float y = ExtractFloat(raw, "y");
            string dirStr = ExtractStringField(raw, "dir");

            if (pIdx > 0 && pIdx <= _allPlayers.Length && pIdx != _myIndex)
            {
                GameObject extObj = _allPlayers[pIdx - 1];
                if (extObj != null && extObj.activeSelf)
                {
                    extObj.transform.position = new Vector3(x, y, 0);
                    MovementController mc = extObj.GetComponent<MovementController>();
                    if (mc != null && !string.IsNullOrEmpty(dirStr)) mc.SetRemoteState(dirStr);
                }
            }
        }
        else if (raw.Contains("\"place_bomb\""))
        {
            int pIdx = ExtractInt(raw, "playerIndex");
            if (pIdx > 0 && pIdx <= _allPlayers.Length && pIdx != _myIndex)
            {
                BombController bc = _allPlayers[pIdx - 1].GetComponent<BombController>();
                if (bc != null) bc.RemotePlaceBomb();
            }
        }
        else if (raw.Contains("\"player_died\""))
        {
            int vIdx = ExtractInt(raw, "victimIndex");
            int kIdx = ExtractInt(raw, "killerIndex");
            
            if (vIdx > 0 && vIdx <= _allPlayers.Length && vIdx != _myIndex)
            {
                GameObject victimObj = _allPlayers[vIdx - 1];
                GameObject killerObj = (kIdx > 0 && kIdx <= _allPlayers.Length) ? _allPlayers[kIdx - 1] : null;

                if (victimObj != null && victimObj.activeSelf)
                {
                    MovementController vMc = victimObj.GetComponent<MovementController>();
                    if (vMc != null) 
                    {
                        vMc.lastKiller = killerObj;
                        vMc.RemoteDeathSequence();
                    }
                }
            }
        }
    }

    private int ExtractInt(string json, string field)
    {
        string val = ExtractStringField(json, field);
        if (int.TryParse(val, out int res)) return res;
        return 0;
    }

    private float ExtractFloat(string json, string field)
    {
        string val = ExtractStringField(json, field);
        if (float.TryParse(val, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float res)) return res;
        return 0f;
    }

    private static string ExtractStringField(string json, string field)
    {
        string key = $"\"{field}\"";
        int start = json.IndexOf(key);
        if (start < 0) return null;
        int colon = json.IndexOf(':', start + key.Length);
        if (colon < 0) return null;
        int afterColon = colon + 1;
        while (afterColon < json.Length && json[afterColon] == ' ') afterColon++;
        if (afterColon >= json.Length) return null;
        if (json[afterColon] != '"')
        {
            int end2 = afterColon;
            while (end2 < json.Length && json[end2] != ',' && json[end2] != '}') end2++;
            return json.Substring(afterColon, end2 - afterColon).Trim();
        }
        int valStart = afterColon + 1;
        int valEnd = json.IndexOf('"', valStart);
        if (valEnd < 0) return null;
        return json.Substring(valStart, valEnd - valStart);
    }

    // (ELIMINADO TOJSON PORQUE NO ERA COMPATIBLE CON ANONYMOUS TYPES EN TODAS LAS VERSIONES DE UNITY)
    
    // ─── Win State i Rondes ───────────────────────────────────────────────────

    public void OnPlayerDied(GameObject playerGO, GameObject killerGO)
    {
        // En mode entrenament: notificar kills al BotAgent però ignorar vides/escena
        if (isTrainingMode)
        {
            if (killerGO != null && killerGO != playerGO)
            {
                BotAgent killerBot = killerGO.GetComponent<BotAgent>();
                if (killerBot != null) killerBot.OnBotKill();
            }
            return;
        }

        for (int i = 0; i < _allPlayers.Length; i++)
        {
            if (_allPlayers[i] == playerGO)
            {
                playerLives[i]--;
                break;
            }
        }

        if (killerGO != null && killerGO != playerGO)
        {
            for (int i = 0; i < _allPlayers.Length; i++)
            {
                if (_allPlayers[i] == killerGO)
                {
                    playerKills[i]++;

                    // Notificar al BotAgent del killer que ha fet un kill
                    BotAgent killerBot = killerGO.GetComponent<BotAgent>();
                    if (killerBot != null) killerBot.OnBotKill();

                    break;
                }
            }
        }

        CheckWinState();
    }

    public void CheckWinState()
    {
        int aliveCount = 0;
        bool isHumanDead = false;
        for (int i = 0; i < _allPlayers.Length; i++)
        {
            if (_allPlayers[i] != null && _allPlayers[i].activeSelf) 
                aliveCount++;
            
            // Si soy yo y estoy inactivo, he muerto
            if (i == (_myIndex - 1) && (_allPlayers[i] == null || !_allPlayers[i].activeSelf))
                isHumanDead = true;
        }

        bool isGameOver = false;
        for (int i = 0; i < _allPlayers.Length; i++)
        {
            if (playerLives[i] <= 0)
            {
                isGameOver = true;
                break;
            }
        }

        if (isGameOver)
        {
            Invoke(nameof(GameOver), 3f);
        }
        else if (aliveCount <= 1 || (isOfflineMode && isHumanDead))
        {
            Invoke(nameof(NewRound), 3f);
        }
    }

    private void NewRound()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    private void GameOver()
    {
        string endJson = $"{{\"type\":\"end_game\",\"lobbyId\":\"{_lobbyId}\"}}";
        _ = SendMessageWS(endJson);

        // Las vidas y muertes se resetearán en la pantalla de Scoreboard.
        SceneManager.LoadScene("Scoreboard");
    }
}