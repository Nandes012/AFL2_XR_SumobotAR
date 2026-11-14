using UnityEngine;
using Vuforia;
using TMPro;

public class GameManager : MonoBehaviour
{
    public enum Mode { None, FreeRoam, Battle }

    [Header("References")]
    public ObserverBehaviour target;     // the Image Target
    public BotController bot;            // the player
    public EnemyBot enemy;               // the AI opponent

    [Header("UI")]
    public GameObject modePanel;
    public GameObject changeModeButton;
    public GameObject lostOverlay;
    public GameObject resultPanel;
    public TMP_Text resultText;
    public TMP_Text countdownText;       // no longer needed (the countdown is drawn with OnGUI)

    [Header("World objects")]
    public GameObject arenaRoot;         // the Battle arena
    public GameObject freeRoamGround;    // the invisible big floor

    [Header("Countdown")]
    public float secondsPerStep = 0.9f;
    public string goText = "FIGHT!";

    public Mode CurrentMode { get; private set; } = Mode.None;

    bool menuShown;
    bool matchOver;

    // Countdown state
    bool counting;
    int countStep;          // 3, 2, 1, 0 (= FIGHT)
    float stepTimer;

    // Match timer
    bool matchRunning;
    float matchTime;

    // HUD drawing
    Texture2D plateTex, accentTex;
    GUIStyle labelStyle, timeStyle, liftStyle, countStyle;

    static readonly Color Orange = new Color(1f, 0.48f, 0.10f, 1f);   // #FF7A1A
    static readonly Color Blue = new Color(0.16f, 0.71f, 0.96f, 1f);  // #28B6F6
    static readonly Color Red = new Color(0.90f, 0.28f, 0.30f, 1f);   // #E5484D
    static readonly Color Panel = new Color(0.106f, 0.133f, 0.169f, 0.85f);

    void Start()
    {
        modePanel.SetActive(false);
        changeModeButton.SetActive(false);
        lostOverlay.SetActive(true);
        resultPanel.SetActive(false);
        if (countdownText != null) countdownText.gameObject.SetActive(false);
        if (arenaRoot != null) arenaRoot.SetActive(false);
        if (freeRoamGround != null) freeRoamGround.SetActive(true);
        enemy.gameObject.SetActive(false);

        bot.canMove = false;
        bot.OnFell += () => EndMatch(false);
        enemy.OnFell += () => EndMatch(true);
    }

    void Update()
    {
        var status = target.TargetStatus.Status;
        bool tracked = status == Status.TRACKED;
        bool extended = status == Status.EXTENDED_TRACKED;

        // First detection: show the mode menu once
        if (!menuShown && tracked)
        {
            menuShown = true;
            ShowMenu();
        }

        // Is the game allowed to run right now?
        bool playable = CurrentMode == Mode.FreeRoam
            ? (tracked || extended)   // Free Roam survives Extended Tracking
            : tracked;                // Battle and the menu need the marker in view

        lostOverlay.SetActive(!playable);

        // The countdown and the match timer only advance while the marker is tracked
        if (counting && tracked) TickCountdown();
        if (matchRunning && tracked) matchTime += Time.deltaTime;
    }

    // ---------- Countdown ----------

    void StartCountdown()
    {
        counting = true;
        countStep = 3;
        stepTimer = secondsPerStep;
    }

    void TickCountdown()
    {
        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;

        countStep--;
        if (countStep < 0)
        {
            FinishCountdown();
            return;
        }
        stepTimer = secondsPerStep;
    }

    void FinishCountdown()
    {
        counting = false;

        // Release the bots and start the match timer
        bot.canMove = true;
        enemy.Begin();
        matchTime = 0f;
        matchRunning = true;
    }

    void CancelCountdown()
    {
        counting = false;
        matchRunning = false;
    }

    // ---------- Modes ----------

    void ShowMenu()
    {
        CancelCountdown();
        CurrentMode = Mode.None;
        matchOver = false;

        enemy.Stop();
        enemy.gameObject.SetActive(false);

        bot.canMove = false;
        bot.respawnOnFall = true;
        bot.ResetPose();

        if (arenaRoot != null) arenaRoot.SetActive(false);
        if (freeRoamGround != null) freeRoamGround.SetActive(true);

        resultPanel.SetActive(false);
        modePanel.SetActive(true);
        changeModeButton.SetActive(false);
    }

    // --- Hooked to the buttons ---
    public void SetFreeRoam()
    {
        CancelCountdown();
        CurrentMode = Mode.FreeRoam;
        matchOver = false;

        enemy.Stop();
        enemy.gameObject.SetActive(false);

        bot.ResetPose();
        bot.allowExtendedTracking = true;
        bot.respawnOnFall = true;
        bot.canMove = true;

        if (arenaRoot != null) arenaRoot.SetActive(false);
        if (freeRoamGround != null) freeRoamGround.SetActive(true);

        resultPanel.SetActive(false);
        modePanel.SetActive(false);
        changeModeButton.SetActive(true);
    }

    public void SetBattle()
    {
        CancelCountdown();
        CurrentMode = Mode.Battle;
        matchOver = false;

        bot.ResetPose();
        bot.allowExtendedTracking = false;   // pause when the marker is lost
        bot.respawnOnFall = false;           // falling means losing
        bot.canMove = false;                 // frozen until the countdown ends

        if (arenaRoot != null) arenaRoot.SetActive(true);
        if (freeRoamGround != null) freeRoamGround.SetActive(false);

        // Place the enemy, but keep it still until FIGHT!
        enemy.gameObject.SetActive(true);
        enemy.Stop();
        enemy.ResetPose();
        enemy.canMove = false;

        resultPanel.SetActive(false);
        modePanel.SetActive(false);
        changeModeButton.SetActive(true);

        StartCountdown();
    }

    public void ChangeMode()
    {
        ShowMenu();
    }

    void EndMatch(bool playerWon)
    {
        if (CurrentMode != Mode.Battle || matchOver) return;
        matchOver = true;
        matchRunning = false;
        CancelCountdown();

        enemy.Stop();
        bot.canMove = false;

        resultText.richText = true;
        resultText.text = (playerWon ? "VICTORY" : "DEFEAT")
                          + "\n<size=50%>TIME " + FormatTime(matchTime) + "</size>";
        resultText.color = playerWon ? Blue : Red;
        resultPanel.SetActive(true);
    }

    // ---------- HUD (drawn with OnGUI, so it always shows in the build) ----------

    static string FormatTime(float t)
    {
        int s = Mathf.FloorToInt(t);
        return (s / 60).ToString("00") + ":" + (s % 60).ToString("00");
    }

    static Texture2D Solid(Color c)
    {
        var t = new Texture2D(1, 1);
        t.SetPixel(0, 0, c);
        t.Apply();
        return t;
    }

    void BuildStyles(float u)
    {
        if (plateTex == null) plateTex = Solid(Panel);
        if (accentTex == null) accentTex = Solid(Orange);

        if (labelStyle == null) labelStyle = new GUIStyle(GUI.skin.label);
        if (timeStyle == null) timeStyle = new GUIStyle(GUI.skin.label);
        if (liftStyle == null) liftStyle = new GUIStyle(GUI.skin.label);
        if (countStyle == null) countStyle = new GUIStyle(GUI.skin.label);

        labelStyle.fontSize = Mathf.RoundToInt(u * 1.0f);
        labelStyle.fontStyle = FontStyle.Bold;
        labelStyle.alignment = TextAnchor.MiddleCenter;
        labelStyle.normal.textColor = Orange;

        timeStyle.fontSize = Mathf.RoundToInt(u * 1.5f);
        timeStyle.fontStyle = FontStyle.Bold;
        timeStyle.alignment = TextAnchor.MiddleCenter;
        timeStyle.normal.textColor = new Color(0.91f, 0.93f, 0.96f, 1f);

        liftStyle.fontSize = Mathf.RoundToInt(u * 2.2f);
        liftStyle.fontStyle = FontStyle.Bold;
        liftStyle.alignment = TextAnchor.MiddleCenter;

        countStyle.fontStyle = FontStyle.Bold;
        countStyle.alignment = TextAnchor.MiddleCenter;
    }

    void DrawShadowed(Rect r, string text, GUIStyle style, Color color)
    {
        var old = style.normal.textColor;
        style.normal.textColor = new Color(0.1f, 0.13f, 0.17f, 0.9f);
        GUI.Label(new Rect(r.x + 5, r.y + 5, r.width, r.height), text, style);
        style.normal.textColor = color;
        GUI.Label(r, text, style);
        style.normal.textColor = old;
    }

    void OnGUI()
    {
        float u = Mathf.Min(Screen.width, Screen.height) * 0.05f;   // base unit
        BuildStyles(u);

        // HUD plate: mode name, and the match timer in Battle
        if (CurrentMode != Mode.None && !modePanel.activeSelf)
        {
            bool battle = CurrentMode == Mode.Battle;
            float w = u * 11f;
            float h = battle ? u * 3.6f : u * 2.0f;
            var plate = new Rect((Screen.width - w) * 0.5f, u * 0.8f, w, h);

            GUI.DrawTexture(plate, plateTex);
            GUI.DrawTexture(new Rect(plate.x, plate.y, plate.width, u * 0.18f), accentTex);   // top accent line
            GUI.Label(new Rect(plate.x, plate.y + u * 0.2f, plate.width, u * 1.4f),
                      battle ? "BATTLE" : "FREE ROAM", labelStyle);
            if (battle)
                GUI.Label(new Rect(plate.x, plate.y + u * 1.5f, plate.width, u * 2f),
                          FormatTime(matchTime), timeStyle);
        }

        // LIFT! flash when a wedge gets under a bot
        if (CurrentMode == Mode.Battle && matchRunning
            && (bot.Lift > 0.4f || enemy.Lift > 0.4f))
        {
            bool playerLifting = enemy.Lift > bot.Lift;   // you are lifting the enemy
            DrawShadowed(new Rect(0, Screen.height * 0.5f, Screen.width, u * 3f),
                         "LIFT!", liftStyle, playerLifting ? Blue : Red);
        }

        // Countdown 3, 2, 1, FIGHT!
        if (counting)
        {
            string text = countStep > 0 ? countStep.ToString() : goText;
            float t = Mathf.Clamp01(stepTimer / secondsPerStep);
            countStyle.fontSize = (int)(Screen.height * 0.18f * Mathf.Lerp(1f, 1.4f, t));
            DrawShadowed(new Rect(0, Screen.height * 0.15f, Screen.width, Screen.height * 0.3f),
                         text, countStyle, Orange);
        }
    }
}