/*
Re9lay - Per-Session EMG Calibration System
Measures player's own rest baseline and maximum contraction before gameplay,
computing a personalized session contraction threshold and saving a sidecar JSON.
*/

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

public class EmgCalibrator : MonoBehaviour
{
    private static EmgCalibrator _instance;
    public static EmgCalibrator Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<EmgCalibrator>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("EmgCalibrator");
                    _instance = go.AddComponent<EmgCalibrator>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    public enum CalibrationPhase { Idle, Rest, Contract, Completed }

    [Header("Phase State")]
    public CalibrationPhase currentPhase = CalibrationPhase.Idle;
    public float phaseDuration = 4.0f;
    public float phaseTimer = 0f;
    public float phaseProgress = 0f; // 0.0 to 1.0

    [Header("Calibrated Values")]
    public float restBaseline = 150f;
    public float maxContraction = 750f;
    public float sessionThreshold = 400f;
    public bool isCalibrated = false;

    private List<float> currentPhaseSamples = new List<float>();
    private Coroutine calibrationCoroutine;

    void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    public void StartCalibration()
    {
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
        }
        calibrationCoroutine = StartCoroutine(CalibrationRoutine());
    }

    public void SkipToDefault()
    {
        if (calibrationCoroutine != null)
        {
            StopCoroutine(calibrationCoroutine);
            calibrationCoroutine = null;
        }

        restBaseline = 150f;
        maxContraction = 750f;
        sessionThreshold = 400f;
        isCalibrated = true;
        currentPhase = CalibrationPhase.Completed;

        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.emgThreshold = Mathf.RoundToInt(sessionThreshold);
        }

        SaveSidecarMetadata();
        Debug.Log("[EmgCalibrator] Skipped to default threshold: 400");
    }

    private IEnumerator CalibrationRoutine()
    {
        isCalibrated = false;

        // PHASE 1: REST (Relax hand)
        currentPhase = CalibrationPhase.Rest;
        currentPhaseSamples.Clear();
        phaseTimer = phaseDuration;

        while (phaseTimer > 0f)
        {
            float emg = GetLiveEmgSample();
            currentPhaseSamples.Add(emg);

            phaseTimer -= Time.unscaledDeltaTime;
            phaseProgress = Mathf.Clamp01(1f - (phaseTimer / phaseDuration));
            yield return null;
        }

        restBaseline = CalculateAverage(currentPhaseSamples);
        Debug.Log($"[EmgCalibrator] Phase 1 Rest Baseline: {restBaseline:F1}");

        // Short pause between phases
        yield return new WaitForSecondsRealtime(0.5f);

        // PHASE 2: MAX CONTRACTION (Contract as hard as possible)
        currentPhase = CalibrationPhase.Contract;
        currentPhaseSamples.Clear();
        phaseTimer = phaseDuration;

        while (phaseTimer > 0f)
        {
            float emg = GetLiveEmgSample();
            currentPhaseSamples.Add(emg);

            phaseTimer -= Time.unscaledDeltaTime;
            phaseProgress = Mathf.Clamp01(1f - (phaseTimer / phaseDuration));
            yield return null;
        }

        maxContraction = CalculatePeak(currentPhaseSamples);
        // Ensure max is strictly above rest
        if (maxContraction <= restBaseline)
        {
            maxContraction = restBaseline + 300f;
        }

        // PHASE 3: COMPUTE MIDPOINT THRESHOLD
        sessionThreshold = restBaseline + 0.5f * (maxContraction - restBaseline);
        isCalibrated = true;
        currentPhase = CalibrationPhase.Completed;

        if (GameSettings.Instance != null)
        {
            GameSettings.Instance.emgThreshold = Mathf.RoundToInt(sessionThreshold);
        }

        SaveSidecarMetadata();
        Debug.Log($"[EmgCalibrator] Calibration Complete! Rest: {restBaseline:F1}, Max: {maxContraction:F1}, Threshold: {sessionThreshold:F1}");
        calibrationCoroutine = null;
    }

    public float GetLiveEmgSample()
    {
        if (BluetoothInputManager.Instance != null)
        {
            return BluetoothInputManager.Instance.emgValue;
        }
        return 150f;
    }

    private float CalculateAverage(List<float> samples)
    {
        if (samples == null || samples.Count == 0) return 150f;
        float sum = 0f;
        foreach (float s in samples) sum += s;
        return sum / samples.Count;
    }

    private float CalculatePeak(List<float> samples)
    {
        if (samples == null || samples.Count == 0) return 750f;
        float peak = 0f;
        foreach (float s in samples)
        {
            if (s > peak) peak = s;
        }
        return peak;
    }

    public void SaveSidecarMetadata()
    {
        try
        {
            string csvPath = (SessionLogger.Instance != null) ? SessionLogger.Instance.CurrentFilePath : "";
            if (string.IsNullOrEmpty(csvPath)) return;

            string metaPath = Path.ChangeExtension(csvPath, null) + "_meta.json";
            string json = "{\n" +
                $"  \"rest_baseline\": {restBaseline:F1},\n" +
                $"  \"max_contraction\": {maxContraction:F1},\n" +
                $"  \"emg_threshold\": {sessionThreshold:F1}\n" +
                "}";

            File.WriteAllText(metaPath, json);
            Debug.Log($"[EmgCalibrator] Saved calibration sidecar JSON to: {metaPath}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[EmgCalibrator] Could not write sidecar JSON: {ex.Message}");
        }
    }
}
