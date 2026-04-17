using System;
using System.Collections;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement; // <-- 1. LIBRERÍA AÑADIDA PER A CANVIAR D'ESCENA

namespace Auth
{
    // DTO per enviar les dades d'autenticació al Node.js JSON Backend
    [Serializable]
    public class AuthData
    {
        public string username;
        public string password;
    }
    
    // DTO per rebre la resposta
    [Serializable]
    public class AuthResponse
    {
        public string message;
        public string token; // Opcional
    }

    public class AuthUIManager : MonoBehaviour
    {
        [Header("Configuració Xarxa")]
        [SerializeField] private string backendUrl = "http://204.168.212.178:3000/api/auth";

        [Header("Configuració UI")]
        [SerializeField] private UIDocument uiDocument;

        private TextField usernameInput;
        private TextField passwordInput;
        private Button loginButton;
        private Button registerButton;
        private Label statusLabel;

        private void OnEnable()
        {
            if (uiDocument == null)
            {
                uiDocument = GetComponent<UIDocument>();
            }

            var root = uiDocument?.rootVisualElement;
            if (root == null)
            {
                Debug.LogError("UIDocument no està assignat o no té un rootVisualElement.");
                return;
            }

            // Buscar els elements per el seu nom (name="...")
            usernameInput = root.Q<TextField>("username-input");
            passwordInput = root.Q<TextField>("password-input");
            loginButton = root.Q<Button>("login-button");
            registerButton = root.Q<Button>("register-button");
            statusLabel = root.Q<Label>("status-label");

            // Subscriure events dels botons
            if (loginButton != null) loginButton.clicked += HandleLoginClick;
            if (registerButton != null) registerButton.clicked += HandleRegisterClick;
        }

        private void OnDisable()
        {
            // Desubscriure per rebaixar pèrdues de memòria
            if (loginButton != null) loginButton.clicked -= HandleLoginClick;
            if (registerButton != null) registerButton.clicked -= HandleRegisterClick;
        }

        private void HandleLoginClick()
        {
            TryAuth("/login");
        }

        private void HandleRegisterClick()
        {
            TryAuth("/register");
        }

        private void TryAuth(string endpoint)
        {
            string user = usernameInput?.value;
            string pass = passwordInput?.value;

            // Simple validació abans d'enviar res al servidor
            if (string.IsNullOrEmpty(user) || string.IsNullOrEmpty(pass))
            {
                UpdateStatus("Usuari i contrasenya són obligatoris.", Color.red);
                return;
            }

            // Desactivar UI temporalment
            SetUIEnabled(false);
            UpdateStatus("Connectant...", Color.yellow);

            StartCoroutine(SendAuthRequest(endpoint, user, pass));
        }

        private IEnumerator SendAuthRequest(string endpoint, string username, string password)
        {
            string url = backendUrl + endpoint;
            
            AuthData data = new AuthData { username = username, password = password };
            string jsonData = JsonUtility.ToJson(data);

            using (UnityWebRequest webRequest = new UnityWebRequest(url, "POST"))
            {
                byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonData);
                
                // Utilitzar UploadHandlerRaw per enviar JSON pur
                webRequest.uploadHandler = new UploadHandlerRaw(bodyRaw);
                webRequest.downloadHandler = new DownloadHandlerBuffer();
                webRequest.SetRequestHeader("Content-Type", "application/json");
                webRequest.timeout = 10; // Timeout de 10 segons

                // Enviar la petició i esperar
                yield return webRequest.SendWebRequest();

                // Tornar a activar UI
                SetUIEnabled(true);

                if (webRequest.result == UnityWebRequest.Result.ConnectionError)
                {
                    UpdateStatus("Error de connexió al servidor.", Color.red);
                }
                else if (webRequest.result == UnityWebRequest.Result.ProtocolError)
                {
                    // Error d'autenticació (ex. 401 Unauthorized) o dades incorrectes (400)
                    string errorMsg = "S'ha produït un error.";
                    
                    if (!string.IsNullOrEmpty(webRequest.downloadHandler.text))
                    {
                        try
                        {
                            AuthResponse errorResponse = JsonUtility.FromJson<AuthResponse>(webRequest.downloadHandler.text);
                            if (errorResponse != null && !string.IsNullOrEmpty(errorResponse.message))
                            {
                                errorMsg = errorResponse.message;
                            }
                        }
                        catch
                        {
                            errorMsg = webRequest.error;
                        }
                    }
                    
                    UpdateStatus($"Error: {errorMsg}", Color.red);
                }
                else
                {
                    // Tot ha anat bé (200 OK, 201 Created)
                    string successMsg = "Operació exitosa!";
                    try
                    {
                        AuthResponse successResponse = JsonUtility.FromJson<AuthResponse>(webRequest.downloadHandler.text);
                        if (successResponse != null && !string.IsNullOrEmpty(successResponse.message))
                        {
                            successMsg = successResponse.message;
                        }
                    }
                    catch { }

                    UpdateStatus(successMsg, Color.green);

                    // <-- 2. LA MÀGIA: SI ÉS LOGIN, GUARDEM L'USUARI I CARREGUEM EL MENÚ
                    if (endpoint == "/login")
                    {
                        PlayerPrefs.SetString("Username", username);
                        PlayerPrefs.Save();
                        SceneManager.LoadScene("Menu");
                    }
                }
            }
        }

        private void UpdateStatus(string message, Color color)
        {
            if (statusLabel != null)
            {
                statusLabel.text = message;
                statusLabel.style.color = new StyleColor(color);
            }
        }

        private void SetUIEnabled(bool isEnabled)
        {
            if (usernameInput != null) usernameInput.SetEnabled(isEnabled);
            if (passwordInput != null) passwordInput.SetEnabled(isEnabled);
            if (loginButton != null) loginButton.SetEnabled(isEnabled);
            if (registerButton != null) registerButton.SetEnabled(isEnabled);
        }
    }
}