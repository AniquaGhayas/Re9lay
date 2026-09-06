using UnityEngine;

public class GameSettings : MonoBehaviour
{
    public static GameSettings Instance { get; private set; }

    [Header("EMG & Input Settings")]
    public int emgThreshold = 400; // Analog EMG value (0 - 1023) threshold for muscle contraction
    public float tiltXThreshold = 40.0f; // Tilt threshold for X axis (left/right)
    public float tiltYThreshold = 90.0f; // Tilt threshold for Y axis (up/down)
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
        tiltXThreshold = PlayerPrefs.GetFloat("TiltXThreshold", 40.0f);
        tiltYThreshold = PlayerPrefs.GetFloat("TiltYThreshold", 90.0f);
        moveSpeed = PlayerPrefs.GetFloat("MoveSpeed", moveSpeed);
        pointsPerSpeedStep = PlayerPrefs.GetInt("PointsPerSpeedStep", pointsPerSpeedStep);
        speedStepIncrement = PlayerPrefs.GetFloat("SpeedStepIncrement", speedStepIncrement);
        maxSpeedMultiplier = PlayerPrefs.GetFloat("MaxSpeedMultiplier", maxSpeedMultiplier);
    }

    public void SaveSettings()
    {
        PlayerPrefs.SetInt("EMGThreshold", emgThreshold);
        PlayerPrefs.SetFloat("TiltXThreshold", tiltXThreshold);
        PlayerPrefs.SetFloat("TiltYThreshold", tiltYThreshold);
        PlayerPrefs.SetFloat("MoveSpeed", moveSpeed);
        PlayerPrefs.SetInt("PointsPerSpeedStep", pointsPerSpeedStep);
        PlayerPrefs.SetFloat("SpeedStepIncrement", speedStepIncrement);
        PlayerPrefs.SetFloat("MaxSpeedMultiplier", maxSpeedMultiplier);
        PlayerPrefs.Save();
    }
}
