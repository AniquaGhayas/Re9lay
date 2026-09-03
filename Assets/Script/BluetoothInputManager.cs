using UnityEngine;
using System;
using System.Threading;

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
    private readonly object lockObj = new object();
    private string pendingDataLine = "";

#if UNITY_ANDROID && !UNITY_EDITOR
    private Thread btThread;
    private bool stopBTThread = false;
#endif

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

#if UNITY_ANDROID && !UNITY_EDITOR
        useSimulation = false;
        StartAndroidBluetoothThread();
#endif
    }

    void Update()
    {
        // Process queued data line from background thread safely on Unity main thread
        string lineToProcess = "";
        lock (lockObj)
        {
            if (!string.IsNullOrEmpty(pendingDataLine))
            {
                lineToProcess = pendingDataLine;
                pendingDataLine = "";
            }
        }

        if (!string.IsNullOrEmpty(lineToProcess))
        {
            ProcessDataLine(lineToProcess);
        }

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

#if UNITY_ANDROID && !UNITY_EDITOR
    private void StartAndroidBluetoothThread()
    {
        stopBTThread = false;
        btThread = new Thread(AndroidBluetoothWorkerLoop);
        btThread.IsBackground = true;
        btThread.Start();
    }

    private void AndroidBluetoothWorkerLoop()
    {
        Debug.Log("[BluetoothInputManager] Android Native Bluetooth worker thread started...");
        while (!stopBTThread)
        {
            try
            {
                using (AndroidJavaClass btAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
                {
                    using (AndroidJavaObject btAdapter = btAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter"))
                    {
                        if (btAdapter == null || !btAdapter.Call<bool>("isEnabled"))
                        {
                            Thread.Sleep(2000);
                            continue;
                        }

                        using (AndroidJavaObject bondedDevices = btAdapter.Call<AndroidJavaObject>("getBondedDevices"))
                        {
                            using (AndroidJavaObject iterator = bondedDevices.Call<AndroidJavaObject>("iterator"))
                            {
                                AndroidJavaObject targetDevice = null;
                                while (iterator.Call<bool>("hasNext"))
                                {
                                    using (AndroidJavaObject device = iterator.Call<AndroidJavaObject>("next"))
                                    {
                                        string devName = device.Call<string>("getName");
                                        string devAddr = device.Call<string>("getAddress");

                                        if ((!string.IsNullOrEmpty(devName) && devName.Contains(targetDeviceName)) ||
                                            (!string.IsNullOrEmpty(targetMACAddress) && devAddr == targetMACAddress))
                                        {
                                            targetDevice = device;
                                            break;
                                        }
                                    }
                                }

                                if (targetDevice != null)
                                {
                                    using (AndroidJavaClass uuidClass = new AndroidJavaClass("java.util.UUID"))
                                    {
                                        using (AndroidJavaObject sppUuid = uuidClass.CallStatic<AndroidJavaObject>("fromString", "00001101-0000-1000-8000-00805F9B34FB"))
                                        {
                                            using (AndroidJavaObject socket = targetDevice.Call<AndroidJavaObject>("createRfcommSocketToServiceRecord", sppUuid))
                                            {
                                                socket.Call("connect");
                                                using (AndroidJavaObject inputStream = socket.Call<AndroidJavaObject>("getInputStream"))
                                                {
                                                    using (AndroidJavaObject isReader = new AndroidJavaObject("java.io.InputStreamReader", inputStream))
                                                    {
                                                        using (AndroidJavaObject bufferedReader = new AndroidJavaObject("java.io.BufferedReader", isReader))
                                                        {
                                                            while (!stopBTThread)
                                                            {
                                                                string line = bufferedReader.Call<string>("readLine");
                                                                if (line != null)
                                                                {
                                                                    lock (lockObj)
                                                                    {
                                                                        pendingDataLine = line;
                                                                    }
                                                                }
                                                                else
                                                                {
                                                                    break; // End of stream / disconnected
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                // Disconnected or connection error, wait and retry connection
                isConnected = false;
                Thread.Sleep(3000);
            }
        }
    }

    void OnDestroy()
    {
        stopBTThread = true;
        if (btThread != null && btThread.IsAlive)
        {
            btThread.Abort();
        }
    }
#endif

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
