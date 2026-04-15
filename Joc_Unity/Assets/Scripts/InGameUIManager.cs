using UnityEngine;
using UnityEngine.UIElements;

public class InGameUIManager : MonoBehaviour
{
    private Label[] _heartsLabels = new Label[4];
    private int _maxPlayers;

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("Falta UIDocument para el InGameUIManager.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        root.Clear();
        
        // Estiramos el contenedor a toda la pantalla sin tapar el fondo
        root.style.width = new StyleLength(Length.Percent(100));
        root.style.height = new StyleLength(Length.Percent(100));
        root.style.position = Position.Absolute;

        _maxPlayers = PlayerPrefs.GetInt("MaxPlayers", 4);
        if (GameManager.Instance != null && GameManager.Instance.isOfflineMode)
        {
            _maxPlayers = 4;
        }

        for (int i = 0; i < _maxPlayers; i++)
        {
            if (GameManager.Instance != null && GameManager.Instance.isOfflineMode) 
            {
                // Dejamos que se creen las intefaces de los bots
            }

            var container = new VisualElement();
            container.style.position = Position.Absolute;
            
            // Mejor diseño del cuadro
            container.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.12f, 0.9f));
            container.style.borderTopColor = new StyleColor(new Color(0.5f, 0.5f, 0.6f, 0.5f));
            container.style.borderBottomColor = new StyleColor(new Color(0.5f, 0.5f, 0.6f, 0.5f));
            container.style.borderLeftColor = new StyleColor(new Color(0.5f, 0.5f, 0.6f, 0.5f));
            container.style.borderRightColor = new StyleColor(new Color(0.5f, 0.5f, 0.6f, 0.5f));
            container.style.borderTopWidth = 2;
            container.style.borderBottomWidth = 2;
            container.style.borderLeftWidth = 2;
            container.style.borderRightWidth = 2;
            
            container.style.paddingTop = 15;
            container.style.paddingBottom = 15;
            container.style.paddingLeft = 25;
            container.style.paddingRight = 25;
            container.style.borderTopLeftRadius = 20;
            container.style.borderTopRightRadius = 20;
            container.style.borderBottomLeftRadius = 20;
            container.style.borderBottomRightRadius = 20;

            // Posicionar contenedor a las esquinas basado en el índice
            // (P1): Arriba-Izquierda
            if (i == 0) {
                container.style.top = 20;
                container.style.left = 20;
            } 
            // (P2): Abajo-Derecha
            else if (i == 1) {
                container.style.bottom = 20;
                container.style.right = 20;
            } 
            // (P3): Abajo-Izquierda
            else if (i == 2) {
                container.style.bottom = 20;
                container.style.left = 20;
            } 
            // (P4): Arriba-Derecha
            else if (i == 3) {
                container.style.top = 20;
                container.style.right = 20;
            }

            string pName;
            if (GameManager.Instance != null && GameManager.Instance.isOfflineMode) {
                pName = (i == 0) ? PlayerPrefs.GetString("Username", "Jugador") : "Bot_" + i;
            } else {
                pName = PlayerPrefs.GetString("PlayerName_" + (i + 1), "Jugador " + (i + 1));
            }
            
            var nameLbl = new Label(pName);
            nameLbl.style.color = new StyleColor(new Color(1f, 0.85f, 0.2f)); // Nombre en dorado/amarillo suave
            nameLbl.style.fontSize = 26;
            nameLbl.style.unityFontStyleAndWeight = FontStyle.Bold;
            nameLbl.style.unityTextAlign = TextAnchor.MiddleCenter;

            var heartsLbl = new Label();
            heartsLbl.enableRichText = true; // Habilitamos rich text para los colores
            heartsLbl.style.fontSize = 32;
            heartsLbl.style.marginTop = 8;
            heartsLbl.style.unityTextAlign = TextAnchor.MiddleCenter;

            _heartsLabels[i] = heartsLbl;

            container.Add(nameLbl);
            container.Add(heartsLbl);
            root.Add(container);
        }
    }

    private void Update()
    {
        for (int i = 0; i < _maxPlayers; i++)
        {
            if (_heartsLabels[i] == null) continue;

            int lives = GameManager.playerLives[i];
            
            // Usamos colores HTML y el símbolo del corazón puro para forzar el color rojo y negro(gris oscuro)
            if (lives >= 3) _heartsLabels[i].text = "<color=#FF3333>♥ ♥ ♥</color>";
            else if (lives == 2) _heartsLabels[i].text = "<color=#FF3333>♥ ♥</color> <color=#333333>♥</color>";
            else if (lives == 1) _heartsLabels[i].text = "<color=#FF3333>♥</color> <color=#333333>♥ ♥</color>";
            else _heartsLabels[i].text = "<color=#333333>♥ ♥ ♥</color>";
        }
    }
}
