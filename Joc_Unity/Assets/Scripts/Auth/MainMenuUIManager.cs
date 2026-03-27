using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;

public class MainMenuUIManager : MonoBehaviour
{
    private Button _btnSolo;
    private Button _btnMulti;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument?.rootVisualElement;

        if (root == null) return;

        _btnSolo = root.Q<Button>("BtnSolo");
        _btnMulti = root.Q<Button>("BtnMulti");

        if (_btnSolo != null) _btnSolo.clicked += OnPlaySoloClicked;
        if (_btnMulti != null) _btnMulti.clicked += OnMultiplayerClicked;
    }

    private void OnPlaySoloClicked()
    {
        Debug.Log("🎮 Iniciant partida per a un sol jugador...");
        // SceneManager.LoadScene("GameScene"); // Ho descomentarem quan tinguem l'escena
    }

    private void OnMultiplayerClicked()
    {
        Debug.Log("🌐 Obrint el cercador de partides (Lobbies)...");
         SceneManager.LoadScene("LobbyBrowser");
    }
}