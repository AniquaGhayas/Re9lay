# Re9lay - EMG Calibration, Adaptive Difficulty & UI Instructions

## Context
Two research-backed mechanics need to be added to the Unity game, replacing
the current fixed EMG threshold and fixed score-tier difficulty scaling:

1. **Per-session EMG calibration** - instead of a hardcoded threshold
   (currently 400), measure the player's own rest/contraction baseline at
   the start of every session.
2. **Rolling-window adaptive difficulty** - instead of "speed increases
   every 10-15 points," adjust difficulty based on the player's last 10
   shot attempts.

Both need a small UI addition so the player understands what's happening
and sees feedback when difficulty changes.

---

## Part A: EMG Calibration

### Flow
Before gameplay starts, run a ~10 second calibration:
1. Show "Relax your hand" for ~4 seconds -> record EMG samples -> compute
   `rest_baseline` (mean of samples).
2. Show "Contract as hard as you can" for ~4 seconds -> record EMG samples
   -> compute `max_contraction` (peak of samples).
3. Compute the session's contraction threshold as the midpoint:
   ```
   emg_threshold = rest_baseline + 0.5 * (max_contraction - rest_baseline)
   ```
4. Store `emg_threshold` for use during gameplay (a session-scoped variable,
   not PlayerPrefs - it should be recalculated every session).

### C# sketch
```csharp
public class EmgCalibrator : MonoBehaviour
{
    public float restDurationSeconds = 4f;
    public float contractDurationSeconds = 4f;

    private List<float> restSamples = new List<float>();
    private List<float> contractSamples = new List<float>();

    public float RestBaseline { get; private set; }
    public float MaxContraction { get; private set; }
    public float SessionThreshold { get; private set; }

    public IEnumerator RunCalibration(System.Func<float> readEmgSample)
    {
        restSamples.Clear();
        contractSamples.Clear();

        // Phase 1: rest
        CalibrationUI.Instance.ShowPrompt("Relax your hand...", restDurationSeconds);
        float t = 0f;
        while (t < restDurationSeconds)
        {
            restSamples.Add(readEmgSample());
            t += Time.deltaTime;
            yield return null;
        }
        RestBaseline = Average(restSamples);

        // Phase 2: max contraction
        CalibrationUI.Instance.ShowPrompt("Contract as hard as you can!", contractDurationSeconds);
        t = 0f;
        while (t < contractDurationSeconds)
        {
            contractSamples.Add(readEmgSample());
            t += Time.deltaTime;
            yield return null;
        }
        MaxContraction = Max(contractSamples);

        SessionThreshold = RestBaseline + 0.5f * (MaxContraction - RestBaseline);
        CalibrationUI.Instance.ShowResult(RestBaseline, MaxContraction, SessionThreshold);
    }

    private float Average(List<float> vals)
    {
        float sum = 0f;
        foreach (var v in vals) sum += v;
        return vals.Count > 0 ? sum / vals.Count : 0f;
    }

    private float Max(List<float> vals)
    {
        float m = float.MinValue;
        foreach (var v in vals) if (v > m) m = v;
        return m;
    }
}
```

Use `emgCalibrator.SessionThreshold` wherever the game currently checks
the hardcoded 400 value for contraction detection.

### Logging the calibration values
Don't add columns to the existing session CSV (that would break the
Python parser's fixed schema). Instead write a small sidecar JSON file
next to each session CSV, same base filename:

```
2026_09_04_04_43.csv
2026_09_04_04_43_meta.json
```

```json
{
  "rest_baseline": 142.3,
  "max_contraction": 861.0,
  "emg_threshold": 501.65
}
```

This lets the Python report pipeline read the *actual* calibrated
threshold used that session instead of assuming 400 - pass it to
`--emg-threshold` when generating the report, or read it automatically
if present.

---

## Part B: Rolling-Window Adaptive Difficulty

Source: rule-based DDA from "Wrist movement classification for adaptive
mobile phone based rehabilitation of children with motor skill
impairments" (arxiv.org/abs/2401.17134).

### Rule
Track the last 10 shot attempts as hit (1) or miss (0):
- If **>= 9 of the last 10** are hits -> increase difficulty
- If **<= 6 of the last 10** are hits -> decrease difficulty
- Otherwise (7 or 8 hits) -> leave difficulty unchanged

A "hit" = a contraction (`shoot_state` transition) that successfully
increments score. A "miss" = a contraction that didn't land a hit, or
however the game currently defines a failed shot attempt.

This replaces the existing fixed "speed increases every 10-15 points"
logic entirely - remove that code path.

### C# sketch
```csharp
public class AdaptiveDifficultyManager : MonoBehaviour
{
    private Queue<int> recentAttempts = new Queue<int>(); // 1 = hit, 0 = miss
    private const int WindowSize = 10;
    private const int IncreaseCount = 9;  // >=9/10 hits
    private const int DecreaseCount = 6;  // <=6/10 hits

    public float gameSpeed = 1.0f;
    public float speedStep = 0.1f;
    public float minSpeed = 0.5f;
    public float maxSpeed = 2.0f;

    public void RecordAttempt(bool wasHit)
    {
        recentAttempts.Enqueue(wasHit ? 1 : 0);
        if (recentAttempts.Count > WindowSize)
            recentAttempts.Dequeue();

        if (recentAttempts.Count < WindowSize)
            return; // not enough data yet, don't adjust

        int hits = 0;
        foreach (var a in recentAttempts) hits += a;

        if (hits >= IncreaseCount)
        {
            gameSpeed = Mathf.Min(gameSpeed + speedStep, maxSpeed);
            DifficultyUI.Instance.ShowChange("Difficulty Up!", gameSpeed);
        }
        else if (hits <= DecreaseCount)
        {
            gameSpeed = Mathf.Max(gameSpeed - speedStep, minSpeed);
            DifficultyUI.Instance.ShowChange("Difficulty Down", gameSpeed);
        }
        // else: 7 or 8 hits - no change
    }
}
```

Call `RecordAttempt(true/false)` every time a shot attempt resolves
(hit or miss), wire `gameSpeed` into wherever the game currently reads
its speed/difficulty variable.

---

## Part C: New UI

Two separate UI pieces are needed - a pre-game calibration panel, and
a small in-game difficulty indicator.

### 1. Calibration Panel (new, shown before gameplay starts)
A full-screen or centered panel shown only during the ~10s calibration:
- Large prompt text (swaps between "Relax your hand..." and "Contract
  as hard as you can!")
- A countdown timer or progress bar for each phase
- A brief result summary after calibration completes (e.g., "Threshold
  set: 502" ) before the "Start Game" button becomes active
- Should block/pause gameplay input until calibration finishes

```csharp
public class CalibrationUI : MonoBehaviour
{
    public static CalibrationUI Instance;
    public Text promptText;
    public Slider progressBar;
    public GameObject resultPanel;
    public Text resultText;
    public Button startGameButton;

    void Awake() { Instance = this; }

    public void ShowPrompt(string prompt, float duration)
    {
        promptText.text = prompt;
        StartCoroutine(AnimateProgress(duration));
    }

    private IEnumerator AnimateProgress(float duration)
    {
        float t = 0f;
        progressBar.value = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            progressBar.value = t / duration;
            yield return null;
        }
    }

    public void ShowResult(float rest, float max, float threshold)
    {
        resultPanel.SetActive(true);
        resultText.text = $"Calibration complete.\nThreshold set: {threshold:F0}";
        startGameButton.interactable = true;
    }
}
```

### 2. In-game difficulty indicator (small HUD addition)
A small, unobtrusive UI element in the corner of the existing gameplay
HUD (not a new full panel) showing:
- Current hit rate over the last 10 attempts, e.g. "Accuracy: 8/10"
- A brief on-screen flash/toast when difficulty changes: "Difficulty
  Up!" or "Difficulty Down" (1-2 second fade, non-blocking)

```csharp
public class DifficultyUI : MonoBehaviour
{
    public static DifficultyUI Instance;
    public Text accuracyText;
    public Text toastText;
    public float toastDuration = 1.5f;

    void Awake() { Instance = this; }

    public void UpdateAccuracy(int hits, int windowSize)
    {
        accuracyText.text = $"Accuracy: {hits}/{windowSize}";
    }

    public void ShowChange(string message, float newSpeed)
    {
        StopAllCoroutines();
        StartCoroutine(ShowToast(message));
    }

    private IEnumerator ShowToast(string message)
    {
        toastText.text = message;
        toastText.gameObject.SetActive(true);
        yield return new WaitForSeconds(toastDuration);
        toastText.gameObject.SetActive(false);
    }
}
```

Why the toast matters: the DDA literature notes players should be aware
their performance affects the challenge to stay motivated - a silent
difficulty change can feel confusing ("why did this suddenly get
harder?"), so the visible feedback is part of the mechanic, not just
polish.

---

## Summary of what to remove
- The old fixed EMG threshold constant (400) - replaced by
  `EmgCalibrator.SessionThreshold`.
- The old fixed "increase speed every 10-15 score points" logic -
  replaced by `AdaptiveDifficultyManager.RecordAttempt()`.

## Summary of what's new
- `EmgCalibrator` + `CalibrationUI` (pre-game calibration panel)
- `AdaptiveDifficultyManager` + `DifficultyUI` (in-game accuracy/toast HUD)
- Sidecar `_meta.json` file per session logging calibration values
