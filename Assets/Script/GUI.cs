/*
2D Space Shooter - Re9lay 3-Panel UI System with Bluetooth Status Badge
Panel 1: Main Menu (Bluetooth Connection Status Indicator)
Panel 2: Gameplay (Real-time Telemetry HUD with Bluetooth Connection Badge)
Panel 3: Game Over & Session Summary
*/

using UnityEngine;
using UnityEngine.UI;

public class GUI : MonoBehaviour {

    public static GUI Instance { get; private set; }

    public enum UIPanel { MainMenu = 1, Gameplay = 2, GameOver = 3 }
    [Header("Active Panel State")]
    public UIPanel currentPanel = UIPanel.MainMenu;

    public int currentScore = 0;
    public bool isGameOver = false;

    private Text livesText;
    private Text scoreText;
    private Text gameOverText;
    private Text instructionsText;

    private readonly float virtualWidth = 400f;
    private readonly float virtualHeight = 700f;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this);
            return;
        }
        Instance = this;
    }

    void Start () {
        // Disable old Canvas UI text elements
        GameObject livesObj = GameObject.Find("Lives");
        if (livesObj != null) {
            livesText = livesObj.GetComponent<Text>();
            if (livesText != null) livesText.enabled = false;
        }

        GameObject scoreObj = GameObject.Find("Score");
        if (scoreObj != null) {
            scoreText = scoreObj.GetComponent<Text>();
            if (scoreText != null) scoreText.enabled = false;
        }

        GameObject gameOverObj = GameObject.Find("GameOver");
        if (gameOverObj != null) {
            gameOverText = gameOverObj.GetComponent<Text>();
            if (gameOverText != null) gameOverText.enabled = false;
        }

        GameObject instructObj = GameObject.Find("Instructions");
        if (instructObj != null) {
            instructionsText = instructObj.GetComponent<Text>();
            if (instructionsText != null) instructionsText.enabled = false;
        }

        // Start at Panel 1 (Main Menu)
        ShowMainMenu();
    }

    public void ShowMainMenu() {
        currentPanel = UIPanel.MainMenu;
        isGameOver = false;
        Time.timeScale = 0f;
        SetGameplayElementsVisible(false);
    }

    public void StartGameplay() {
        currentPanel = UIPanel.Gameplay;
        currentScore = 0;
        isGameOver = false;
        Time.timeScale = 1.0f;

        SetGameplayElementsVisible(true);

        GameObject player = GameObject.Find("Player");
        if (player != null) {
            player.transform.position = new Vector3(0f, -3.4f, 0f);
            playerController pc = player.GetComponent<playerController>();
            if (pc != null) pc.isGameOver = false;
        }

        if (DifficultyManager.Instance != null) {
            DifficultyManager.Instance.ResetDifficulty();
        }

        if (GameManager.Instance != null) {
            GameManager.Instance.StartNewSession();
        }
    }

    public void TriggerGameOver() {
        currentPanel = UIPanel.GameOver;
        isGameOver = true;
        Time.timeScale = 0f;

        SetGameplayElementsVisible(false);

        if (GameManager.Instance != null) {
            GameManager.Instance.OnPlayerGameOver();
        }
    }

    public static void SetGameplayElementsVisible(bool visible) {
        GameObject player = GameObject.Find("Player");
        if (player != null) {
            foreach (var sr in player.GetComponentsInChildren<SpriteRenderer>(true)) {
                sr.enabled = visible;
            }
        }

        if (!visible) {
            foreach (var alien in Object.FindObjectsOfType<alienController>()) {
                Destroy(alien.gameObject);
            }
            foreach (var bullet in Object.FindObjectsOfType<Bullet>()) {
                Destroy(bullet.gameObject);
            }
        }
    }

    void Update() {
        GameObject player = GameObject.Find("Player");
        if (player != null) {
            playerController pc = player.GetComponent<playerController>();
            if (pc != null && pc.isGameOver && currentPanel == UIPanel.Gameplay) {
                TriggerGameOver();
            }
        }
    }

    void OnGUI() {
        if (Instance != this) return;

        Matrix4x4 origMatrix = UnityEngine.GUI.matrix;
        Vector3 scale = new Vector3(Screen.width / virtualWidth, Screen.height / virtualHeight, 1.0f);
        UnityEngine.GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

        switch (currentPanel) {
            case UIPanel.MainMenu:
                DrawPanel1_MainMenu();
                break;
            case UIPanel.Gameplay:
                DrawPanel2_Gameplay();
                break;
            case UIPanel.GameOver:
                DrawPanel3_GameOver();
                break;
        }

        UnityEngine.GUI.matrix = origMatrix;
    }

    // --- PANEL 1: MAIN MENU ---
    private void DrawPanel1_MainMenu() {
        UnityEngine.GUI.Box(new Rect(20f, 90f, 360f, 500f), "");

        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        titleStyle.fontSize = 24;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.yellow;
        UnityEngine.GUI.Label(new Rect(20f, 115f, 360f, 40f), "Re9lay", titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        subTitleStyle.fontSize = 12;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = Color.cyan;
        UnityEngine.GUI.Label(new Rect(20f, 155f, 360f, 25f), "Gamified IoT Rehabilitation System", subTitleStyle);

        // Bluetooth Connection Confirmation Indicator Badge
        bool isBTConnected = (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.isConnected);
        string targetDevice = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.targetDeviceName : "HC-05";
        string btBadgeStr = isBTConnected 
            ? $"<color=lime><b>● BLUETOOTH CONNECTED ({targetDevice})</b></color>" 
            : $"<color=yellow><b>○ BT SIMULATION MODE (WASD)</b></color>";

        GUIStyle btStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        btStyle.fontSize = 11;
        btStyle.alignment = TextAnchor.MiddleCenter;
        btStyle.richText = true;
        UnityEngine.GUI.Label(new Rect(20f, 185f, 360f, 25f), btBadgeStr, btStyle);

        GUIStyle bodyStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        bodyStyle.fontSize = 11;
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = Color.white;
        bodyStyle.wordWrap = true;
        UnityEngine.GUI.Label(new Rect(40f, 220f, 320f, 140f),
            "<b>REHABILITATION GOALS:</b>\n" +
            "• Tilt Wearable Glove or WASD to Move\n" +
            "• Contract Muscle or Hold Spacebar to Shoot\n" +
            "• Destroy incoming alien targets to score\n" +
            "• Dynamic adaptive speed scales every 10 points", bodyStyle);

        GUIStyle btnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.normal.textColor = Color.white;

        if (UnityEngine.GUI.Button(new Rect(60f, 430f, 280f, 55f), "START REHABILITATION SESSION", btnStyle)) {
            StartGameplay();
        }
    }

    // --- PANEL 2: ACTUAL GAMEPLAY & HUD ---
    private void DrawPanel2_Gameplay() {
        float totalSeconds = 0f;
        if (GameManager.Instance != null) {
            totalSeconds = GameManager.Instance.sessionTimer;
        } else {
            totalSeconds = Time.timeSinceLevelLoad;
        }

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);
        string timeFormatted = string.Format("{0:00}:{1:00}", minutes, seconds);

        GUIStyle headerStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        headerStyle.fontSize = 18;
        headerStyle.fontStyle = FontStyle.Bold;
        headerStyle.alignment = TextAnchor.MiddleCenter;
        headerStyle.normal.textColor = Color.yellow;
        string headerText = $"SCORE: {currentScore}    |    TIME: {timeFormatted}";
        UnityEngine.GUI.Label(new Rect(0f, 10f, virtualWidth, 30f), headerText, headerStyle);

        // Telemetry HUD Box with Live Bluetooth Status Badge
        GUILayout.BeginArea(new Rect(10f, 45f, 185f, 90f), UnityEngine.GUI.skin.box);
        
        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        titleStyle.fontSize = 11;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.white;
        GUILayout.Label("Re9lay", titleStyle);

        bool isBTConnected = (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.isConnected);
        string btStatusStr = isBTConnected 
            ? "<color=lime>● BT: CONNECTED</color>" 
            : "<color=yellow>○ BT: SIMULATION</color>";

        GUIStyle infoStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        infoStyle.fontSize = 10;
        infoStyle.richText = true;
        infoStyle.normal.textColor = Color.white;

        GUILayout.Label(btStatusStr, infoStyle);

        float speed = 1.0f;
        if (DifficultyManager.Instance != null) {
            speed = DifficultyManager.Instance.CurrentSpeedMultiplier;
        }
        GUILayout.Label($"Speed: {speed:F2}x", infoStyle);

        bool isShooting = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1") || (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.shoot == 1);
        string shootStatus = isShooting ? "<color=green>SHOOTING</color>" : "<color=yellow>READY</color>";
        GUILayout.Label($"Weapon: {shootStatus}", infoStyle);
        GUILayout.EndArea();

        if (GameManager.Instance != null && GameManager.Instance.isPaused) {
            UnityEngine.GUI.Box(new Rect(virtualWidth / 2f - 100f, virtualHeight / 2f - 50f, 200f, 100f), "PAUSED");
            if (UnityEngine.GUI.Button(new Rect(virtualWidth / 2f - 80f, virtualHeight / 2f, 160f, 30f), "Resume Session")) {
                GameManager.Instance.TogglePause();
            }
        }
    }

    // --- PANEL 3: GAME OVER & RESTART ---
    private void DrawPanel3_GameOver() {
        UnityEngine.GUI.Box(new Rect(20f, 140f, 360f, 400f), "");

        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;
        UnityEngine.GUI.Label(new Rect(20f, 170f, 360f, 40f), "SESSION COMPLETED", titleStyle);

        float totalSeconds = 0f;
        if (GameManager.Instance != null) {
            totalSeconds = GameManager.Instance.sessionTimer;
        }

        int minutes = Mathf.FloorToInt(totalSeconds / 60f);
        int seconds = Mathf.FloorToInt(totalSeconds % 60f);

        GUIStyle statsStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        statsStyle.fontSize = 14;
        statsStyle.fontStyle = FontStyle.Bold;
        statsStyle.alignment = TextAnchor.MiddleCenter;
        statsStyle.normal.textColor = Color.white;

        UnityEngine.GUI.Label(new Rect(20f, 230f, 360f, 30f), $"Final Score: {currentScore}", statsStyle);
        UnityEngine.GUI.Label(new Rect(20f, 270f, 360f, 30f), $"Duration: {minutes:00}:{seconds:00}", statsStyle);

        GUIStyle btnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;

        if (UnityEngine.GUI.Button(new Rect(70f, 350f, 260f, 50f), "RESTART SESSION", btnStyle)) {
            StartGameplay();
        }

        if (UnityEngine.GUI.Button(new Rect(70f, 420f, 260f, 40f), "MAIN MENU", btnStyle)) {
            ShowMainMenu();
        }
    }
}