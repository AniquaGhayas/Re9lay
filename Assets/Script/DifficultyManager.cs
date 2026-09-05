using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Current Adaptive Difficulty State")]
    [SerializeField] private float currentSpeedMultiplier = 1.0f;
    [SerializeField] private int speedStepLevel = 0;

    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public int SpeedStepLevel => speedStepLevel;

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

    public void ResetDifficulty()
    {
        currentSpeedMultiplier = 1.0f;
        speedStepLevel = 0;
    }

    /// <summary>
    /// Call when player score changes to update dynamic game speed.
    /// Speed increases every 10–15 points (configurable in GameSettings).
    /// </summary>
    public void UpdateScore(int currentScore)
    {
        int pointsPerStep = (GameSettings.Instance != null) ? GameSettings.Instance.pointsPerSpeedStep : 10;
        float increment = (GameSettings.Instance != null) ? GameSettings.Instance.speedStepIncrement : 0.1f;
        float maxSpeed = (GameSettings.Instance != null) ? GameSettings.Instance.maxSpeedMultiplier : 1.4f;

        if (pointsPerStep <= 0) pointsPerStep = 10;

        int newStepLevel = currentScore / pointsPerStep;

        if (newStepLevel != speedStepLevel)
        {
            speedStepLevel = newStepLevel;
            float targetMultiplier = 1.0f + (speedStepLevel * increment);
            currentSpeedMultiplier = Mathf.Min(targetMultiplier, maxSpeed);

            Debug.Log($"[DifficultyManager] Score: {currentScore} -> Level: {speedStepLevel}, Speed Multiplier: {currentSpeedMultiplier:F2}x (Max: {maxSpeed:F2}x)");
        }
    }
}
