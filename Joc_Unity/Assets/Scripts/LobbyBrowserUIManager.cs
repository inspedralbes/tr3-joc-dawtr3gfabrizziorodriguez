using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

namespace GameUI 
{
    [Serializable]
    public class LobbyData { public string _id; public string lobbyName; public int maxPlayers; public int currentPlayers; public string host; public string status; }

    [Serializable]
    public class LobbyListWrapper { public List<LobbyData> lobbies; }

    [Serializable]
    public class CreateLobbyResponse { public string missatge; public string lobbyId; public string joinCode; }

    public class LobbyBrowserUIManager : MonoBehaviour
    {
        private ScrollView _lobbyList;
        private Label _loadingText;
        private Button _btnRefresh, _btnBack, _btnCreate;

        private VisualElement _createModal;
        private TextField _inputLobbyName, _inputMaxPlayers;
        private Button _btnCancelCreate, _btnConfirmCreate;

        private string _apiUrlList = "http://localhost:3000/api/games/list";
        private string _apiUrlCreate = "http://localhost:3000/api/games/create";
        private string _apiUrlJoin = "http://localhost:3000/api/games/join"; 

        private void OnEnable()
        {
            var root = GetComponent<UIDocument>()?.rootVisualElement;
            if (root == null) return;
            
            _lobbyList = root.Q<ScrollView>("LobbyList");
            _loadingText = root.Q<Label>("LoadingText");
            _btnRefresh = root.Q<Button>("BtnRefresh");
            _btnBack = root.Q<Button>("BtnBack");
            _btnCreate = root.Q<Button>("BtnCreate");

            _createModal = root.Q<VisualElement>("CreateModal");
            _inputLobbyName = root.Q<TextField>("InputLobbyName");
            _inputMaxPlayers = root.Q<TextField>("InputMaxPlayers");
            _btnCancelCreate = root.Q<Button>("BtnCancelCreate");
            _btnConfirmCreate = root.Q<Button>("BtnConfirmCreate");

            _btnBack.clicked += () => SceneManager.LoadScene("Menu");
            _btnRefresh.clicked += () => StartCoroutine(FetchLobbies());
            
            _btnCreate.clicked += () => _createModal.style.display = DisplayStyle.Flex;
            _btnCancelCreate.clicked += () => _createModal.style.display = DisplayStyle.None;
            _btnConfirmCreate.clicked += () => StartCoroutine(CreateLobbyRequest());

            StartCoroutine(FetchLobbies());
        }

        private IEnumerator JoinLobbyRequest(string lobbyId, Button clickedButton)
        {
            clickedButton.text = "Unint-se...";
            clickedButton.SetEnabled(false);

            string username = PlayerPrefs.GetString("Username", "Jugador");
            
            string json = $"{{\"lobbyId\":\"{lobbyId}\", \"username\":\"{username}\"}}";

            using (UnityWebRequest request = new UnityWebRequest(_apiUrlJoin, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success)
                {
                    Debug.Log("✅ T'has unit a la partida amb èxit!");
                    PlayerPrefs.SetString("CurrentLobbyId", lobbyId);
                    PlayerPrefs.SetInt("PlayerIndex", 0);
                    PlayerPrefs.Save();
                    
                    SceneManager.LoadScene("SalaEspera");
                }
                else
                {
                    Debug.LogError("❌ Error al unir-se: " + request.downloadHandler.text);
                    clickedButton.text = "ERROR";
                }
            }
        }

        private IEnumerator CreateLobbyRequest()
        {
            _btnConfirmCreate.text = "Creant...";
            _btnConfirmCreate.SetEnabled(false);

            string lobbyName = _inputLobbyName.value;
            int maxPlayers = 4;
            int.TryParse(_inputMaxPlayers.value, out maxPlayers);
            string creator = PlayerPrefs.GetString("Username", "Jugador");

            string json = $"{{\"lobbyName\":\"{lobbyName}\", \"maxPlayers\":{maxPlayers}, \"createdBy\":\"{creator}\"}}";

            using (UnityWebRequest request = new UnityWebRequest(_apiUrlCreate, "POST"))
            {
                byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(json);
                request.uploadHandler = new UploadHandlerRaw(bodyRaw);
                request.downloadHandler = new DownloadHandlerBuffer();
                request.SetRequestHeader("Content-Type", "application/json");

                yield return request.SendWebRequest();

                if (request.result == UnityWebRequest.Result.Success) {
                    _createModal.style.display = DisplayStyle.None; 
                    _inputLobbyName.value = ""; 
                    
                    CreateLobbyResponse responseDeServidor = JsonUtility.FromJson<CreateLobbyResponse>(request.downloadHandler.text);
                    
                    PlayerPrefs.SetString("CurrentLobbyId",   responseDeServidor.lobbyId);
                    PlayerPrefs.SetString("CurrentLobbyCode", responseDeServidor.joinCode);
                    PlayerPrefs.SetInt("PlayerIndex",         1);
                    PlayerPrefs.SetInt("MaxPlayers",          maxPlayers);
                    PlayerPrefs.Save();
                    
                    SceneManager.LoadScene("SalaEspera");
                } else {
                    Debug.LogError("❌ Error al crear: " + request.error);
                }
                _btnConfirmCreate.text = "✅ CREAR";
                _btnConfirmCreate.SetEnabled(true);
            }
        }

        private IEnumerator FetchLobbies()
        {
            _loadingText.style.display = DisplayStyle.Flex;
            _loadingText.text = "Connectant amb el servidor...";
            _lobbyList.Clear(); _lobbyList.Add(_loadingText);

            using (UnityWebRequest request = UnityWebRequest.Get(_apiUrlList))
            {
                yield return request.SendWebRequest();

                if (request.result != UnityWebRequest.Result.Success) {
                    _loadingText.text = "❌ Error al connectar amb el servidor.";
                } else {
                    _loadingText.style.display = DisplayStyle.None; 
                    string wrappedJson = "{ \"lobbies\": " + request.downloadHandler.text + "}";
                    LobbyListWrapper data = JsonUtility.FromJson<LobbyListWrapper>(wrappedJson);

                    if (data == null || data.lobbies == null || data.lobbies.Count == 0) {
                        _loadingText.text = "No hi ha partides disponibles. Crea'n una!";
                        _loadingText.style.display = DisplayStyle.Flex;
                        _lobbyList.Add(_loadingText);
                    } else {
                        foreach (var lobby in data.lobbies) _lobbyList.Add(CreateLobbyUIItem(lobby));
                    }
                }
            }
        }

        private VisualElement CreateLobbyUIItem(LobbyData lobby)
        {
            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row; row.style.justifyContent = Justify.SpaceBetween;
            row.style.alignItems = Align.Center; row.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f));
            row.style.paddingTop = 10; row.style.paddingBottom = 10; row.style.paddingLeft = 15; row.style.paddingRight = 15;
            row.style.marginBottom = 10;
            row.style.borderTopLeftRadius = 8; row.style.borderTopRightRadius = 8; row.style.borderBottomLeftRadius = 8; row.style.borderBottomRightRadius = 8;

            var infoLabel = new Label($"🎮 {lobby.lobbyName} | Host: {lobby.host} | Jugadors: {lobby.currentPlayers}/{lobby.maxPlayers}");
            infoLabel.style.color = Color.white; infoLabel.style.fontSize = 14; infoLabel.style.unityFontStyleAndWeight = FontStyle.Bold;

            row.Add(infoLabel);

            if (lobby.status == "waiting" && lobby.currentPlayers < lobby.maxPlayers)
            {
                var joinBtn = new Button();
                joinBtn.text = "UNIR-SE";
                joinBtn.style.backgroundColor = new StyleColor(new Color(0f, 0.6f, 1f));
                joinBtn.style.color = Color.white; joinBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                joinBtn.style.paddingLeft = 15; joinBtn.style.paddingRight = 15;
                joinBtn.clicked += () => StartCoroutine(JoinLobbyRequest(lobby._id, joinBtn));
                row.Add(joinBtn);
            }
            else
            {
                var tagBtn = new Button();
                tagBtn.text = lobby.status == "playing" ? "JUGANDO" : (lobby.status == "finished" ? "ACABADO" : "LLENO");
                tagBtn.SetEnabled(false);
                tagBtn.style.backgroundColor = new StyleColor(Color.gray);
                tagBtn.style.color = Color.white; tagBtn.style.unityFontStyleAndWeight = FontStyle.Bold;
                tagBtn.style.paddingLeft = 15; tagBtn.style.paddingRight = 15;
                row.Add(tagBtn);
            }

            return row;
        }
    }
}