using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ScoreboardUIManager : MonoBehaviour
{
    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        // Si no hay UIDocument, no hacemos nada
        if (uiDocument == null)
        {
            Debug.LogError("Falta componente UIDocument en el GameObject para ScoreboardUIManager.");
            return;
        }

        var root = uiDocument.rootVisualElement;

        // Limpiar estilos si había algo
        root.Clear();
        
        // Estilos base de la pantalla
        root.style.backgroundColor = new StyleColor(new Color(0.1f, 0.1f, 0.1f, 0.95f));
        root.style.justifyContent = Justify.Center;
        root.style.alignItems = Align.Center;
        root.style.width = new StyleLength(Length.Percent(100));
        root.style.height = new StyleLength(Length.Percent(100));

        // Calcular el ganador (Basado en el que más puntuación tiene. Puntuación = Vidas*10 + Kills)
        int winnerIndex = 0;
        int maxScore = -999;
        
        int maxPlayers = PlayerPrefs.GetInt("MaxPlayers", 4);

        for (int i = 0; i < maxPlayers; i++)
        {
            int score = (GameManager.playerLives[i] * 10) + GameManager.playerKills[i];
            if (score > maxScore)
            {
                maxScore = score;
                winnerIndex = i;
            }
        }

        bool wasOffline = PlayerPrefs.GetInt("LastMatchOffline", 0) == 1;

        string winnerName;
        if (wasOffline) {
            winnerName = (winnerIndex == 0) ? PlayerPrefs.GetString("Username", "Jugador") : "Bot_" + winnerIndex;
        } else {
            winnerName = PlayerPrefs.GetString("PlayerName_" + (winnerIndex + 1), "Jugador " + (winnerIndex + 1));
        }

        // Título "GANADOR"
        Label winnerLabel = new Label();
        winnerLabel.text = $"GANADOR:\n{winnerName}";
        winnerLabel.style.fontSize = 80;
        winnerLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
        winnerLabel.style.color = new StyleColor(Color.yellow);
        winnerLabel.style.marginBottom = 50;
        root.Add(winnerLabel);

        // Contenedor de la tabla
        var tableContainer = new VisualElement();
        tableContainer.style.backgroundColor = new StyleColor(new Color(0.2f, 0.2f, 0.2f, 1f));
        tableContainer.style.paddingTop = 20;
        tableContainer.style.paddingBottom = 20;
        tableContainer.style.paddingLeft = 40;
        tableContainer.style.paddingRight = 40;
        tableContainer.style.borderTopLeftRadius = 15;
        tableContainer.style.borderTopRightRadius = 15;
        tableContainer.style.borderBottomLeftRadius = 15;
        tableContainer.style.borderBottomRightRadius = 15;
        root.Add(tableContainer);

        // Cabecera de la tabla
        tableContainer.Add(CreateRow("Jugador", "Vidas", "Bajas (Kills)", true));

        // Filas de los jugadores
        for (int i = 0; i < maxPlayers; i++)
        {
            bool isWinner = (i == winnerIndex);
            string pName;
            if (wasOffline) {
                pName = (i == 0) ? PlayerPrefs.GetString("Username", "Jugador") : "Bot_" + i;
            } else {
                pName = PlayerPrefs.GetString("PlayerName_" + (i + 1), "Jugador " + (i + 1));
            }
            
            tableContainer.Add(CreateRow(
                pName, 
                GameManager.playerLives[i].ToString(), 
                GameManager.playerKills[i].ToString(), 
                false, 
                isWinner ? Color.yellow : Color.white
            ));
        }

        // Botón para volver al menú
        Button btnMenu = new Button();
        btnMenu.text = "Volver al Menú";
        btnMenu.style.fontSize = 30;
        btnMenu.style.marginTop = 60;
        btnMenu.style.paddingTop = 15;
        btnMenu.style.paddingBottom = 15;
        btnMenu.style.paddingLeft = 40;
        btnMenu.style.paddingRight = 40;
        btnMenu.style.backgroundColor = new StyleColor(new Color(0.15f, 0.5f, 0.8f));
        btnMenu.style.color = new StyleColor(Color.white);
        btnMenu.style.borderTopLeftRadius = 10;
        btnMenu.style.borderTopRightRadius = 10;
        btnMenu.style.borderBottomLeftRadius = 10;
        btnMenu.style.borderBottomRightRadius = 10;
        
        btnMenu.clicked += OnMenuClicked;
        
        root.Add(btnMenu);
    }

    private VisualElement CreateRow(string col1, string col2, string col3, bool isHeader, Color color = default)
    {
        if (color == default) color = Color.white;

        var row = new VisualElement();
        row.style.flexDirection = FlexDirection.Row;
        row.style.justifyContent = Justify.SpaceBetween;
        row.style.width = new StyleLength(new Length(600, LengthUnit.Pixel));
        row.style.borderBottomWidth = isHeader ? 2 : 1;
        row.style.borderBottomColor = new StyleColor(Color.gray);
        row.style.paddingTop = 10;
        row.style.paddingBottom = 10;

        Label l1 = new Label(col1);
        Label l2 = new Label(col2);
        Label l3 = new Label(col3);

        int fontSize = isHeader ? 30 : 25;
        l1.style.fontSize = fontSize;
        l2.style.fontSize = fontSize;
        l3.style.fontSize = fontSize;

        l1.style.color = new StyleColor(color);
        l2.style.color = new StyleColor(color);
        l3.style.color = new StyleColor(color);

        l1.style.width = new StyleLength(new Length(40, LengthUnit.Percent));
        l2.style.width = new StyleLength(new Length(30, LengthUnit.Percent));
        l3.style.width = new StyleLength(new Length(30, LengthUnit.Percent));

        l2.style.unityTextAlign = TextAnchor.MiddleCenter;
        l3.style.unityTextAlign = TextAnchor.MiddleCenter;

        if (isHeader)
        {
            l1.style.unityFontStyleAndWeight = FontStyle.Bold;
            l2.style.unityFontStyleAndWeight = FontStyle.Bold;
            l3.style.unityFontStyleAndWeight = FontStyle.Bold;
        }

        row.Add(l1);
        row.Add(l2);
        row.Add(l3);

        return row;
    }

    private void OnMenuClicked()
    {
        // Reset statistics safely before exiting
        GameManager.playerLives = new int[] { 3, 3, 3, 3 };
        GameManager.playerKills = new int[] { 0, 0, 0, 0 };
        
        SceneManager.LoadScene("Menu");
    }
}
