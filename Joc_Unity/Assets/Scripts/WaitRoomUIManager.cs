using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using SocketIOClient;

namespace GameUI
{
    public class WaitRoomUIManager : MonoBehaviour
    {
        private SocketIO client;
        private Label _statusLabel;
        private Label _lblLobbyCode;
        private ScrollView _playersList;
        private Button _btnLeave;
        private Button _btnStartGame;

        private string _lobbyId;
        private string _lobbyCode;
        private string _username;
        
        // Cues per creuar fils
        private string _statusToSet = "";
        private bool _needsStatusUpdate = false;
        private Queue<string> _playersToAdd = new Queue<string>();
        private bool _startGameNow = false;

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root != null)
            {
                _statusLabel = root.Q<Label>("StatusText");
                _lblLobbyCode = root.Q<Label>("LblLobbyCode");
                _playersList = root.Q<ScrollView>("PlayersList");
                _btnLeave = root.Q<Button>("BtnLeave");
                _btnStartGame = root.Q<Button>("BtnStartGame");

                if (_btnLeave != null) _btnLeave.clicked += LeaveRoom;
                if (_btnStartGame != null) _btnStartGame.clicked += StartGame;
            }

            _lobbyId = PlayerPrefs.GetString("CurrentLobbyId", "");
            _lobbyCode = PlayerPrefs.GetString("CurrentLobbyCode", "---");

            if (_lblLobbyCode != null)
            {
                _lblLobbyCode.text = _lobbyCode;
            }

            _username = PlayerPrefs.GetString("Username", "Fabrizzio_Convidat");

            if (string.IsNullOrEmpty(_lobbyId))
            {
                Debug.LogError("No tenim ID de partida!");
                return;
            }

            ConnectToServer();
        }

        private void Update()
        {
            if (_needsStatusUpdate)
            {
                if (_statusLabel != null) _statusLabel.text = _statusToSet;
                _needsStatusUpdate = false;
            }

            while (_playersToAdd.Count > 0)
            {
                string playerName = _playersToAdd.Dequeue();
                AddPlayerToUI(playerName);
            }

            if (_startGameNow)
            {
                _startGameNow = false;
                SceneManager.LoadScene("Partida");
            }
        }

        private void AddPlayerToUI(string playerName)
        {
            if (_playersList == null) return;

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            row.style.paddingTop = 10; row.style.paddingBottom = 10;
            row.style.paddingLeft = 15; row.style.marginBottom = 10;
            row.style.borderTopLeftRadius = 5; row.style.borderTopRightRadius = 5;
            row.style.borderBottomLeftRadius = 5; row.style.borderBottomRightRadius = 5;

            var lbl = new Label($"🎮 JUGADOR: {playerName}");
            lbl.style.color = Color.white;
            lbl.style.fontSize = 18;
            lbl.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(lbl);
            _playersList.Add(row);
        }

        private async void ConnectToServer()
        {
            Debug.Log("Intentant connectar al Socket.IO...");
            client = new SocketIO("http://localhost:3000", new SocketIOOptions
            {
                Transport = SocketIOClient.Transport.TransportProtocol.WebSocket
            });

            client.OnConnected += (sender, e) =>
            {
                Debug.Log("⚡ Connectat al servidor en temps real!");
                _statusToSet = "🟢 Connectat i escoltant jugadors...";
                _needsStatusUpdate = true;

                var joinData = new Dictionary<string, string> { { "lobbyId", _lobbyId }, { "username", _username } };
                client.EmitAsync("join_room", joinData);
            };

            client.OnDisconnected += (sender, e) =>
            {
                _statusToSet = "🔴 Desconnectat";
                _needsStatusUpdate = true;
            };

            client.On("player_joined", response =>
            {
                // Extraiem les dades del jugador enviat per NodeJS
                var data = response.GetValue<Dictionary<string, string>>();
                if (data != null && data.ContainsKey("username"))
                {
                    _playersToAdd.Enqueue(data["username"]);
                    Debug.Log($"👤 NOU JUGADOR: {data["username"]}");
                }
            });

            client.On("game_started", response => 
            {
                Debug.Log("🚀 COMENCEM LA PARTIDA!");
                _startGameNow = true;
            });

            try 
            {
                await client.ConnectAsync();
            }
            catch (Exception ex)
            {
                Debug.LogError("Error connectant per Socket: " + ex.Message);
            }
        }

        private void StartGame()
        {
            if (client != null && client.Connected)
            {
                var startData = new Dictionary<string, string> { { "lobbyId", _lobbyId } };
                client.EmitAsync("start_game", startData);
            }
        }

        private void LeaveRoom()
        {
            if (client != null && client.Connected)
            {
                client.DisconnectAsync();
            }
            SceneManager.LoadScene("LobbyBrowser");
        }

        private void OnDestroy()
        {
            if (client != null)
            {
                client.DisconnectAsync();
                client.Dispose();
            }
        }
    }
}
