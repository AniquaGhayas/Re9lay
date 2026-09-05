/*
Re9lay - Rolling-Window Dynamic Difficulty Adjustment (DDA)
Tracks the last 10 shot attempts and adjusts both game speed (+-0.1x)
and enemy spawn interval (-+0.5s) based on clinical rehabilitation rules.
*/

using System.Collections.Generic;
using UnityEngine;

public class DifficultyManager : MonoBehaviour
{
    public static DifficultyManager Instance { get; private set; }

    [Header("Adaptive Difficulty Parameters")]
    [SerializeField] private float currentSpeedMultiplier = 1.0f;
    [SerializeField] private float currentSpawnInterval = 7.0f;

    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public float CurrentSpawnInterval => currentSpawnInterval;

    [Header("Clinical Safety Ceilings")]
    public float minSpeed = 0.7f;
    public float maxSpeed = 1.3f;
    public float speedStep = 0.1f;

    public float minSpawnInterval = 4.5f; // Never faster than 4.5s
    public float maxSpawnInterval = 9.5f; // Slower spawn if struggling
    public float spawnStep = 0.5f;

    [Header("Rolling Window State")]
    private Queue<int> recentAttempts = new Queue<int>();
    public const int WindowSize = 10;
    public const int IncreaseThreshold = 9; // >= 9 / 10 hits
    public const int DecreaseThreshold = 6; // <= 6 / 10 hits

    [Header("On-Screen Notification Toast")]
    public string activeToastMessage = "";
    public Color activeToastColor = Color.yellow;
    public float toastDuration = 2.0f;
    public float toastTimer = 0f;

    public int CurrentHits
    {
        get
        {
            int h = 0;
            foreach (int a in recentAttempts) h += a;
            return h;
        }
    }

    public int TotalAttemptsInWindow => recentAttempts.Count;

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
        if (toastTimer > 0f)
        {
            toastTimer -= Time.unscaledDeltaTime;
            if (toastTimer <= 0f)
            {
                activeToastMessage = "";
            }
        }
    }

    public void ResetDifficulty()
    {
        currentSpeedMultiplier = 1.0f;
        currentSpawnInterval = 7.0f;
        recentAttempts.Clear();
        activeToastMessage = "";
        toastTimer = 0f;
        Debug.Log("[DifficultyManager] Difficulty Reset: Speed 1.0x, Spawn 7.0s");
    }

    public void UpdateScore(int score)
    {
        // Compatibility stub - difficulty is now dynamically adjusted via RecordAttempt()
    }

    /// <summary>
    /// Records a shot attempt resolution (Hit or Miss).
    /// </summary>
    public void RecordAttempt(bool wasHit)
    {
        recentAttempts.Enqueue(wasHit ? 1 : 0);
        if (recentAttempts.Count > WindowSize)
        {
            recentAttempts.Dequeue();
        }

        int hits = CurrentHits;
        int total = recentAttempts.Count;

        Debug.Log($"[DifficultyManager] Shot Resolution: {(wasHit ? "HIT" : "MISS")} | Window Accuracy: {hits}/{total}");

        // Only evaluate difficulty adjustments once the rolling window has 10 attempts
        if (total >= WindowSize)
        {
            if (hits >= IncreaseThreshold)
            {
                // Difficulty UP
                currentSpeedMultiplier = Mathf.Min(currentSpeedMultiplier + speedStep, maxSpeed);
                currentSpawnInterval = Mathf.Max(currentSpawnInterval - spawnStep, minSpawnInterval);

                ShowToast($"▲ DIFFICULTY UP! (Speed: {currentSpeedMultiplier:F1}x | Spawn: {currentSpawnInterval:F1}s)", Color.yellow);
                Debug.Log($"[DifficultyManager] ▲ Increased Difficulty -> Speed: {currentSpeedMultiplier:F1}x, Spawn: {currentSpawnInterval:F1}s");
            }
            else if (hits <= DecreaseThreshold)
            {
                // Difficulty DOWN
                currentSpeedMultiplier = Mathf.Max(currentSpeedMultiplier - speedStep, minSpeed);
                currentSpawnInterval = Mathf.Min(currentSpawnInterval + spawnStep, maxSpawnInterval);

                ShowToast($"▼ DIFFICULTY DOWN (Speed: {currentSpeedMultiplier:F1}x | Spawn: {currentSpawnInterval:F1}s)", Color.cyan);
                Debug.Log($"[DifficultyManager] ▼ Decreased Difficulty -> Speed: {currentSpeedMultiplier:F1}x, Spawn: {currentSpawnInterval:F1}s");
            }
        }
    }

    private void ShowToast(string message, Color color)
    {
        activeToastMessage = message;
        activeToastColor = color;
        toastTimer = toastDuration;
    }
}
