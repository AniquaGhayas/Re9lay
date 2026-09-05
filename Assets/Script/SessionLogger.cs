/*
2D Space Shooter - Session Logger (Re9lay 2.0 20Hz Sampling Rate)
Saves CSV files to Downloads/game_csv on PC, and Documents/Re9layLogs on Android.
*/

using UnityEngine;
using System;
using System.IO;
using System.Text;

public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; }

    [Header("Logging Settings")]
    public float samplingInterval = 0.05f; // 20 Hz logging rate (50ms interval between rows)
    public bool isLoggingActive = false;

    private string currentFilePath;
    public string CurrentFilePath => currentFilePath;
    public string CurrentSessionLabel => sessionStartClockTime.ToString("yyyy_MM_dd_HH_mm");
    private StringBuilder csvBuffer = new StringBuilder();
    private float nextSampleTime = 0f;
    private float sessionStartTime = 0f;
    private DateTime sessionStartClockTime;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartLoggingSession()
    {
        sessionStartClockTime = DateTime.Now;
        
        // Filename format: yyyy_mm_dd_hh_mm.csv (e.g. 2026_09_04_05_01.csv)
        string filename = sessionStartClockTime.ToString("yyyy_MM_dd_HH_mm") + ".csv";

        // Determine Save Directory:
        // PC (Windows Editor / Standalone): Downloads/game_csv
        // Android (APK): Documents/Re9layLogs
        string logDir = Path.Combine(Application.persistentDataPath, "Re9layLogs");

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            string pcDownloads = Path.Combine(userProfile, "Downloads", "game_csv");
            if (!Directory.Exists(pcDownloads)) {
                Directory.CreateDirectory(pcDownloads);
            }
            logDir = pcDownloads;
        } catch (Exception ex) {
            Debug.LogWarning("[SessionLogger] PC Downloads directory fallback note: " + ex.Message);
        }
#elif UNITY_ANDROID && !UNITY_EDITOR
        try {
            string publicDocs = "/storage/emulated/0/Documents/Re9layLogs";
            if (!Directory.Exists(publicDocs)) {
                Directory.CreateDirectory(publicDocs);
            }
            logDir = publicDocs;
        } catch (Exception ex) {
            Debug.LogWarning("[SessionLogger] Android Documents fallback note: " + ex.Message);
        }
#endif

        if (!Directory.Exists(logDir))
        {
            Directory.CreateDirectory(logDir);
        }

        currentFilePath = Path.Combine(logDir, filename);

        csvBuffer.Clear();
        // Write CSV Header
        csvBuffer.AppendLine("timestamp,pitch,roll,emg_value,player_x,player_y,score,game_speed,shoot_state");

        sessionStartTime = Time.time;
        nextSampleTime = Time.time;
        isLoggingActive = true;

        Debug.Log($"[SessionLogger] Started 20Hz session logging -> File: {currentFilePath}");
    }

    void Update()
    {
        if (!isLoggingActive) return;

        if (Time.time >= nextSampleTime)
        {
            LogCurrentSample();
            nextSampleTime = Time.time + samplingInterval;
        }
    }

    private void LogCurrentSample()
    {
        float elapsedSeconds = Time.time - sessionStartTime;
        DateTime currentClockTime = sessionStartClockTime.AddSeconds(elapsedSeconds);
        string clockTimeStr = currentClockTime.ToString("HH_mm_ss_fff"); // Format: HH_mm_ss_fff

        float pitch = 0f, roll = 0f;
        int emg = 0, shoot = 0;

        if (BluetoothInputManager.Instance != null)
        {
            pitch = BluetoothInputManager.Instance.pitch;
            roll = BluetoothInputManager.Instance.roll;
            emg = BluetoothInputManager.Instance.emgValue;
            shoot = BluetoothInputManager.Instance.shoot;
        }

        float playerX = 0f, playerY = 0f;
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerX = playerObj.transform.position.x;
            playerY = playerObj.transform.position.y;
        }

        int score = 0;
        GUI gui = FindObjectOfType<GUI>();
        if (gui != null) score = gui.currentScore;

        float speed = 1.0f;
        if (DifficultyManager.Instance != null) speed = DifficultyManager.Instance.CurrentSpeedMultiplier;

        csvBuffer.AppendLine($"{clockTimeStr},{pitch:F2},{roll:F2},{emg},{playerX:F3},{playerY:F3},{score},{speed:F2},{shoot}");
    }

    public void StopLoggingSession()
    {
        if (!isLoggingActive) return;

        isLoggingActive = false;

        try
        {
            File.WriteAllText(currentFilePath, csvBuffer.ToString());
            Debug.Log($"[SessionLogger] Saved Re9lay 20Hz session CSV log ({csvBuffer.Length} bytes) to: {currentFilePath}");
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SessionLogger] Error writing session log: {ex.Message}");
        }
    }

    void OnApplicationQuit()
    {
        StopLoggingSession();
    }
}
