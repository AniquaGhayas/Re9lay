/*
2D Space Shooter - Session Logger (0.50s / 500ms Time Interval CSV Logging)
Saves CSV files to Public Documents folder on Android.
*/

using UnityEngine;
using System;
using System.IO;
using System.Text;

public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; }

    [Header("Logging Settings")]
    public float samplingInterval = 0.50f; // 0.50s interval (500ms time difference between readings / 2 Hz sampling rate)
    public bool isLoggingActive = false;

    private string currentFilePath;
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
        string timestampStr = sessionStartClockTime.ToString("yyyyMMdd_HHmmss");
        string filename = $"session_{timestampStr}.csv";

        string logDir = Path.Combine(Application.persistentDataPath, "SessionLogs");

#if UNITY_ANDROID && !UNITY_EDITOR
        try {
            string publicDocs = "/storage/emulated/0/Documents/NeuroPlayLogs";
            if (!Directory.Exists(publicDocs)) {
                Directory.CreateDirectory(publicDocs);
            }
            logDir = publicDocs;
        } catch (Exception ex) {
            Debug.LogWarning("[SessionLogger] Fallback to persistentDataPath: " + ex.Message);
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

        Debug.Log($"[SessionLogger] Started logging session at {sessionStartClockTime:HH_mm_ss_fff} -> Saved to: {currentFilePath}");
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
        string clockTimeStr = currentClockTime.ToString("HH_mm_ss_fff"); // Format: HH_mm_ss_fff (e.g. 21_23_34_500)

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
            Debug.Log($"[SessionLogger] Saved session log ({csvBuffer.Length} bytes) to: {currentFilePath}");
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
