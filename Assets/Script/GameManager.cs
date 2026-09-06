using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    public bool isGameActive = false;
    public bool isPaused = false;
    public float sessionTimer = 0f;
    public float baseGameSpeed = 0.5f;

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
        StartNewSession();
    }

    public void StartNewSession()
    {
        Time.timeScale = baseGameSpeed;
        Time.fixedDeltaTime = 0.02f * Time.timeScale;
        isGameActive = true;
        isPaused = false;
        sessionTimer = 0f;

        if (DifficultyManager.Instance != null)
        {
            DifficultyManager.Instance.ResetDifficulty();
        }

        if (SessionLogger.Instance != null)
        {
            SessionLogger.Instance.StartLoggingSession();
        }

        if (ReportUploader.Instance != null)
        {
            ReportUploader.Instance.WarmUp();
        }

        if (BluetoothInputManager.Instance != null)
        {
            BluetoothInputManager.Instance.StartOrientationCalibration();
        }
    }

    void Update()
    {
        if (!isGameActive || isPaused) return;

        sessionTimer += Time.deltaTime;

        // Check for session duration limit (if configured)
        if (GameSettings.Instance != null && GameSettings.Instance.sessionDurationSeconds > 0)
        {
            if (sessionTimer >= GameSettings.Instance.sessionDurationSeconds)
            {
                CompleteSession();
            }
        }

        // Toggle Pause with Escape or P key
        if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
        {
            TogglePause();
        }
    }

    public void TogglePause()
    {
        isPaused = !isPaused;
        Time.timeScale = isPaused ? 0.0f : baseGameSpeed;
        Time.fixedDeltaTime = isPaused ? 0.02f : 0.02f * Time.timeScale;
    }

    public void CompleteSession()
    {
        isGameActive = false;
        Time.timeScale = 0.0f;

        if (SessionLogger.Instance != null)
        {
            SessionLogger.Instance.StopLoggingSession();
        }

        Debug.Log("[GameManager] Rehabilitation Session Completed!");
    }

    public void OnPlayerGameOver()
    {
        isGameActive = false;

        if (SessionLogger.Instance != null)
        {
            SessionLogger.Instance.StopLoggingSession();
        }
    }
}
