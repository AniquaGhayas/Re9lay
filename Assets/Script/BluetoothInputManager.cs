using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BluetoothInputManager : MonoBehaviour
{
    public static BluetoothInputManager Instance { get; private set; }

    [Header("Bluetooth Device Targeting")]
    public string targetDeviceName = "HC-05";
    public string targetMACAddress = "";
    public string editorCOMPort = "COM4";
    public int baudRate = 9600;

    [Header("Current Sensor Data")]
    public float pitch = 0f;
    public float roll = 0f;
    public int emgValue = 0;
    public int shoot = 0;
    public bool isContracted = false;

    [Header("Connection Status")]
    public bool isConnected = false;
    public string connectionStatus = "Disconnected";

    [Header("Paired Devices List")]
    public List<string> pairedDevices = new List<string>();
    public bool isScanning = false;

    [Header("Simulation Controls (Editor Mode)")]
    public bool useSimulation = false;
    public int simulatedContractedEMG = 750;
    public int simulatedRelaxedEMG = 150;

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

        RequestAndroidPermissions();
    }

    void Start()
    {
        Debug.Log("🎮 [BluetoothInputManager] System Initialized. Target: " + targetDeviceName);
        RequestAndroidPermissions();
        ScanPairedDevices();

#if UNITY_EDITOR
        if (!useSimulation)
        {
            StartEditorSerialThread();
        }
#endif
    }

    public void RequestAndroidPermissions()
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

    public void ScanPairedDevices()
    {
        pairedDevices.Clear();
        isScanning = true;

#if UNITY_ANDROID && !UNITY_EDITOR
        try
        {
            using (AndroidJavaClass btAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
            {
                using (AndroidJavaObject btAdapter = btAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter"))
                {
                    if (btAdapter != null && btAdapter.Call<bool>("isEnabled"))
                    {
                        using (AndroidJavaObject bondedDevices = btAdapter.Call<AndroidJavaObject>("getBondedDevices"))
                        {
                            using (AndroidJavaObject iterator = bondedDevices.Call<AndroidJavaObject>("iterator"))
                            {
                                while (iterator.Call<bool>("hasNext"))
                                {
                                    using (AndroidJavaObject dev = iterator.Call<AndroidJavaObject>("next"))
                                    {
                                        string name = dev.Call<string>("getName");
                                        if (!string.IsNullOrEmpty(name) && !pairedDevices.Contains(name))
                                        {
                                            pairedDevices.Add(name);
                                        }
                                    }
                                }
                            }
                        }
                    }
                    else
                    {
                        connectionStatus = "Bluetooth is Turned Off";
                    }
                }
            }
        }
        catch (Exception ex)
        {
            connectionStatus = "Scan error: " + ex.Message;
        }
#elif UNITY_EDITOR
        try
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports != null)
            {
                foreach (string p in ports)
                {
                    if (!pairedDevices.Contains(p)) pairedDevices.Add(p);
                }
            }
        }
        catch { }
#endif
        isScanning = false;
    }

    public void ConnectToDevice(string deviceName)
    {
        targetDeviceName = deviceName;
        connectionStatus = "Connecting to " + deviceName + "...";
        isConnected = false;

        // Stop existing thread if running
        stopBTThread = true;
        if (btThread != null && btThread.IsAlive)
        {
            try { btThread.Abort(); } catch { }
        }

#if UNITY_ANDROID && !UNITY_EDITOR
        stopBTThread = false;
        btThread = new Thread(AndroidBluetoothWorkerLoop);
        btThread.IsBackground = true;
        btThread.Start();
#elif UNITY_EDITOR
        editorCOMPort = deviceName;
        stopBTThread = false;
        btThread = new Thread(EditorSerialWorkerLoop);
        btThread.IsBackground = true;
        btThread.Start();
#endif
    }

    public void Disconnect()
    {
        stopBTThread = true;
        isConnected = false;
        connectionStatus = "Disconnected";
        if (btThread != null && btThread.IsAlive)
        {
            try { btThread.Abort(); } catch { }
        }
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
            string modeStr = isConnected ? $"Bluetooth ({targetDeviceName})" : "Editor Simulation Mode (WASD/Spacebar)";
            Debug.Log($"📡 [BluetoothInputManager] [{modeStr}] Telemetry -> Pitch: {pitch:F1}°, Roll: {roll:F1}°, EMG: {emgValue}, ShootState: {shoot}");
            nextLogTime = Time.time + 3.0f;
        }
    }

#if UNITY_ANDROID && !UNITY_EDITOR
    private void AndroidBluetoothWorkerLoop()
    {
        AndroidJNI.AttachCurrentThread();
        try
        {
            using (AndroidJavaClass btAdapterClass = new AndroidJavaClass("android.bluetooth.BluetoothAdapter"))
            {
                using (AndroidJavaObject btAdapter = btAdapterClass.CallStatic<AndroidJavaObject>("getDefaultAdapter"))
                {
                    if (btAdapter == null || !btAdapter.Call<bool>("isEnabled"))
                    {
                        connectionStatus = "Bluetooth Disabled";
                        return;
                    }

                    try { btAdapter.Call<bool>("cancelDiscovery"); } catch { }

                    using (AndroidJavaObject bondedDevices = btAdapter.Call<AndroidJavaObject>("getBondedDevices"))
                    {
                        using (AndroidJavaObject iterator = bondedDevices.Call<AndroidJavaObject>("iterator"))
                        {
                            AndroidJavaObject targetDevice = null;
                            while (iterator.Call<bool>("hasNext"))
                            {
                                AndroidJavaObject dev = iterator.Call<AndroidJavaObject>("next");
                                string devName = dev.Call<string>("getName");
                                string devAddr = dev.Call<string>("getAddress");

                                if (!string.IsNullOrEmpty(devName) && devName.Equals(targetDeviceName, StringComparison.OrdinalIgnoreCase))
                                {
                                    targetDevice = dev;
                                    break;
                                }
                                else if (!string.IsNullOrEmpty(targetMACAddress) && devAddr.Equals(targetMACAddress, StringComparison.OrdinalIgnoreCase))
                                {
                                    targetDevice = dev;
                                    break;
                                }
                                else
                                {
                                    dev.Dispose();
                                }
                            }

                            if (targetDevice == null)
                            {
                                connectionStatus = $"Device '{targetDeviceName}' not found in paired list";
                                return;
                            }

                            AndroidJavaObject socket = null;
                            try
                            {
                                using (AndroidJavaClass uuidClass = new AndroidJavaClass("java.util.UUID"))
                                {
                                    using (AndroidJavaObject sppUuid = uuidClass.CallStatic<AndroidJavaObject>("fromString", "00001101-0000-1000-8000-00805F9B34FB"))
                                    {
                                        socket = targetDevice.Call<AndroidJavaObject>("createRfcommSocketToServiceRecord", sppUuid);
                                    }
                                }
                                socket.Call("connect");
                            }
                            catch
                            {
                                if (socket != null) { try { socket.Call("close"); } catch { } socket = null; }

                                // Fallback to insecure RFCOMM
                                try
                                {
                                    using (AndroidJavaClass uuidClass = new AndroidJavaClass("java.util.UUID"))
                                    {
                                        using (AndroidJavaObject sppUuid = uuidClass.CallStatic<AndroidJavaObject>("fromString", "00001101-0000-1000-8000-00805F9B34FB"))
                                        {
                                            socket = targetDevice.Call<AndroidJavaObject>("createInsecureRfcommSocketToServiceRecord", sppUuid);
                                        }
                                    }
                                    socket.Call("connect");
                                }
                                catch (Exception connEx)
                                {
                                    if (socket != null) { try { socket.Call("close"); } catch { } socket = null; }
                                    connectionStatus = "Connection failed: " + connEx.Message;
                                }
                            }

                            if (socket != null)
                            {
                                isConnected = true;
                                connectionStatus = "Connected to " + targetDeviceName;

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
                                try { socket.Call("close"); } catch { }
                            }

                            targetDevice.Dispose();
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            connectionStatus = "Error: " + ex.Message;
        }
        finally
        {
            isConnected = false;
            AndroidJNI.DetachCurrentThread();
        }
    }
#endif

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
        while (!stopBTThread)
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports == null || ports.Length == 0)
            {
                Thread.Sleep(3000);
                continue;
            }

            string activePort = editorCOMPort;
            if (string.IsNullOrEmpty(activePort) || Array.IndexOf(ports, activePort) < 0)
            {
                activePort = ports[0];
            }

            SerialPort sp = null;
            try
            {
                sp = new SerialPort(activePort, baudRate);
                sp.ReadTimeout = 1500;
                sp.Open();
                isConnected = true;
                connectionStatus = "Connected to " + activePort;

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
                connectionStatus = "Port error: " + ex.Message;
                Thread.Sleep(3000);
            }
            finally
            {
                if (sp != null && sp.IsOpen) sp.Close();
            }
        }
    }
#endif

    void OnDestroy()
    {
        stopBTThread = true;
        if (btThread != null && btThread.IsAlive)
        {
            try { btThread.Abort(); } catch { }
        }
    }

    private void HandleKeyboardSimulation(int threshold)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        // Pitch controls Left/Right: RIGHT: pitch > 35, LEFT: pitch < -20, Neutral: -20 to 35
        pitch = (h > 0) ? 45.0f : ((h < 0) ? -35.0f : 0f);

        // Roll controls Up/Down: UP: roll between +40 and +140, DOWN: roll between -180 and -140, Neutral: 0°
        roll = (v > 0) ? 90.0f : ((v < 0) ? -160.0f : 0f);

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
        // -------------------------------------------------------------
        // Left/Right Movement: uses Pitch (Swapped Axis)
        // -------------------------------------------------------------
        float dirX = 0f;
        if (pitch > 35.0f)
        {
            dirX = 1f;  // RIGHT: pitch > 35
        }
        else if (pitch < -20.0f)
        {
            dirX = -1f; // LEFT: pitch < -20
        }
        // Neutral: pitch between -20 and 35 -> dirX remains 0f

        // -------------------------------------------------------------
        // Up/Down Movement: uses Roll (Swapped Axis)
        // -------------------------------------------------------------
        float dirY = 0f;
        if (roll >= 40.0f && roll <= 140.0f)
        {
            dirY = 1f;  // UP: roll between +40 and +140
        }
        // DOWN: roll between -180 and -140 (handle wraparound near ±180 if roll is reported in that range)
        // NOTE: The DOWN roll range (-180 to -140) is close to the rest/neutral zone (~176°),
        // so it may need tighter tuning or a larger dead zone if it misfires during idle movement.
        else if ((roll >= -180.0f && roll <= -140.0f) || roll <= -180.0f)
        {
            dirY = -1f; // DOWN: roll between -180 and -140
        }
        // Neutral: everything else -> dirY remains 0f

        // -------------------------------------------------------------
        // Diagonal Handling:
        // Both axes are evaluated independently each frame. If both pitch and roll
        // cross their respective thresholds simultaneously, genuine diagonal input
        // (both movements active at the same time) is returned.
        // -------------------------------------------------------------
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

                isConnected = true;
                connectionStatus = "Connected to " + targetDeviceName;

                int threshold = (GameSettings.Instance != null) ? GameSettings.Instance.emgThreshold : 400;
                EvaluateShootState(threshold);
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"❌ [BluetoothInputManager] Parse error: {ex.Message}");
        }
    }
}
