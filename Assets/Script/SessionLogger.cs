using UnityEngine;
using System;
using System.IO;
using System.Text;
using System.Collections.Generic;

public class SessionLogger : MonoBehaviour
{
    public static SessionLogger Instance { get; private set; }

    [Header("Logging Settings")]
    public float samplingInterval = 0.05f; // 20 Hz logging rate
    public bool isLoggingActive = false;

    private string currentFilePath;
    private StringBuilder csvBuffer = new StringBuilder();
    private float nextSampleTime = 0f;
    private float sessionStartTime = 0f;

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
        string timestamp = DateTime.Now.ToString("yyyyMMDD_HHmmss");
        string filename = $"session_{timestamp}.csv";
        currentFilePath = Path.Combine(Application.persistentDataPath, filename);

        csvBuffer.Clear();
        // Write CSV Header
        csvBuffer.AppendLine("timestamp,pitch,roll,emg_value,player_x,player_y,score,game_speed,shoot_state");

        sessionStartTime = Time.time;
        nextSampleTime = Time.time;
        isLoggingActive = true;

        Debug.Log($"[SessionLogger] Started new logging session: {currentFilePath}");
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
        float relativeTimestamp = Time.time - sessionStartTime;

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

        csvBuffer.AppendLine($"{relativeTimestamp:F3},{pitch:F2},{roll:F2},{emg},{playerX:F3},{playerY:F3},{score},{speed:F2},{shoot}");
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
