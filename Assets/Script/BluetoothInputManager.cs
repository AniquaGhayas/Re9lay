using UnityEngine;
using System;
using System.IO.Ports;
using System.Threading;
using System.Globalization;
using System.Collections.Generic;
using System.Text.RegularExpressions;

#if UNITY_ANDROID
using UnityEngine.Android;
#endif

public class BluetoothInputManager : MonoBehaviour
{
    public static BluetoothInputManager Instance { get; private set; }

    [Header("Bluetooth Device Targeting")]
    public string targetDeviceName = "Re9lay-Glove";
    public string targetMACAddress = "";
    public string editorCOMPort = "COM4";
    public int baudRate = 115200;

    [Header("Current Sensor Data")]
    public float pitch = 0f;
    public float roll = 0f;
    public int emgValue = 0;
    public int shoot = 0;
    public bool isContracted = false;
    public bool is12BitADC = false;

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

    [Header("Neutral Orientation Baseline Calibration")]
    public bool isOrientationCalibrated = false;
    public bool isCalibratingOrientation = false;
    public float pitch0 = 0f;
    public float roll0 = 0f;
    public float orientationCalibrationDuration = 2.0f; // ~2 seconds of hand at rest
    public float orientationCalibrationTimer = 0f;
    private List<float> calibPitchSamples = new List<float>();
    private List<float> calibRollSamples = new List<float>();

    [Header("Live Relative Angles & Peak Envelopes (ROM Telemetry)")]
    public float currentDeltaPitch = 0f;
    public float currentDeltaRoll = 0f;
    public float peakLeftPitch = 0f;
    public float peakRightPitch = 0f;
    public float peakUpRoll = 0f;
    public float peakDownRoll = 0f;

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
        StartOrientationCalibration();

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
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
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        try
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports != null)
            {
                List<string> labeledPorts = new List<string>();
                foreach (string p in ports)
                {
                    string label = GetFriendlyPortLabel(p);
                    labeledPorts.Add(label);
                }

                // Prioritize Re9lay / Glove / HC-05 devices to the top of the list
                labeledPorts.Sort((a, b) =>
                {
                    bool aPref = a.IndexOf("Re9lay", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("Glove", StringComparison.OrdinalIgnoreCase) >= 0 || a.IndexOf("HC-05", StringComparison.OrdinalIgnoreCase) >= 0;
                    bool bPref = b.IndexOf("Re9lay", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("Glove", StringComparison.OrdinalIgnoreCase) >= 0 || b.IndexOf("HC-05", StringComparison.OrdinalIgnoreCase) >= 0;
                    if (aPref && !bPref) return -1;
                    if (!aPref && bPref) return 1;
                    return a.CompareTo(b);
                });

                foreach (string lp in labeledPorts)
                {
                    if (!pairedDevices.Contains(lp)) pairedDevices.Add(lp);
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
#elif UNITY_EDITOR || UNITY_STANDALONE_WIN
        editorCOMPort = ExtractCOMPort(deviceName);
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

        if (isCalibratingOrientation)
        {
            orientationCalibrationTimer -= Time.unscaledDeltaTime;
            calibPitchSamples.Add(pitch);
            calibRollSamples.Add(roll);

            if (orientationCalibrationTimer <= 0f)
            {
                FinishOrientationCalibration();
            }
        }

        // Compute live relative angles from neutral baseline
        if (isOrientationCalibrated)
        {
            currentDeltaPitch = NormalizeAngle(pitch - pitch0);
            currentDeltaRoll = NormalizeAngle(roll - roll0);
        }
        else
        {
            currentDeltaPitch = pitch;
            currentDeltaRoll = roll;
        }

        // Track live peak Range of Motion (ROM) envelopes
        if (currentDeltaPitch < peakLeftPitch) peakLeftPitch = currentDeltaPitch;
        if (currentDeltaPitch > peakRightPitch) peakRightPitch = currentDeltaPitch;
        if (currentDeltaRoll > peakUpRoll) peakUpRoll = currentDeltaRoll;
        if (currentDeltaRoll < peakDownRoll) peakDownRoll = currentDeltaRoll;

        if (Time.time >= nextLogTime)
        {
            string modeStr = isConnected ? $"Bluetooth ({targetDeviceName})" : "Editor Simulation Mode (WASD/Spacebar)";
            Debug.Log($"📡 [BluetoothInputManager] [{modeStr}] Telemetry -> Pitch: {pitch:F1}°, Roll: {roll:F1}°, EMG: {emgValue}, ShootState: {shoot}");
            nextLogTime = Time.time + 3.0f;
        }
    }

    public void ResetPeakEnvelopes()
    {
        peakLeftPitch = 0f;
        peakRightPitch = 0f;
        peakUpRoll = 0f;
        peakDownRoll = 0f;
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

                                if (!string.IsNullOrEmpty(devName) && (devName.Equals(targetDeviceName, StringComparison.OrdinalIgnoreCase) || devName.Contains("Re9lay") || devName.Contains("Glove") || devName.Equals("HC-05", StringComparison.OrdinalIgnoreCase)))
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

#if UNITY_EDITOR || UNITY_STANDALONE_WIN
    public static string GetFriendlyPortLabel(string portName)
    {
        if (string.IsNullOrEmpty(portName)) return "";
        try
        {
            // 1. Check Bluetooth paired devices in BTHENUM
            using (Microsoft.Win32.RegistryKey bthEnum = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\BTHENUM"))
            {
                if (bthEnum != null)
                {
                    foreach (string serviceName in bthEnum.GetSubKeyNames())
                    {
                        using (Microsoft.Win32.RegistryKey serviceKey = bthEnum.OpenSubKey(serviceName))
                        {
                            if (serviceKey == null) continue;
                            foreach (string deviceId in serviceKey.GetSubKeyNames())
                            {
                                using (Microsoft.Win32.RegistryKey devParams = serviceKey.OpenSubKey(deviceId + @"\Device Parameters"))
                                {
                                    if (devParams == null) continue;
                                    object pName = devParams.GetValue("PortName");
                                    if (pName != null && string.Equals(pName.ToString(), portName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        string mac = null;
                                        object uniqueId = devParams.GetValue("Bluetooth_UniqueID");
                                        if (uniqueId != null)
                                        {
                                            Match m = Regex.Match(uniqueId.ToString(), @"#([0-9A-Fa-f]{12})_");
                                            if (m.Success) mac = m.Groups[1].Value.ToLower();
                                        }
                                        if (string.IsNullOrEmpty(mac))
                                        {
                                            Match m2 = Regex.Match(deviceId, @"&([0-9A-Fa-f]{12})_");
                                            if (m2.Success) mac = m2.Groups[1].Value.ToLower();
                                        }

                                        if (!string.IsNullOrEmpty(mac))
                                        {
                                            using (Microsoft.Win32.RegistryKey bthDev = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Services\BTHPORT\Parameters\Devices\" + mac))
                                            {
                                                if (bthDev != null)
                                                {
                                                    object rawName = bthDev.GetValue("FriendlyName");
                                                    byte[] rawBytes = rawName as byte[];
                                                    string rawStr = rawName as string;
                                                    if (rawName == null || (rawBytes != null && rawBytes.Length <= 1) || (rawStr != null && string.IsNullOrEmpty(rawStr)))
                                                    {
                                                        rawName = bthDev.GetValue("Name");
                                                        rawBytes = rawName as byte[];
                                                        rawStr = rawName as string;
                                                    }

                                                    if (rawBytes != null)
                                                    {
                                                        string name = System.Text.Encoding.UTF8.GetString(rawBytes).Trim('\0', ' ');
                                                        if (!string.IsNullOrEmpty(name)) return $"{name} ({portName})";
                                                    }
                                                    else if (!string.IsNullOrEmpty(rawStr))
                                                    {
                                                        return $"{rawStr.Trim()} ({portName})";
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

            // 2. Check USB connected serial devices (e.g. ESP32 via USB cable)
            using (Microsoft.Win32.RegistryKey usbKey = Microsoft.Win32.Registry.LocalMachine.OpenSubKey(@"SYSTEM\CurrentControlSet\Enum\USB"))
            {
                if (usbKey != null)
                {
                    foreach (string vidPid in usbKey.GetSubKeyNames())
                    {
                        using (Microsoft.Win32.RegistryKey vidKey = usbKey.OpenSubKey(vidPid))
                        {
                            if (vidKey == null) continue;
                            foreach (string instId in vidKey.GetSubKeyNames())
                            {
                                using (Microsoft.Win32.RegistryKey devParams = vidKey.OpenSubKey(instId + @"\Device Parameters"))
                                {
                                    if (devParams == null) continue;
                                    object pName = devParams.GetValue("PortName");
                                    if (pName != null && string.Equals(pName.ToString(), portName, StringComparison.OrdinalIgnoreCase))
                                    {
                                        using (Microsoft.Win32.RegistryKey instKey = vidKey.OpenSubKey(instId))
                                        {
                                            if (instKey != null)
                                            {
                                                object fn = instKey.GetValue("FriendlyName");
                                                if (fn != null && !string.IsNullOrEmpty(fn.ToString()))
                                                {
                                                    return $"{fn} ({portName})";
                                                }
                                                object dd = instKey.GetValue("DeviceDesc");
                                                if (dd != null && !string.IsNullOrEmpty(dd.ToString()))
                                                {
                                                    string desc = dd.ToString();
                                                    int semi = desc.LastIndexOf(';');
                                                    if (semi >= 0 && semi < desc.Length - 1) desc = desc.Substring(semi + 1);
                                                    return $"{desc} ({portName})";
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
        catch { }
        return portName;
    }

    public static string ExtractCOMPort(string deviceLabel)
    {
        if (string.IsNullOrEmpty(deviceLabel)) return "";
        Match m = Regex.Match(deviceLabel, @"\((COM\d+)\)", RegexOptions.IgnoreCase);
        if (m.Success) return m.Groups[1].Value.ToUpper();
        Match m2 = Regex.Match(deviceLabel, @"\b(COM\d+)\b", RegexOptions.IgnoreCase);
        if (m2.Success) return m2.Groups[1].Value.ToUpper();
        return deviceLabel.Trim();
    }

    public static string FindPortForDevice(string searchKeyword)
    {
        try
        {
            string[] ports = SerialPort.GetPortNames();
            if (ports != null)
            {
                foreach (string p in ports)
                {
                    string label = GetFriendlyPortLabel(p);
                    if (label.IndexOf(searchKeyword, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return p;
                    }
                }
            }
        }
        catch { }
        return null;
    }

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
                connectionStatus = "No serial/COM ports found";
                Thread.Sleep(3000);
                continue;
            }

            string activePort = ExtractCOMPort(editorCOMPort);

            // If activePort is unset, default, or not found in ports, auto-detect Re9lay-Glove
            if (string.IsNullOrEmpty(activePort) || Array.IndexOf(ports, activePort) < 0 || activePort == "COM4")
            {
                string autoPort = FindPortForDevice("Re9lay");
                if (string.IsNullOrEmpty(autoPort)) autoPort = FindPortForDevice("Glove");
                if (!string.IsNullOrEmpty(autoPort) && Array.IndexOf(ports, autoPort) >= 0)
                {
                    activePort = autoPort;
                }
                else if (Array.IndexOf(ports, activePort) < 0)
                {
                    activePort = ports[0];
                }
            }

            SerialPort sp = null;
            try
            {
                sp = new SerialPort(activePort, baudRate);
                sp.ReadTimeout = 1500;
                sp.Open();
                isConnected = true;
                string friendly = GetFriendlyPortLabel(activePort);
                connectionStatus = "Connected to " + friendly;

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
                if (ex.Message.IndexOf("does not exist", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    connectionStatus = $"Cannot open {activePort}: Device offline or unready. Ensure ESP32 is powered ON.";
                }
                else
                {
                    connectionStatus = "Port error: " + ex.Message;
                }
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

    public void StartOrientationCalibration()
    {
        isOrientationCalibrated = false;
        isCalibratingOrientation = true;
        orientationCalibrationTimer = orientationCalibrationDuration;
        calibPitchSamples.Clear();
        calibRollSamples.Clear();
        Debug.Log("🎯 [BluetoothInputManager] Starting Neutral Orientation Calibration (2s rest period)...");
    }

    private void FinishOrientationCalibration()
    {
        isCalibratingOrientation = false;
        isOrientationCalibrated = true;

        if (calibPitchSamples.Count > 0 && calibRollSamples.Count > 0)
        {
            pitch0 = CalculateCircularMean(calibPitchSamples);
            roll0 = CalculateCircularMean(calibRollSamples);
        }
        else
        {
            pitch0 = pitch;
            roll0 = roll;
        }

        Debug.Log($"🎯 [BluetoothInputManager] Neutral Orientation Baseline Calibrated: pitch0 = {pitch0:F1}°, roll0 = {roll0:F1}° (from {calibPitchSamples.Count} samples)");
    }

    public static float NormalizeAngle(float angle)
    {
        while (angle > 180f) angle -= 360f;
        while (angle < -180f) angle += 360f;
        return angle;
    }

    public static float CalculateCircularMean(List<float> anglesInDegrees)
    {
        if (anglesInDegrees == null || anglesInDegrees.Count == 0) return 0f;
        float sumSin = 0f;
        float sumCos = 0f;
        for (int i = 0; i < anglesInDegrees.Count; i++)
        {
            float rad = anglesInDegrees[i] * Mathf.Deg2Rad;
            sumSin += Mathf.Sin(rad);
            sumCos += Mathf.Cos(rad);
        }
        if (Mathf.Approximately(sumSin, 0f) && Mathf.Approximately(sumCos, 0f))
        {
            return anglesInDegrees[0];
        }
        return Mathf.Atan2(sumSin, sumCos) * Mathf.Rad2Deg;
    }

    private void HandleKeyboardSimulation(int threshold)
    {
        float h = Input.GetAxisRaw("Horizontal");
        float v = Input.GetAxisRaw("Vertical");

        if (isCalibratingOrientation)
        {
            pitch = pitch0;
            roll = roll0;
        }
        else
        {
            // Relative displacement from baseline (pitch0, roll0):
            // D/Right: deltaPitch = +45 (> 30) -> RIGHT
            // A/Left:  deltaPitch = -45 (< -30) -> LEFT
            // Neutral: deltaPitch = 0
            float targetPitch = pitch0 + ((h > 0) ? 45.0f : ((h < 0) ? -45.0f : 0f));

            // W/Up:    deltaRoll = +50 (> 40) -> UP
            // S/Down:  deltaRoll = -50 (< -40) -> DOWN
            // Neutral: deltaRoll = 0
            float targetRoll = roll0 + ((v > 0) ? 50.0f : ((v < 0) ? -50.0f : 0f));

            pitch = Mathf.Lerp(pitch, targetPitch, Time.deltaTime * 10f);
            roll = Mathf.Lerp(roll, targetRoll, Time.deltaTime * 10f);
        }

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
        int effectiveThresh = threshold;
        if (is12BitADC && threshold <= 1023 && (EmgCalibrator.Instance == null || !EmgCalibrator.Instance.isCalibrated))
        {
            effectiveThresh = threshold * 4; // Scale 400 -> 1600 for uncalibrated 12-bit ESP32
        }

        if (emgValue >= effectiveThresh)
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
        // Before gesture detection begins (or if not yet calibrated), hold player neutral
        if (isCalibratingOrientation || !isOrientationCalibrated)
        {
            return Vector2.zero;
        }

        // Relative angular displacement from baseline (normalized to -180..180 for ±180 wraparound)
        float deltaPitch = NormalizeAngle(pitch - pitch0);
        float deltaRoll = NormalizeAngle(roll - roll0);

        float threshPitch = (GameSettings.Instance != null) ? GameSettings.Instance.deltaPitchThreshold : 30.0f;
        float threshRoll = (GameSettings.Instance != null) ? GameSettings.Instance.deltaRollThreshold : 40.0f;

        // -------------------------------------------------------------
        // Left / Right Movement: uses deltaPitch
        // RIGHT: deltaPitch > threshPitch (default 30)
        // LEFT:  deltaPitch < -threshPitch (default -30)
        // Neutral: deltaPitch between -threshPitch and threshPitch
        // -------------------------------------------------------------
        float dirX = 0f;
        if (deltaPitch > threshPitch)
        {
            dirX = 1f;  // RIGHT
        }
        else if (deltaPitch < -threshPitch)
        {
            dirX = -1f; // LEFT
        }

        // -------------------------------------------------------------
        // Up / Down Movement: uses deltaRoll
        // UP:   deltaRoll > threshRoll (default 40)
        // DOWN: deltaRoll < -threshRoll (default -40)
        // Neutral: deltaRoll between -threshRoll and threshRoll
        // -------------------------------------------------------------
        float dirY = 0f;
        if (deltaRoll > threshRoll)
        {
            dirY = 1f;  // UP
        }
        else if (deltaRoll < -threshRoll)
        {
            dirY = -1f; // DOWN
        }

        // -------------------------------------------------------------
        // Diagonal Handling:
        // Both axes checked independently each frame. If both deltaPitch
        // and deltaRoll cross thresholds simultaneously, genuine diagonal
        // input (both movements active at the same time) is returned.
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
                if (int.TryParse(parts[2], out int parsedEMG))
                {
                    emgValue = parsedEMG;
                    if (emgValue > 1023) is12BitADC = true;
                }

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
