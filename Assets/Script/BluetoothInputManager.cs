using UnityEngine;
using System;

public class BluetoothInputManager : MonoBehaviour
{
    public static BluetoothInputManager Instance { get; private set; }

    [Header("Current Sensor Data")]
    public float pitch = 0f;
    public float roll = 0f;
    public int emgValue = 0;
    public int shoot = 0; // 1 if emgValue >= emgThreshold, else 0
    public bool isContracted = false;

    [Header("Connection Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected (Simulation Mode Active)";

    [Header("Simulation Controls (Editor Mode)")]
    public bool useSimulation = true;
    public int simulatedContractedEMG = 750;
    public int simulatedRelaxedEMG = 150;

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

    void Update()
    {
        int emgThresh = (GameSettings.Instance != null) ? GameSettings.Instance.emgThreshold : 400;

        if (useSimulation || !isConnected)
        {
            HandleKeyboardSimulation(emgThresh);
        }
        else
        {
            EvaluateShootState(emgThresh);
        }
    }

    private void HandleKeyboardSimulation(int threshold)
    {
        // Keyboard WASD / Arrow Keys simulate tilt angles for testing
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float threshX = (GameSettings.Instance != null) ? GameSettings.Instance.tiltXThreshold : 32.0f;
        float threshY = (GameSettings.Instance != null) ? GameSettings.Instance.tiltYThreshold : 32.0f;

        roll = h * (threshX + 10.0f);
        pitch = v * (threshY + 10.0f);

        // Simulated EMG Contraction (Spacebar or Left Click)
        if (Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1"))
        {
            emgValue = simulatedContractedEMG;
        }
        else
        {
            emgValue = simulatedRelaxedEMG;
        }

        EvaluateShootState(threshold);
    }

    public void EvaluateShootState(int threshold)
    {
        if (emgValue >= threshold)
        {
            shoot = 1;
            isContracted = true;
        }
        else
        {
            shoot = 0;
            isContracted = false;
        }
    }

    /// <summary>
    /// Returns 4-directional movement vector based on tilt thresholds:
    /// Move Right if roll > tiltXThreshold (+32)
    /// Move Left if roll < -tiltXThreshold (-32)
    /// Move Up if pitch > tiltYThreshold (+32)
    /// Move Down if pitch < -tiltYThreshold (-32)
    /// </summary>
    public Vector2 GetMoveDirection()
    {
        float threshX = (GameSettings.Instance != null) ? GameSettings.Instance.tiltXThreshold : 32.0f;
        float threshY = (GameSettings.Instance != null) ? GameSettings.Instance.tiltYThreshold : 32.0f;

        float dirX = 0f;
        if (roll > threshX) dirX = 1f;
        else if (roll < -threshX) dirX = -1f;

        float dirY = 0f;
        if (pitch > threshY) dirY = 1f;
        else if (pitch < -threshY) dirY = -1f;

        return new Vector2(dirX, dirY);
    }

    public void ProcessDataLine(string dataLine)
    {
        if (string.IsNullOrEmpty(dataLine)) return;

        try
        {
            string[] parts = dataLine.Trim().Split(',');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], out float parsedPitch)) pitch = parsedPitch;
                if (float.TryParse(parts[1], out float parsedRoll)) roll = parsedRoll;
                if (int.TryParse(parts[2], out int parsedEMG)) emgValue = parsedEMG;

                isConnected = true;
                connectionStatus = "Connected";

                int threshold = (GameSettings.Instance != null) ? GameSettings.Instance.emgThreshold : 400;
                EvaluateShootState(threshold);
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("Bluetooth parse error: " + ex.Message + " | Line: " + dataLine);
        }
    }
}
