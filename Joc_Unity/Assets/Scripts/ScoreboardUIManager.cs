using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Linq;

public class ScoreboardUIManager : MonoBehaviour
{
    // Paleta de colors arcade Bomberman
    private static readonly Color BG_DARK       = new Color(0.039f, 0.031f, 0.078f, 1f);   // rgb(10,8,20)
    private static readonly Color CARD_BG       = new Color(0.071f, 0.055f, 0.149f, 1f);   // rgb(18,14,38)
    private static readonly Color ORANGE        = new Color(1f,    0.549f, 0f,    1f);      // rgb(255,140,0)
    private static readonly Color GOLD          = new Color(1f,    0.784f, 0f,    1f);      // rgb(255,200,0)
    private static readonly Color RED_ARCADE    = new Color(0.627f, 0.078f, 0.235f, 1f);   // rgb(160,20,60)
    private static readonly Color BLUE_ARCADE   = new Color(0.078f, 0.392f, 0.784f, 1f);   // rgb(20,100,200)
    private static readonly Color BLUE_BORDER   = new Color(0.314f, 0.784f, 1f,    1f);    // rgb(80,200,255)
    private static readonly Color PURPLE_LIGHT  = new Color(0.784f, 0.706f, 1f,    1f);    // rgb(200,180,255)
    private static readonly Color TEXT_CREAM    = new Color(1f,    0.941f, 0.784f, 1f);    // rgb(255,240,200)

    private void OnEnable()
    {
        var uiDocument = GetComponent<UIDocument>();
        if (uiDocument == null)
        {
            Debug.LogError("Falta componente UIDocument en el GameObject para ScoreboardUIManager.");
            return;
        }

        var root = uiDocument.rootVisualElement;
        root.Clear();

        // Fons fosc principal
        root.style.backgroundColor  = new StyleColor(BG_DARK);
        root.style.justifyContent   = Justify.Center;
        root.style.alignItems       = Align.Center;
        root.style.width            = new StyleLength(Length.Percent(100));
        root.style.height           = new StyleLength(Length.Percent(100));

        // Bombes cantonades
        AddCornerBomb(root, top: 16, left: 16);
        AddCornerBomb(root, top: 16, right: 16);
        AddCornerBomb(root, bottom: 16, left: 16);
        AddCornerBomb(root, bottom: 16, right: 16);

        // --- CARD CENTRAL ---
        var card = new VisualElement();
        card.style.backgroundColor          = new StyleColor(CARD_BG);
        card.style.borderTopWidth           = 3;
        card.style.borderBottomWidth        = 3;
        card.style.borderLeftWidth          = 3;
        card.style.borderRightWidth         = 3;
        card.style.borderTopColor           = new StyleColor(ORANGE);
        card.style.borderBottomColor        = new StyleColor(ORANGE);
        card.style.borderLeftColor          = new StyleColor(ORANGE);
        card.style.borderRightColor         = new StyleColor(ORANGE);
        card.style.borderTopLeftRadius      = 6;
        card.style.borderTopRightRadius     = 6;
        card.style.borderBottomLeftRadius   = 6;
        card.style.borderBottomRightRadius  = 6;
        card.style.alignItems               = Align.Center;
        card.style.paddingTop               = 40;
        card.style.paddingBottom            = 36;
        card.style.paddingLeft              = 50;
        card.style.paddingRight             = 50;
        root.Add(card);

        // Títol BOMBERMAN
        var titleLabel = new Label("BOMBERMAN");
        titleLabel.style.fontSize               = 52;
        titleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        titleLabel.style.color                  = new StyleColor(GOLD);
        titleLabel.style.unityTextAlign         = TextAnchor.MiddleCenter;
        titleLabel.style.marginBottom           = 4;
        card.Add(titleLabel);

        var subtitleLabel = new Label("💥  MARCADOR FINAL  💥");
        subtitleLabel.style.fontSize            = 14;
        subtitleLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        subtitleLabel.style.color               = new StyleColor(new Color(1f, 0.392f, 0.118f));
        subtitleLabel.style.unityTextAlign      = TextAnchor.MiddleCenter;
        subtitleLabel.style.marginBottom        = 20;
        card.Add(subtitleLabel);

        // Divider
        card.Add(MakeDivider());

        // --- Calcular guanyador ---
        int winnerIndex = 0;
        int maxScore    = -999;
        int maxPlayers  = PlayerPrefs.GetInt("MaxPlayers", 4);

        for (int i = 0; i < maxPlayers; i++)
        {
            int score = (GameManager.playerLives[i] * 10) + GameManager.playerKills[i];
            if (score > maxScore) { maxScore = score; winnerIndex = i; }
        }

        bool wasOffline = PlayerPrefs.GetInt("LastMatchOffline", 0) == 1;
        string winnerName = wasOffline
            ? (winnerIndex == 0 ? PlayerPrefs.GetString("Username", "Jugador") : "Bot_" + winnerIndex)
            : PlayerPrefs.GetString("PlayerName_" + (winnerIndex + 1), "Jugador " + (winnerIndex + 1));

        // --- Banner GUANYADOR ---
        var winnerBanner = new VisualElement();
        winnerBanner.style.backgroundColor         = new StyleColor(new Color(0.118f, 0.549f, 0.235f));
        winnerBanner.style.borderTopWidth           = 2;
        winnerBanner.style.borderBottomWidth        = 2;
        winnerBanner.style.borderLeftWidth          = 2;
        winnerBanner.style.borderRightWidth         = 2;
        winnerBanner.style.borderTopColor           = new StyleColor(new Color(0.392f, 1f, 0.510f));
        winnerBanner.style.borderBottomColor        = new StyleColor(new Color(0.392f, 1f, 0.510f));
        winnerBanner.style.borderLeftColor          = new StyleColor(new Color(0.392f, 1f, 0.510f));
        winnerBanner.style.borderRightColor         = new StyleColor(new Color(0.392f, 1f, 0.510f));
        winnerBanner.style.borderTopLeftRadius      = 4;
        winnerBanner.style.borderTopRightRadius     = 4;
        winnerBanner.style.borderBottomLeftRadius   = 4;
        winnerBanner.style.borderBottomRightRadius  = 4;
        winnerBanner.style.paddingTop               = 14;
        winnerBanner.style.paddingBottom            = 14;
        winnerBanner.style.paddingLeft              = 30;
        winnerBanner.style.paddingRight             = 30;
        winnerBanner.style.alignItems               = Align.Center;
        winnerBanner.style.marginBottom             = 24;
        winnerBanner.style.width                    = 600;
        card.Add(winnerBanner);

        var winnerLabel = new Label($"🏆  GUANYADOR:  {winnerName}  🏆");
        winnerLabel.style.fontSize              = 28;
        winnerLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
        winnerLabel.style.color                 = new StyleColor(GOLD);
        winnerLabel.style.unityTextAlign        = TextAnchor.MiddleCenter;
        winnerBanner.Add(winnerLabel);

        // --- Taula ---
        var tableContainer = new VisualElement();
        tableContainer.style.backgroundColor        = new StyleColor(new Color(0.047f, 0.039f, 0.110f));
        tableContainer.style.borderTopWidth         = 2;
        tableContainer.style.borderBottomWidth      = 2;
        tableContainer.style.borderLeftWidth        = 2;
        tableContainer.style.borderRightWidth       = 2;
        tableContainer.style.borderTopColor         = new StyleColor(new Color(1f, 0.549f, 0f, 0.4f));
        tableContainer.style.borderBottomColor      = new StyleColor(new Color(1f, 0.549f, 0f, 0.4f));
        tableContainer.style.borderLeftColor        = new StyleColor(new Color(1f, 0.549f, 0f, 0.4f));
        tableContainer.style.borderRightColor       = new StyleColor(new Color(1f, 0.549f, 0f, 0.4f));
        tableContainer.style.borderTopLeftRadius    = 4;
        tableContainer.style.borderTopRightRadius   = 4;
        tableContainer.style.borderBottomLeftRadius = 4;
        tableContainer.style.borderBottomRightRadius= 4;
        tableContainer.style.paddingTop             = 20;
        tableContainer.style.paddingBottom          = 20;
        tableContainer.style.paddingLeft            = 30;
        tableContainer.style.paddingRight           = 30;
        tableContainer.style.marginBottom           = 24;
        card.Add(tableContainer);

        // Capçalera de la taula
        tableContainer.Add(CreateRow("JUGADOR", "VIDES", "KILLS", true));

        // Files dels jugadors
        for (int i = 0; i < maxPlayers; i++)
        {
            bool isWinner = (i == winnerIndex);
            string pName = wasOffline
                ? (i == 0 ? PlayerPrefs.GetString("Username", "Jugador") : "Bot_" + i)
                : PlayerPrefs.GetString("PlayerName_" + (i + 1), "Jugador " + (i + 1));

            tableContainer.Add(CreateRow(
                pName,
                GameManager.playerLives[i].ToString(),
                GameManager.playerKills[i].ToString(),
                false,
                isWinner ? GOLD : TEXT_CREAM
            ));
        }

        // Divider
        card.Add(MakeDivider());

        // Botó Volver al Menú
        var btnMenu = new Button();
        btnMenu.text = "◀  TORNAR AL MENÚ";
        btnMenu.style.fontSize                  = 20;
        btnMenu.style.unityFontStyleAndWeight   = FontStyle.Bold;
        btnMenu.style.marginTop                 = 8;
        btnMenu.style.paddingTop                = 14;
        btnMenu.style.paddingBottom             = 14;
        btnMenu.style.paddingLeft               = 40;
        btnMenu.style.paddingRight              = 40;
        btnMenu.style.width                     = 340;
        btnMenu.style.height                    = 54;
        btnMenu.style.backgroundColor           = new StyleColor(BLUE_ARCADE);
        btnMenu.style.color                     = new StyleColor(new Color(0.784f, 0.922f, 1f));
        btnMenu.style.borderTopWidth            = 2;
        btnMenu.style.borderBottomWidth         = 2;
        btnMenu.style.borderLeftWidth           = 2;
        btnMenu.style.borderRightWidth          = 2;
        btnMenu.style.borderTopColor            = new StyleColor(BLUE_BORDER);
        btnMenu.style.borderBottomColor         = new StyleColor(BLUE_BORDER);
        btnMenu.style.borderLeftColor           = new StyleColor(BLUE_BORDER);
        btnMenu.style.borderRightColor          = new StyleColor(BLUE_BORDER);
        btnMenu.style.borderTopLeftRadius       = 4;
        btnMenu.style.borderTopRightRadius      = 4;
        btnMenu.style.borderBottomLeftRadius    = 4;
        btnMenu.style.borderBottomRightRadius   = 4;
        btnMenu.clicked += OnMenuClicked;
        card.Add(btnMenu);

        // Peu INSERT COIN
        var footer = new Label("INSERT COIN TO CONTINUE");
        footer.style.fontSize               = 11;
        footer.style.unityFontStyleAndWeight = FontStyle.Bold;
        footer.style.color                  = new StyleColor(new Color(1f, 0.706f, 0f, 0.7f));
        footer.style.unityTextAlign         = TextAnchor.MiddleCenter;
        footer.style.marginTop              = 16;
        card.Add(footer);
    }

    // --- Helpers ---

    private VisualElement MakeDivider()
    {
        var d = new VisualElement();
        d.style.width               = new StyleLength(new Length(600, LengthUnit.Pixel));
        d.style.height              = 2;
        d.style.backgroundColor     = new StyleColor(ORANGE);
        d.style.marginTop           = 4;
        d.style.marginBottom        = 20;
        d.style.opacity             = 0.6f;
        return d;
    }

    private void AddCornerBomb(VisualElement root, float top = -1, float bottom = -1, float left = -1, float right = -1)
    {
        var bomb = new Label("💣");
        bomb.style.position     = Position.Absolute;
        bomb.style.fontSize     = 32;
        if (top    >= 0) bomb.style.top    = top;
        if (bottom >= 0) bomb.style.bottom = bottom;
        if (left   >= 0) bomb.style.left   = left;
        if (right  >= 0) bomb.style.right  = right;
        root.Add(bomb);
    }

    private VisualElement CreateRow(string col1, string col2, string col3, bool isHeader, Color color = default)
    {
        if (color == default) color = TEXT_CREAM;

        var row = new VisualElement();
        row.style.flexDirection         = FlexDirection.Row;
        row.style.justifyContent        = Justify.SpaceBetween;
        row.style.width                 = new StyleLength(new Length(600, LengthUnit.Pixel));
        row.style.borderBottomWidth     = isHeader ? 2 : 1;
        row.style.borderBottomColor     = new StyleColor(isHeader ? ORANGE : new Color(1f, 0.549f, 0f, 0.25f));
        row.style.paddingTop            = 10;
        row.style.paddingBottom         = 10;

        int fontSize = isHeader ? 18 : 16;
        Color headerColor = ORANGE;

        Label l1 = MakeCell(col1, fontSize, isHeader ? headerColor : color, isHeader, 40);
        Label l2 = MakeCell(col2, fontSize, isHeader ? headerColor : color, isHeader, 30, TextAnchor.MiddleCenter);
        Label l3 = MakeCell(col3, fontSize, isHeader ? headerColor : color, isHeader, 30, TextAnchor.MiddleCenter);

        row.Add(l1); row.Add(l2); row.Add(l3);
        return row;
    }

    private Label MakeCell(string text, int fontSize, Color color, bool bold, float widthPercent, TextAnchor align = TextAnchor.MiddleLeft)
    {
        var l = new Label(text);
        l.style.fontSize                = fontSize;
        l.style.color                   = new StyleColor(color);
        l.style.unityFontStyleAndWeight = bold ? FontStyle.Bold : FontStyle.Normal;
        l.style.width                   = new StyleLength(new Length(widthPercent, LengthUnit.Percent));
        l.style.unityTextAlign          = align;
        return l;
    }

    private void OnMenuClicked()
    {
        GameManager.playerLives = new int[] { 3, 3, 3, 3 };
        GameManager.playerKills = new int[] { 0, 0, 0, 0 };
        SceneManager.LoadScene("Menu");
    }
}
