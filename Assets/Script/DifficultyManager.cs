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
    [SerializeField] private float currentSpawnInterval = 5.0f;

    public float CurrentSpeedMultiplier => currentSpeedMultiplier;
    public float CurrentSpawnInterval => currentSpawnInterval;

    [Header("Clinical Safety Ceilings")]
    public float minSpeed = 0.7f;
    public float maxSpeed = 1.3f;
    public float speedStep = 0.1f;

    public float minSpawnInterval = 3.0f; // Fastest spawn rate
    public float maxSpawnInterval = 8.0f; // Slowest spawn rate if struggling
    public float spawnStep = 0.5f;

    [Header("Rolling Window State")]
    private Queue<int> recentAttempts = new Queue<int>();
    public const int WindowSize = 10;
    public const int IncreaseThreshold = 8; // >= 8 / 10 hits (80%)
    public const int DecreaseThreshold = 5; // <= 5 / 10 hits (50%)
    public const int ActivationPoints = 5;  // Difficulty unlocks after 5 points
    private int attemptsSinceAdjustment = 0;

    [Header("Session Lifetime Stats")]
    public int totalSessionHits = 0;
    public int totalSessionAttempts = 0;

    public int TotalSessionHits => totalSessionHits;
    public int TotalSessionAttempts => totalSessionAttempts;

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

        // Force runtime limits so Unity Inspector serialization cannot clamp spawn rate at 4.5s
        minSpawnInterval = 3.0f;
        maxSpawnInterval = 8.0f;
        spawnStep = 0.5f;
        minSpeed = 0.7f;
        maxSpeed = 1.3f;
        speedStep = 0.1f;
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
        currentSpawnInterval = 5.0f;
        minSpawnInterval = 3.0f;
        maxSpawnInterval = 8.0f;
        recentAttempts.Clear();
        attemptsSinceAdjustment = 0;
        totalSessionHits = 0;
        totalSessionAttempts = 0;
        activeToastMessage = "";
        toastTimer = 0f;
        Debug.Log("[DifficultyManager] Difficulty Reset: Speed 1.0x, Spawn 5.0s, MinSpawn 3.0s");
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
        totalSessionAttempts++;
        if (wasHit) totalSessionHits++;

        recentAttempts.Enqueue(wasHit ? 1 : 0);
        if (recentAttempts.Count > WindowSize)
        {
            recentAttempts.Dequeue();
        }

        attemptsSinceAdjustment++;

        int hits = CurrentHits;
        int total = recentAttempts.Count;

        Debug.Log($"[DifficultyManager] Shot Resolution: {(wasHit ? "HIT" : "MISS")} | Window Accuracy: {hits}/{total} | Session: {TotalSessionHits}/{TotalSessionAttempts}");

        // Do not adjust difficulty until at least 5 points have been earned
        int currentScore = (GUI.Instance != null) ? GUI.Instance.currentScore : 0;
        if (currentScore < ActivationPoints)
        {
            return;
        }

        // Apply a 5-shot buffer between consecutive difficulty adjustments
        if (attemptsSinceAdjustment < 5)
        {
            return;
        }

        // Only evaluate adjustments when rolling window has enough data (>= 5)
        if (total >= 5)
        {
            float accuracy = (float)hits / total;

            if (wasHit && (hits >= IncreaseThreshold || (total >= 5 && accuracy >= 0.78f)))
            {
                // Difficulty UP - only triggered on a HIT
                currentSpeedMultiplier = Mathf.Min(currentSpeedMultiplier + speedStep, maxSpeed);
                currentSpawnInterval = Mathf.Max(currentSpawnInterval - spawnStep, minSpawnInterval);
                attemptsSinceAdjustment = 0;

                ShowToast($"▲ DIFFICULTY UP! (Speed: {currentSpeedMultiplier:F1}x | Spawn: {currentSpawnInterval:F1}s)", Color.yellow);
                Debug.Log($"[DifficultyManager] ▲ Increased Difficulty -> Speed: {currentSpeedMultiplier:F1}x, Spawn: {currentSpawnInterval:F1}s");
            }
            else if (!wasHit && (hits <= DecreaseThreshold || (total >= 5 && accuracy <= 0.50f)))
            {
                // Difficulty DOWN - only triggered on a MISS
                currentSpeedMultiplier = Mathf.Max(currentSpeedMultiplier - speedStep, minSpeed);
                currentSpawnInterval = Mathf.Min(currentSpawnInterval + spawnStep, maxSpawnInterval);
                attemptsSinceAdjustment = 0;

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
