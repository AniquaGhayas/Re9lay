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

    public enum UIPanel { MainMenu = 1, Calibration = 2, Gameplay = 3, GameOver = 4 }
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
    private Vector2 deviceScrollPos = Vector2.zero;

    [Header("Main Menu Branding")]
    public Texture2D mainMenuLogo;

    void Awake() {
        if (Instance != null && Instance != this) {
            Destroy(this);
            return;
        }
        Instance = this;

        if (mainMenuLogo == null) {
            mainMenuLogo = Resources.Load<Texture2D>("main_menu_logo");
        }

        if (mainMenuLogo == null) {
            string logoPath = System.IO.Path.Combine(Application.dataPath, "Sprites/main_menu_logo.png");
            if (System.IO.File.Exists(logoPath)) {
                try {
                    byte[] rawBytes = System.IO.File.ReadAllBytes(logoPath);
                    mainMenuLogo = new Texture2D(2, 2);
                    mainMenuLogo.LoadImage(rawBytes);
                } catch { }
            }
        }
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

    public void ShowCalibration() {
        currentPanel = UIPanel.Calibration;
        isGameOver = false;
        Time.timeScale = 0f;
        SetGameplayElementsVisible(false);

        if (EmgCalibrator.Instance != null) {
            EmgCalibrator.Instance.StartCalibration();
        }
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
            case UIPanel.Calibration:
                DrawPanel_Calibration();
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
        UnityEngine.GUI.Box(new Rect(20f, 25f, 360f, 650f), "");

        if (mainMenuLogo != null) {
            UnityEngine.GUI.DrawTexture(new Rect(50f, 32f, 300f, 52f), mainMenuLogo, ScaleMode.ScaleToFit);
        } else {
            GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            titleStyle.fontSize = 26;
            titleStyle.fontStyle = FontStyle.Bold;
            titleStyle.alignment = TextAnchor.MiddleCenter;
            titleStyle.normal.textColor = Color.yellow;
            UnityEngine.GUI.Label(new Rect(20f, 42f, 360f, 35f), "Re9lay", titleStyle);
        }

        GUIStyle subTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = Color.cyan;
        UnityEngine.GUI.Label(new Rect(20f, 85f, 360f, 18f), "Gamified IoT Rehabilitation System", subTitleStyle);

        // Bluetooth Setup Box
        UnityEngine.GUI.Box(new Rect(35f, 108f, 330f, 258f), "BLUETOOTH DEVICE SETUP");

        bool isBTConnected = (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.isConnected);
        string currentStatus = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.connectionStatus : "Disconnected";
        string targetDevice = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.targetDeviceName : "HC-05";

        GUIStyle statusStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        statusStyle.fontSize = 12;
        statusStyle.alignment = TextAnchor.MiddleCenter;
        statusStyle.richText = true;

        if (isBTConnected) {
            UnityEngine.GUI.Label(new Rect(40f, 140f, 320f, 30f), $"<color=lime><b>● CONNECTED: {targetDevice}</b></color>", statusStyle);

            GUIStyle discBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            discBtnStyle.fontSize = 12;
            if (UnityEngine.GUI.Button(new Rect(95f, 190f, 210f, 38f), "Disconnect Device", discBtnStyle)) {
                if (BluetoothInputManager.Instance != null) {
                    BluetoothInputManager.Instance.Disconnect();
                }
            }

            GUIStyle hintStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            hintStyle.fontSize = 11;
            hintStyle.alignment = TextAnchor.MiddleCenter;
            hintStyle.normal.textColor = Color.white;
            hintStyle.wordWrap = true;
            UnityEngine.GUI.Label(new Rect(45f, 245f, 310f, 60f), "Hardware module is connected and active.\nTap Start below to begin your rehabilitation workout.", hintStyle);
        } else {
            UnityEngine.GUI.Label(new Rect(40f, 130f, 320f, 25f), $"Status: <color=yellow>{currentStatus}</color>", statusStyle);

            GUIStyle scanBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            scanBtnStyle.fontSize = 12;
            scanBtnStyle.fontStyle = FontStyle.Bold;

            if (UnityEngine.GUI.Button(new Rect(55f, 160f, 290f, 36f), "🔍 SCAN PAIRED DEVICES", scanBtnStyle)) {
                if (BluetoothInputManager.Instance != null) {
                    BluetoothInputManager.Instance.RequestAndroidPermissions();
                    BluetoothInputManager.Instance.ScanPairedDevices();
                }
            }

            // Scrollable list of paired devices
            GUILayout.BeginArea(new Rect(45f, 205f, 310f, 150f));
            deviceScrollPos = GUILayout.BeginScrollView(deviceScrollPos, GUILayout.Width(310f), GUILayout.Height(150f));

            if (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.pairedDevices.Count > 0) {
                GUILayout.Label("<b>Select your module to connect:</b>", statusStyle);
                foreach (string dev in BluetoothInputManager.Instance.pairedDevices) {
                    bool isHC05 = dev.IndexOf("HC-05", System.StringComparison.OrdinalIgnoreCase) >= 0;
                    string btnLabel = isHC05 ? $"★ CONNECT TO {dev} ★" : $"Connect to {dev}";
                    if (GUILayout.Button(btnLabel, GUILayout.Height(34f))) {
                        BluetoothInputManager.Instance.ConnectToDevice(dev);
                    }
                }
            } else {
                GUIStyle emptyStyle = new GUIStyle(UnityEngine.GUI.skin.label);
                emptyStyle.fontSize = 11;
                emptyStyle.alignment = TextAnchor.MiddleCenter;
                emptyStyle.normal.textColor = Color.gray;
                emptyStyle.wordWrap = true;
                GUILayout.Space(15f);
                GUILayout.Label("No paired devices found.\n1. Pair HC-05 in Phone Bluetooth Settings (PIN 1234).\n2. Tap 'SCAN PAIRED DEVICES' above.", emptyStyle);
            }

            GUILayout.EndScrollView();
            GUILayout.EndArea();
        }

        GUIStyle bodyStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        bodyStyle.fontSize = 11;
        bodyStyle.alignment = TextAnchor.MiddleCenter;
        bodyStyle.normal.textColor = Color.white;
        bodyStyle.wordWrap = true;
        UnityEngine.GUI.Label(new Rect(35f, 380f, 330f, 140f),
            "<b>REHABILITATION GOALS:</b>\n" +
            "• Tilt Wearable Glove or WASD to Move\n" +
            "• Contract Muscle or Hold Spacebar to Shoot\n" +
            "• Destroy incoming alien targets to score\n" +
            "• Dynamic adaptive speed scales every 10 points", bodyStyle);

        GUIStyle btnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;
        btnStyle.normal.textColor = Color.white;

        if (UnityEngine.GUI.Button(new Rect(50f, 545f, 300f, 55f), "START REHABILITATION SESSION", btnStyle)) {
            ShowCalibration();
        }
    }

    // --- PANEL: EMG CALIBRATION ---
    private void DrawPanel_Calibration() {
        UnityEngine.GUI.Box(new Rect(20f, 25f, 360f, 650f), "");

        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        titleStyle.fontSize = 20;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.yellow;
        UnityEngine.GUI.Label(new Rect(20f, 40f, 360f, 30f), "EMG CALIBRATION", titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        subTitleStyle.fontSize = 11;
        subTitleStyle.alignment = TextAnchor.MiddleCenter;
        subTitleStyle.normal.textColor = Color.cyan;
        UnityEngine.GUI.Label(new Rect(20f, 70f, 360f, 20f), "Guided Pre-Session Muscle Threshold Setup", subTitleStyle);

        EmgCalibrator calibrator = EmgCalibrator.Instance;
        float liveEmg = (calibrator != null) ? calibrator.GetLiveEmgSample() : 0f;

        // Central Guided Calibration Card
        UnityEngine.GUI.Box(new Rect(35f, 100f, 330f, 360f), "CALIBRATION PROTOCOL");

        GUIStyle phaseHeaderStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        phaseHeaderStyle.fontSize = 15;
        phaseHeaderStyle.fontStyle = FontStyle.Bold;
        phaseHeaderStyle.alignment = TextAnchor.MiddleCenter;
        phaseHeaderStyle.richText = true;

        GUIStyle instructionStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        instructionStyle.fontSize = 12;
        instructionStyle.alignment = TextAnchor.MiddleCenter;
        instructionStyle.wordWrap = true;
        instructionStyle.normal.textColor = Color.white;

        GUIStyle timerStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        timerStyle.fontSize = 22;
        timerStyle.fontStyle = FontStyle.Bold;
        timerStyle.alignment = TextAnchor.MiddleCenter;
        timerStyle.normal.textColor = Color.white;

        if (calibrator == null || calibrator.currentPhase == EmgCalibrator.CalibrationPhase.Idle) {
            UnityEngine.GUI.Label(new Rect(45f, 160f, 310f, 30f), "<color=yellow>READY TO CALIBRATE</color>", phaseHeaderStyle);
            UnityEngine.GUI.Label(new Rect(45f, 200f, 310f, 60f), "This 8-second test measures your resting muscle signal and maximum contraction to personalize weapon firing.", instructionStyle);

            GUIStyle startCalBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            startCalBtnStyle.fontSize = 13;
            startCalBtnStyle.fontStyle = FontStyle.Bold;
            if (UnityEngine.GUI.Button(new Rect(70f, 280f, 260f, 45f), "▶ BEGIN CALIBRATION", startCalBtnStyle)) {
                if (calibrator != null) calibrator.StartCalibration();
            }
        } else if (calibrator.currentPhase == EmgCalibrator.CalibrationPhase.Rest) {
            UnityEngine.GUI.Label(new Rect(45f, 130f, 310f, 25f), "<color=cyan><b>PHASE 1 OF 2: REST BASELINE</b></color>", phaseHeaderStyle);
            UnityEngine.GUI.Label(new Rect(45f, 160f, 310f, 75f), "Relax your hand, wrist, and forearm completely on the surface.\n\nDo NOT contract or squeeze any muscles.", instructionStyle);

            UnityEngine.GUI.Label(new Rect(45f, 245f, 310f, 35f), $"{calibrator.phaseTimer:F1}s", timerStyle);

            // Progress bar
            UnityEngine.GUI.Box(new Rect(55f, 290f, 290f, 22f), "");
            float fillWidth = 286f * Mathf.Clamp01(calibrator.phaseProgress);
            UnityEngine.GUI.Box(new Rect(57f, 292f, fillWidth, 18f), "");

            GUIStyle meterStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            meterStyle.fontSize = 11;
            meterStyle.alignment = TextAnchor.MiddleCenter;
            meterStyle.normal.textColor = Color.cyan;
            UnityEngine.GUI.Label(new Rect(45f, 325f, 310f, 20f), $"Live Sensor: {liveEmg:F0} ADC", meterStyle);
        } else if (calibrator.currentPhase == EmgCalibrator.CalibrationPhase.Contract) {
            UnityEngine.GUI.Label(new Rect(45f, 130f, 310f, 25f), "<color=yellow><b>PHASE 2 OF 2: MAX CONTRACTION</b></color>", phaseHeaderStyle);
            UnityEngine.GUI.Label(new Rect(45f, 160f, 310f, 75f), "CONTRACT your hand or forearm firmly!\n\nHOLD the contraction steadily until the timer finishes.", instructionStyle);

            UnityEngine.GUI.Label(new Rect(45f, 245f, 310f, 35f), $"{calibrator.phaseTimer:F1}s", timerStyle);

            // Progress bar
            UnityEngine.GUI.Box(new Rect(55f, 290f, 290f, 22f), "");
            float fillWidth = 286f * Mathf.Clamp01(calibrator.phaseProgress);
            UnityEngine.GUI.Box(new Rect(57f, 292f, fillWidth, 18f), "");

            GUIStyle meterStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            meterStyle.fontSize = 11;
            meterStyle.alignment = TextAnchor.MiddleCenter;
            meterStyle.normal.textColor = Color.yellow;
            UnityEngine.GUI.Label(new Rect(45f, 325f, 310f, 20f), $"Live Sensor: {liveEmg:F0} ADC", meterStyle);
        } else if (calibrator.currentPhase == EmgCalibrator.CalibrationPhase.Completed) {
            UnityEngine.GUI.Label(new Rect(45f, 130f, 310f, 25f), "<color=lime><b>✔ CALIBRATION COMPLETE!</b></color>", phaseHeaderStyle);

            GUIStyle resStyle = new GUIStyle(UnityEngine.GUI.skin.label);
            resStyle.fontSize = 12;
            resStyle.alignment = TextAnchor.MiddleLeft;
            resStyle.normal.textColor = Color.white;
            resStyle.richText = true;

            GUILayout.BeginArea(new Rect(60f, 165f, 280f, 140f));
            GUILayout.Label($"• Rest Baseline: <b>{calibrator.restBaseline:F0} ADC</b>", resStyle);
            GUILayout.Label($"• Max Contraction: <b>{calibrator.maxContraction:F0} ADC</b>", resStyle);
            GUILayout.Space(6f);
            GUILayout.Label($"• Firing Threshold: <color=yellow><b>{calibrator.sessionThreshold:F0} ADC</b></color>", resStyle);
            GUILayout.Label("<size=10><color=gray>(50% midpoint between rest and max)</color></size>", resStyle);
            GUILayout.EndArea();

            GUIStyle playBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            playBtnStyle.fontSize = 13;
            playBtnStyle.fontStyle = FontStyle.Bold;
            playBtnStyle.normal.textColor = Color.white;

            if (UnityEngine.GUI.Button(new Rect(55f, 320f, 290f, 48f), "★ START REHABILITATION WORKOUT ★", playBtnStyle)) {
                StartGameplay();
            }

            GUIStyle reCalBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
            reCalBtnStyle.fontSize = 11;
            if (UnityEngine.GUI.Button(new Rect(105f, 385f, 190f, 30f), "🔄 Re-Calibrate", reCalBtnStyle)) {
                calibrator.StartCalibration();
            }
        }

        // Live Sensor Monitor Box
        UnityEngine.GUI.Box(new Rect(35f, 475f, 330f, 65f), "LIVE EMG SENSOR");
        GUIStyle sensorStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        sensorStyle.fontSize = 11;
        sensorStyle.alignment = TextAnchor.MiddleCenter;
        sensorStyle.richText = true;
        sensorStyle.normal.textColor = Color.white;
        string threshStr = (calibrator != null && calibrator.isCalibrated) ? $"{calibrator.sessionThreshold:F0}" : "400 (Default)";
        UnityEngine.GUI.Label(new Rect(45f, 498f, 310f, 30f), $"Raw Value: <b>{liveEmg:F0} ADC</b>   |   Active Threshold: <b>{threshStr}</b>", sensorStyle);

        // Action Buttons at bottom
        GUIStyle subBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
        subBtnStyle.fontSize = 11;

        if (calibrator != null && calibrator.currentPhase != EmgCalibrator.CalibrationPhase.Completed) {
            if (UnityEngine.GUI.Button(new Rect(50f, 555f, 300f, 38f), "SKIP TO DEFAULT THRESHOLD (400 ADC)", subBtnStyle)) {
                calibrator.SkipToDefault();
                StartGameplay();
            }
        }

        if (UnityEngine.GUI.Button(new Rect(80f, 605f, 240f, 34f), "Cancel / Main Menu", subBtnStyle)) {
            ShowMainMenu();
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
        GUILayout.BeginArea(new Rect(10f, 45f, 195f, 125f), UnityEngine.GUI.skin.box);
        
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

        int hits = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.CurrentHits : 0;
        int attempts = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalAttemptsInWindow : 0;

        int sessHits = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalSessionHits : currentScore;
        int sessAtts = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalSessionAttempts : currentScore;
        int pct = (sessAtts > 0) ? Mathf.RoundToInt((float)sessHits / sessAtts * 100f) : 100;

        if (currentScore < 5) {
            GUILayout.Label($"Accuracy: Calibrating ({currentScore}/5 pts)", infoStyle);
        } else {
            GUILayout.Label($"Accuracy: {pct}% ({sessHits}/{sessAtts})", infoStyle);
        }

        float spawnInt = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.CurrentSpawnInterval : 5.0f;
        GUILayout.Label($"Spawn: {spawnInt:F1}s", infoStyle);

        bool isShooting = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1") || (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.shoot == 1);
        string shootStatus = isShooting ? "<color=green>SHOOTING</color>" : "<color=yellow>READY</color>";
        GUILayout.Label($"Weapon: {shootStatus}", infoStyle);
        GUILayout.EndArea();

        // On-Screen Dynamic Difficulty Adjustment Toast Banner
        if (DifficultyManager.Instance != null && DifficultyManager.Instance.toastTimer > 0f) {
            GUIStyle toastStyle = new GUIStyle(UnityEngine.GUI.skin.box);
            toastStyle.fontSize = 12;
            toastStyle.fontStyle = FontStyle.Bold;
            toastStyle.alignment = TextAnchor.MiddleCenter;
            toastStyle.normal.textColor = DifficultyManager.Instance.activeToastColor;
            UnityEngine.GUI.Box(new Rect(20f, 180f, 360f, 40f), DifficultyManager.Instance.activeToastMessage, toastStyle);
        }

        if (GameManager.Instance != null && GameManager.Instance.isPaused) {
            UnityEngine.GUI.Box(new Rect(virtualWidth / 2f - 100f, virtualHeight / 2f - 50f, 200f, 100f), "PAUSED");
            if (UnityEngine.GUI.Button(new Rect(virtualWidth / 2f - 80f, virtualHeight / 2f, 160f, 30f), "Resume Session")) {
                GameManager.Instance.TogglePause();
            }
        }
    }

    // --- PANEL 3: GAME OVER & RESTART ---
    private void DrawPanel3_GameOver() {
        UnityEngine.GUI.Box(new Rect(20f, 90f, 360f, 520f), "");

        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        titleStyle.fontSize = 22;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.alignment = TextAnchor.MiddleCenter;
        titleStyle.normal.textColor = Color.red;
        UnityEngine.GUI.Label(new Rect(20f, 115f, 360f, 35f), "SESSION COMPLETED", titleStyle);

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

        UnityEngine.GUI.Label(new Rect(20f, 155f, 360f, 25f), $"Final Score: {currentScore}", statsStyle);
        UnityEngine.GUI.Label(new Rect(20f, 185f, 360f, 25f), $"Duration: {minutes:00}:{seconds:00}", statsStyle);

        // --- CLOUD PROGRESS REPORT SECTION ---
        UnityEngine.GUI.Box(new Rect(35f, 225f, 330f, 160f), "REHABILITATION PROGRESS REPORT");

        GUIStyle reportStatusStyle = new GUIStyle(UnityEngine.GUI.skin.label);
        reportStatusStyle.fontSize = 11;
        reportStatusStyle.alignment = TextAnchor.MiddleCenter;
        reportStatusStyle.wordWrap = true;
        reportStatusStyle.richText = true;

        ReportUploader uploader = ReportUploader.Instance;
        if (uploader != null) {
            if (uploader.currentStatus == ReportUploader.UploadStatus.Uploading) {
                reportStatusStyle.normal.textColor = Color.yellow;
                UnityEngine.GUI.Label(new Rect(45f, 255f, 310f, 40f), "⏳ Uploading CSV & Generating Report...\n(Waking up cloud container)", reportStatusStyle);
            } else if (uploader.currentStatus == ReportUploader.UploadStatus.Success) {
                reportStatusStyle.normal.textColor = Color.green;
                UnityEngine.GUI.Label(new Rect(45f, 250f, 310f, 30f), "<color=lime><b>✔ PDF Report Generated & Saved!</b></color>", reportStatusStyle);

                GUIStyle openBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
                openBtnStyle.fontSize = 12;
                openBtnStyle.fontStyle = FontStyle.Bold;
                if (UnityEngine.GUI.Button(new Rect(65f, 290f, 270f, 40f), "📂 RE-OPEN PDF REPORT", openBtnStyle)) {
                    uploader.OpenLastReport();
                }
            } else {
                if (uploader.currentStatus == ReportUploader.UploadStatus.Error) {
                    reportStatusStyle.normal.textColor = Color.red;
                    UnityEngine.GUI.Label(new Rect(45f, 250f, 310f, 35f), $"<color=red>{uploader.statusMessage}</color>", reportStatusStyle);
                } else {
                    reportStatusStyle.normal.textColor = Color.cyan;
                    UnityEngine.GUI.Label(new Rect(45f, 252f, 310f, 32f), "Generate PDF report with kinematics & EMG analysis:", reportStatusStyle);
                }

                GUIStyle genBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
                genBtnStyle.fontSize = 12;
                genBtnStyle.fontStyle = FontStyle.Bold;
                string btnText = (uploader.currentStatus == ReportUploader.UploadStatus.Error) ? "🔄 RETRY REPORT GENERATION" : "📄 GENERATE REHAB REPORT (PDF)";
                if (UnityEngine.GUI.Button(new Rect(65f, 292f, 270f, 44f), btnText, genBtnStyle)) {
                    uploader.GenerateReport();
                }
            }
        }

        GUIStyle btnStyle = new GUIStyle(UnityEngine.GUI.skin.button);
        btnStyle.fontSize = 14;
        btnStyle.fontStyle = FontStyle.Bold;

        if (UnityEngine.GUI.Button(new Rect(65f, 410f, 270f, 48f), "RESTART SESSION", btnStyle)) {
            StartGameplay();
        }

        if (UnityEngine.GUI.Button(new Rect(65f, 470f, 270f, 42f), "MAIN MENU", btnStyle)) {
            ShowMainMenu();
        }
    }
}