using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace GameUI
{
    public class WaitRoomUIManager : MonoBehaviour
    {
        private ClientWebSocket _ws;
        private Label _statusLabel;
        private Label _lblLobbyCode;
        private ScrollView _playersList;
        private Button _btnLeave;
        private Button _btnStartGame;

        private string _lobbyId;
        private string _lobbyCode;
        private string _username;
        private int    _maxPlayers;

        // Cues per creuar fils
        private string _statusToSet       = "";
        private bool   _needsStatusUpdate = false;
        private Queue<string> _playersToAdd = new Queue<string>();
        private bool   _startGameNow      = false;
        private int    _startMaxPlayers   = 0;

        private CancellationTokenSource _cts;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root != null)
            {
                _statusLabel  = root.Q<Label>("StatusText");
                _lblLobbyCode = root.Q<Label>("LblLobbyCode");
                _playersList  = root.Q<ScrollView>("PlayersList");
                _btnLeave     = root.Q<Button>("BtnLeave");
                _btnStartGame = root.Q<Button>("BtnStartGame");

                if (_btnLeave     != null) _btnLeave.clicked     += LeaveRoom;
                if (_btnStartGame != null) _btnStartGame.clicked += StartGame;
            }

            _lobbyId    = PlayerPrefs.GetString("CurrentLobbyId",   "");
            _lobbyCode  = PlayerPrefs.GetString("CurrentLobbyCode", "---");
            _username   = PlayerPrefs.GetString("Username",         "Jugador");
            _maxPlayers = PlayerPrefs.GetInt   ("MaxPlayers",       4);

            if (_lblLobbyCode != null)
                _lblLobbyCode.text = _lobbyCode;

            if (string.IsNullOrEmpty(_lobbyId))
            {
                Debug.LogError("No tenim ID de partida!");
                return;
            }

            _cts = new CancellationTokenSource();
            _ = ConnectAndListen();
        }

        private void Update()
        {
            if (_needsStatusUpdate)
            {
                if (_statusLabel != null) _statusLabel.text = _statusToSet;
                _needsStatusUpdate = false;
            }

            while (_playersToAdd.Count > 0)
                AddPlayerToUI(_playersToAdd.Dequeue());

            if (_startGameNow)
            {
                _startGameNow = false;
                PlayerPrefs.SetInt("MaxPlayers", _startMaxPlayers);
                PlayerPrefs.Save();
                SceneManager.LoadScene("Partida");
            }
        }

        // ─── WebSocket ───────────────────────────────────────────────────

        private async Task ConnectAndListen()
        {
            _ws = new ClientWebSocket();
            try
            {
                await _ws.ConnectAsync(new Uri("ws://localhost:3000"), _cts.Token);

                _statusToSet       = "🟢 Connectat i escoltant jugadors...";
                _needsStatusUpdate = true;

                // Enviar join_room incloent maxPlayers perquè el servidor el conegui
                await SendMessage(new {
                    type       = "join_room",
                    lobbyId    = _lobbyId,
                    username   = _username,
                    maxPlayers = _maxPlayers
                });

                var buffer = new byte[4096];
                while (_ws.State == WebSocketState.Open && !_cts.Token.IsCancellationRequested)
                {
                    var result = await _ws.ReceiveAsync(new ArraySegment<byte>(buffer), _cts.Token);

                    if (result.MessageType == WebSocketMessageType.Close)
                    {
                        _statusToSet       = "🔴 Desconnectat";
                        _needsStatusUpdate = true;
                        break;
                    }

                    string raw = Encoding.UTF8.GetString(buffer, 0, result.Count);
                    HandleMessage(raw);
                }
            }
            catch (OperationCanceledException) { }
            catch (Exception ex)
            {
                _statusToSet       = "🔴 Error de connexió";
                _needsStatusUpdate = true;
                Debug.LogError("WebSocket error: " + ex.Message);
            }
        }

        private void HandleMessage(string raw)
        {
            // { "type": "you_are", "index": 2 }
            if (raw.Contains("\"you_are\""))
            {
                string indexStr = ExtractStringField(raw, "index");
                if (!string.IsNullOrEmpty(indexStr) && int.TryParse(indexStr, out int idx))
                {
                    PlayerPrefs.SetInt("PlayerIndex", idx);
                    PlayerPrefs.Save();
                    Debug.Log($"🎮 Soc el Player {idx}");
                }
            }
            // { "type": "player_joined", "username": "Danilo", "index": 2 }
            else if (raw.Contains("\"player_joined\""))
            {
                string username = ExtractStringField(raw, "username");
                string idx      = ExtractStringField(raw, "index");
                if (!string.IsNullOrEmpty(username))
                {
                    _playersToAdd.Enqueue($"[P{idx}] {username}");
                    Debug.Log($"👤 NOU JUGADOR P{idx}: {username}");
                }
            }
            // { "type": "game_started", "maxPlayers": 3 }
            else if (raw.Contains("\"game_started\""))
            {
                string mpStr = ExtractStringField(raw, "maxPlayers");
                int.TryParse(mpStr, out _startMaxPlayers);
                if (_startMaxPlayers < 1) _startMaxPlayers = _maxPlayers;
                Debug.Log($"🚀 COMENCEM LA PARTIDA! maxPlayers={_startMaxPlayers}");
                _startGameNow = true;
            }
        }

        private async Task SendMessage(object data)
        {
            if (_ws == null || _ws.State != WebSocketState.Open) return;
            string json  = ToJson(data);
            byte[] bytes = Encoding.UTF8.GetBytes(json);
            await _ws.SendAsync(new ArraySegment<byte>(bytes), WebSocketMessageType.Text, true, _cts.Token);
        }

        // ─── Botons ──────────────────────────────────────────────────────

        private void StartGame()
        {
            _ = SendMessage(new { type = "start_game", lobbyId = _lobbyId });
        }

        private void LeaveRoom()
        {
            _cts?.Cancel();
            SceneManager.LoadScene("LobbyBrowser");
        }

        // ─── UI ──────────────────────────────────────────────────────────

        private void AddPlayerToUI(string playerName)
        {
            if (_playersList == null) return;

            var row = new VisualElement();
            row.style.flexDirection    = FlexDirection.Row;
            row.style.alignItems       = Align.Center;
            row.style.backgroundColor  = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            row.style.paddingTop       = 10; row.style.paddingBottom = 10;
            row.style.paddingLeft      = 15; row.style.marginBottom  = 10;
            row.style.borderTopLeftRadius    = 5; row.style.borderTopRightRadius    = 5;
            row.style.borderBottomLeftRadius = 5; row.style.borderBottomRightRadius = 5;

            var lbl = new Label($"🎮 {playerName}");
            lbl.style.color                   = Color.white;
            lbl.style.fontSize                = 18;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(lbl);
            _playersList.Add(row);
        }

        // ─── Lifecycle ───────────────────────────────────────────────────

        private void OnDestroy()
        {
            _cts?.Cancel();
            if (_ws != null)
            {
                if (_ws.State == WebSocketState.Open)
                    _ws.CloseAsync(WebSocketCloseStatus.NormalClosure, "bye", CancellationToken.None);
                _ws.Dispose();
            }
        }

        // ─── Helpers JSON ────────────────────────────────────────────────

        private static string ToJson(object obj)
        {
            var sb = new StringBuilder("{");
            foreach (var prop in obj.GetType().GetProperties())
                sb.Append($"\"{prop.Name}\":\"{prop.GetValue(obj)}\",");
            if (sb[sb.Length - 1] == ',') sb.Length--;
            sb.Append('}');
            return sb.ToString();
        }

        private static string ExtractStringField(string json, string field)
        {
            string key   = $"\"{field}\"";
            int    start = json.IndexOf(key);
            if (start < 0) return null;
            // Saltar el separador : i possibles espais/cometes
            int colon = json.IndexOf(':', start + key.Length);
            if (colon < 0) return null;
            int afterColon = colon + 1;
            while (afterColon < json.Length && json[afterColon] == ' ') afterColon++;
            if (afterColon >= json.Length) return null;
            // Valor numèric (sense cometes)
            if (json[afterColon] != '"')
            {
                int end2 = afterColon;
                while (end2 < json.Length && json[end2] != ',' && json[end2] != '}') end2++;
                return json.Substring(afterColon, end2 - afterColon).Trim();
            }
            // Valor string (amb cometes)
            int valStart = afterColon + 1;
            int valEnd   = json.IndexOf('"', valStart);
            if (valEnd < 0) return null;
            return json.Substring(valStart, valEnd - valStart);
        }
    }
}
