using UnityEngine;
using System;

public class BluetoothInputManager : MonoBehaviour
{
    public static BluetoothInputManager Instance { get; private set; }

    [Header("Bluetooth Device Targeting")]
    public string targetDeviceName = "HC-05";
    public string targetMACAddress = "";

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

    private bool loggedConnectionSuccess = false;
    private float nextLogTime = 0f;

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

    void Start()
    {
        Debug.Log("🎮 [BluetoothInputManager] System Initialized. Target Device: " + targetDeviceName + " | Simulation Mode: " + useSimulation);
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

        // Print Telemetry stream log in Unity Console every 3 seconds
        if (Time.time >= nextLogTime)
        {
            string modeStr = isConnected ? "Bluetooth (HC-05)" : "Editor Simulation Mode (WASD/Spacebar)";
            Debug.Log($"📡 [BluetoothInputManager] [{modeStr}] Telemetry Stream -> Pitch: {pitch:F1}°, Roll: {roll:F1}°, EMG: {emgValue}, ShootState: {shoot} ({(shoot == 1 ? "SHOOTING" : "READY")})");
            nextLogTime = Time.time + 3.0f;
        }
    }

    private void HandleKeyboardSimulation(int threshold)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        float threshX = (GameSettings.Instance != null) ? GameSettings.Instance.tiltXThreshold : 32.0f;
        float threshY = (GameSettings.Instance != null) ? GameSettings.Instance.tiltYThreshold : 32.0f;

        roll = h * (threshX + 10.0f);
        pitch = v * (threshY + 10.0f);

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

                if (!isConnected)
                {
                    isConnected = true;
                    connectionStatus = "Connected to " + targetDeviceName;
                    if (!loggedConnectionSuccess)
                    {
                        Debug.Log($"✅ [BluetoothInputManager] Bluetooth Connected Successfully to device '{targetDeviceName}'!");
                        loggedConnectionSuccess = true;
                    }
                }

                int threshold = (GameSettings.Instance != null) ? GameSettings.Instance.emgThreshold : 400;
                EvaluateShootState(threshold);
            }
            else
            {
                Debug.LogWarning($"⚠️ [BluetoothInputManager] Received malformed data packet: '{dataLine}'");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [BluetoothInputManager] Bluetooth Data Parsing Error: {ex.Message} | Packet: '{dataLine}'");
        }
    }

    public void OnBluetoothDisconnected()
    {
        isConnected = false;
        loggedConnectionSuccess = false;
        connectionStatus = "Disconnected";
        Debug.LogWarning($"⚠️ [BluetoothInputManager] Bluetooth Disconnected from device '{targetDeviceName}'. Re-entering Simulation Mode.");
    }
}
