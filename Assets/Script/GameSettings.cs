using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("EMG & Input Settings")]
    public int emgThreshold = 400; // Analog EMG value (0 - 1023) threshold for muscle contraction
    public float deltaPitchThreshold = 30.0f; // Relative Pitch threshold for Left/Right (±30° from baseline)
    public float deltaRollThreshold = 40.0f;  // Relative Roll threshold for Up/Down (±40° from baseline)
    public float moveSpeed = 4.0f;

    [Header("Difficulty Settings")]
    public int pointsPerSpeedStep = 10; // Increase speed every 10 points
    public float speedStepIncrement = 0.1f; // +0.1x per step
    public float maxSpeedMultiplier = 1.4f; // Safety ceiling for rehabilitation

    [Header("Session Settings")]
    public float sessionDurationSeconds = 300f; // 5 minutes default session

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadSettings();
    }

    public void LoadSettings()
    {
        emgThreshold = PlayerPrefs.GetInt("EMGThreshold", emgThreshold);
        deltaPitchThreshold = PlayerPrefs.GetFloat("DeltaPitchThreshold", 30.0f);
        deltaRollThreshold = PlayerPrefs.GetFloat("DeltaRollThreshold", 40.0f);
        moveSpeed = PlayerPrefs.GetFloat("MoveSpeed", moveSpeed);
        pointsPerSpeedStep = PlayerPrefs.GetInt("PointsPerSpeedStep", pointsPerSpeedStep);
        speedStepIncrement = PlayerPrefs.GetFloat("SpeedStepIncrement", speedStepIncrement);
        maxSpeedMultiplier = PlayerPrefs.GetFloat("MaxSpeedMultiplier", maxSpeedMultiplier);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("EMGThreshold", emgThreshold);
        PlayerPrefs.SetFloat("DeltaPitchThreshold", deltaPitchThreshold);
        PlayerPrefs.SetFloat("DeltaRollThreshold", deltaRollThreshold);
        PlayerPrefs.SetFloat("MoveSpeed", moveSpeed);
        PlayerPrefs.SetInt("PointsPerSpeedStep", pointsPerSpeedStep);
        PlayerPrefs.SetFloat("SpeedStepIncrement", speedStepIncrement);
        PlayerPrefs.SetFloat("MaxSpeedMultiplier", maxSpeedMultiplier);
        PlayerPrefs.Save();
    }
}
