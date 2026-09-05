using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Globalization;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BluetoothInputManager : MonoBehaviour
{
    public static BluetoothInputManager Instance { get; private set; }

    [Header("Bluetooth Device Targeting")]
    public string targetDeviceName = "HC-05";
    public string targetMACAddress = "";
    public string editorCOMPort = "COM7"; // Windows COM port when HC-05 paired to PC
    public int baudRate = 9600;

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
    public bool useSimulation = false; // Set to false to test live Bluetooth in Editor
    public int simulatedContractedEMG = 750;
    public int simulatedRelaxedEMG = 150;

    private bool loggedConnectionSuccess = false;
    private float nextLogTime = 0f;
    private readonly object lockObj = new object();
    private string pendingDataLine = "";

    private Thread btThread;
    private bool stopBTThread = false;

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

        RequestAndroidPermissions();

#if UNITY_EDITOR
        if (!useSimulation)
        {
            StartEditorSerialThread();
        }
#elif UNITY_ANDROID
        useSimulation = false;
        StartAndroidBluetoothThread();
#endif
    }

    private void RequestAndroidPermissions()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_CONNECT"))
            {
                Permission.RequestUserPermission("android.permission.BLUETOOTH_CONNECT");
            }
            if (!Permission.HasUserAuthorizedPermission("android.permission.BLUETOOTH_SCAN"))
            {
                Permission.RequestUserPermission("android.permission.BLUETOOTH_SCAN");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[BluetoothInputManager] Permission Request Note: " + ex.Message);
        }
#endif
    }

    void Update()
    {
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

        if (Time.time >= nextLogTime)
        {
            string modeStr = isConnected ? "Bluetooth (HC-05)" : "Editor Simulation Mode (WASD/Spacebar)";
            Debug.Log($"📡 [BluetoothInputManager] [{modeStr}] Telemetry Stream -> Pitch: {pitch:F1}°, Roll: {roll:F1}°, EMG: {emgValue}, ShootState: {shoot} ({(shoot == 1 ? "SHOOTING" : "READY")})");
            nextLogTime = Time.time + 3.0f;
        }
    }

#if UNITY_EDITOR
    private void StartEditorSerialThread()
    {
        stopBTThread = false;
        btThread = new Thread(EditorSerialWorkerLoop);
        btThread.IsBackground = true;
        btThread.Start();
    }

    private void EditorSerialWorkerLoop()
    {
        Debug.Log("[BluetoothInputManager] Unity Editor Windows Serial worker thread started...");
        
        string[] ports = SerialPort.GetPortNames();
        string activePort = editorCOMPort;
        if (ports != null && ports.Length > 0)
        {
            foreach (string p in ports)
            {
                if (p.Equals(editorCOMPort, StringComparison.OrdinalIgnoreCase))
                {
                    activePort = p;
                    break;
                }
            }
            if (string.IsNullOrEmpty(activePort) || Array.IndexOf(ports, activePort) < 0)
            {
                activePort = ports[0];
            }
        }

        while (!stopBTThread)
        {
            SerialPort sp = null;
            try
            {
                sp = new SerialPort(activePort, baudRate);
                sp.ReadTimeout = 1000;
                sp.Open();
                Debug.Log($"✅ [BluetoothInputManager] Opened Windows Serial COM Port '{activePort}' at {baudRate} baud.");

                while (!stopBTThread && sp.IsOpen)
                {
                    try
                    {
                        string line = sp.ReadLine();
                        if (!string.IsNullOrEmpty(line))
                        {
                            lock (lockObj)
                            {
                                pendingDataLine = line;
                            }
                        }
                    }
                    catch (TimeoutException) { }
                }
            }
            catch (Exception ex)
            {
                isConnected = false;
                Debug.LogWarning("[BluetoothInputManager] Editor Serial note: " + ex.Message);
                Thread.Sleep(3000);
            }
            finally
            {
                if (sp != null && sp.IsOpen)
                {
                    sp.Close();
                }
            }
        }
    }
#endif

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
                            int count = bondedDevices.Call<int>("size");
                            using (AndroidJavaObject iterator = bondedDevices.Call<AndroidJavaObject>("iterator"))
                            {
                                AndroidJavaObject targetDevice = null;
                                AndroidJavaObject fallbackDevice = null;

                                while (iterator.Call<bool>("hasNext"))
                                {
                                    using (AndroidJavaObject device = iterator.Call<AndroidJavaObject>("next"))
                                    {
                                        string devName = device.Call<string>("getName");
                                        string devAddr = device.Call<string>("getAddress");

                                        if (fallbackDevice == null) fallbackDevice = device;

                                        if (!string.IsNullOrEmpty(devName))
                                        {
                                            string lowerName = devName.ToLower();
                                            if (lowerName.Contains("hc-05") || lowerName.Contains("hc-06") || lowerName.Contains("bt05") ||
                                                (!string.IsNullOrEmpty(targetDeviceName) && lowerName.Contains(targetDeviceName.ToLower())) ||
                                                (!string.IsNullOrEmpty(targetMACAddress) && devAddr.Equals(targetMACAddress, StringComparison.OrdinalIgnoreCase)))
                                            {
                                                targetDevice = device;
                                                break;
                                            }
                                        }
                                    }
                                }

                                if (targetDevice == null && count == 1)
                                {
                                    targetDevice = fallbackDevice;
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
                                                                    break;
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
                isConnected = false;
                Debug.LogWarning("[BluetoothInputManager] Android Bluetooth note: " + ex.Message);
                Thread.Sleep(3000);
            }
        }
    }
#endif

    void OnDestroy()
    {
        stopBTThread = true;
        if (btThread != null && btThread.IsAlive)
        {
            btThread.Abort();
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
            string clean = dataLine.Trim();
            string[] parts = clean.Split(',');
            if (parts.Length >= 3)
            {
                if (float.TryParse(parts[0], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedPitch)) pitch = parsedPitch;
                if (float.TryParse(parts[1], NumberStyles.Any, CultureInfo.InvariantCulture, out float parsedRoll)) roll = parsedRoll;
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
