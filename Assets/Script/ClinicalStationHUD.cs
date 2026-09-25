using UnityEngine;
using System.Collections.Generic;

/*
Re9lay - Windows Patient Rehabilitation Dashboard
Tailored specifically for patient motivation and clinical biofeedback:
- Center Fly-in Trophy Celebration (10, 20, 30, 40, 50 Score Points).
- Left Sidebar: Weapon Capacitor (EMG biofeedback), Workout Mission (Score & Trophy Showcase Rack), Flow Streak.
- Right Sidebar: Live ROM Radar with MPU status & 10-Pip Rolling DDA window.
*/

public class ClinicalStationHUD : MonoBehaviour
{
    public static ClinicalStationHUD Instance { get; private set; }

    [Header("Workout Score & Milestone Trophies")]
    public float sessionElapsedTime = 0f;
    public float recommendedBreakInterval = 120f;
    public float timeUntilRecommendedBreak = 120f;

    [Header("Optional Custom Trophy Sprites")]
    public Texture2D bronzeTrophySprite;
    public Texture2D silverTrophySprite;
    public Texture2D goldTrophySprite;
    public Texture2D platinumTrophySprite;
    public Texture2D diamondTrophySprite;

    [Header("Hand Coach Sprite")]
    public Texture2D handCoachTexture;

    // Trophy Tiers: 10, 20, 30, 40, 50 score points
    public struct TrophyInfo
    {
        public int scoreRequirement;
        public string title;
        public string icon;
        public Color accentColor;
        public Texture2D customTexture;
        public Texture2D[] animFrames;

        public Texture2D GetCurrentFrame(float time, float fps = 3f)
        {
            if (animFrames != null && animFrames.Length > 0)
            {
                int index = Mathf.FloorToInt(time * fps) % animFrames.Length;
                return animFrames[index];
            }
            return customTexture;
        }
    }

    private readonly List<TrophyInfo> trophyTiers = new List<TrophyInfo>();
    private readonly HashSet<int> earnedTrophyScores = new HashSet<int>();

    // Center-to-Sidebar Fly Animation State
    private bool isTrophyAnimActive = false;
    private float trophyAnimTimer = 0f;
    private const float CenterCelebrationDuration = 1.6f;
    private const float FlyToSidebarDuration = 0.6f;
    private const float TotalAnimDuration = CenterCelebrationDuration + FlyToSidebarDuration;
    private TrophyInfo activeAnimTrophy;
    private Rect sidebarTargetDockRect = Rect.zero;

    [Header("Movement Smoothness (Jerk)")]
    public float currentJerkMagnitude = 0f;
    public string smoothnessStatus = "Smooth";

    private Vector3 prevPlayerPos = Vector3.zero;
    private Vector3 prevPlayerVel = Vector3.zero;
    private Vector3 prevPlayerAccel = Vector3.zero;

    // Procedural UI textures
    private Texture2D whiteTexture;
    private Texture2D panelBgTexture;

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;

        InitializeTrophyTiers();
        CreateProceduralTextures();
    }

    private float S(float basePx)
    {
        float scale = Mathf.Clamp(Screen.height / 720f, 1.0f, 2.5f);
        return basePx * scale;
    }

    private int F(int baseFontSize)
    {
        float scale = Mathf.Clamp(Screen.height / 720f, 1.0f, 2.5f);
        return Mathf.RoundToInt(baseFontSize * scale);
    }

    private Texture2D LoadTextureFromFile(string relativePath)
    {
        try
        {
            string fullPath = System.IO.Path.Combine(Application.dataPath, relativePath);
            if (System.IO.File.Exists(fullPath))
            {
                byte[] bytes = System.IO.File.ReadAllBytes(fullPath);
                Texture2D tex = new Texture2D(2, 2);
                if (tex.LoadImage(bytes))
                {
                    tex.filterMode = FilterMode.Bilinear;
                    return tex;
                }
            }
        }
        catch (System.Exception ex)
        {
            Debug.LogWarning($"[ClinicalStationHUD] Could not load texture at {relativePath}: {ex.Message}");
        }
        return null;
    }

    private Texture2D LoadTextureResource(string resourceName, string diskRelativePath)
    {
        // 1. Try loading from packaged Resources (Works in standalone builds and editor)
        Texture2D tex = Resources.Load<Texture2D>(resourceName);
        if (tex != null)
        {
            tex.filterMode = FilterMode.Bilinear;
            return tex;
        }

        // 2. Fallback to direct file loading from disk (Works in Editor)
        return LoadTextureFromFile(diskRelativePath);
    }

    private Texture2D[] LoadTrophyAnimation(string baseName, int frameCount)
    {
        List<Texture2D> frames = new List<Texture2D>();
        for (int i = 0; i < frameCount; i++)
        {
            Texture2D frame = LoadTextureResource($"{baseName}_{i}", $"Sprites/{baseName}_{i}.png");
            if (frame != null)
            {
                frames.Add(frame);
            }
        }
        return frames.Count > 0 ? frames.ToArray() : null;
    }

    void InitializeTrophyTiers()
    {
        Texture2D[] bronzeFrames = LoadTrophyAnimation("bronzetrophy", 2);
        Texture2D[] silverFrames = LoadTrophyAnimation("silvertrophy", 2);
        Texture2D[] goldFrames = LoadTrophyAnimation("goldtrophy", 2);
        Texture2D[] platFrames = LoadTrophyAnimation("plattrophy", 2);
        Texture2D[] diamondFrames = LoadTrophyAnimation("diamondtrophy", 4);

        if (bronzeTrophySprite == null) bronzeTrophySprite = (bronzeFrames != null && bronzeFrames.Length > 0) ? bronzeFrames[0] : LoadTextureResource("bronze_trophy", "Sprites/bronze_trophy.png");
        if (silverTrophySprite == null) silverTrophySprite = (silverFrames != null && silverFrames.Length > 0) ? silverFrames[0] : LoadTextureResource("Silver_trophy", "Sprites/Silver_trophy.png");
        if (goldTrophySprite == null) goldTrophySprite = (goldFrames != null && goldFrames.Length > 0) ? goldFrames[0] : LoadTextureResource("Gold_trophy", "Sprites/Gold_trophy.png");
        if (platinumTrophySprite == null) platinumTrophySprite = (platFrames != null && platFrames.Length > 0) ? platFrames[0] : LoadTextureResource("Platinum_trophy", "Sprites/Platinum_trophy.png");
        if (diamondTrophySprite == null) diamondTrophySprite = (diamondFrames != null && diamondFrames.Length > 0) ? diamondFrames[0] : LoadTextureResource("Diamond_Trophy", "Sprites/Diamond_Trophy.png");
        if (handCoachTexture == null) handCoachTexture = LoadTextureResource("Hand", "Sprites/Hand.png");

        trophyTiers.Clear();
        trophyTiers.Add(new TrophyInfo { scoreRequirement = 10, title = "Bronze Trophy", icon = "🥉", accentColor = new Color(0.85f, 0.55f, 0.3f), customTexture = bronzeTrophySprite, animFrames = bronzeFrames });
        trophyTiers.Add(new TrophyInfo { scoreRequirement = 20, title = "Silver Trophy", icon = "🥈", accentColor = new Color(0.8f, 0.85f, 0.95f), customTexture = silverTrophySprite, animFrames = silverFrames });
        trophyTiers.Add(new TrophyInfo { scoreRequirement = 30, title = "Gold Trophy", icon = "🥇", accentColor = new Color(1.0f, 0.84f, 0.0f), customTexture = goldTrophySprite, animFrames = goldFrames });
        trophyTiers.Add(new TrophyInfo { scoreRequirement = 40, title = "Platinum Trophy", icon = "💠", accentColor = new Color(0.4f, 0.9f, 1.0f), customTexture = platinumTrophySprite, animFrames = platFrames });
        trophyTiers.Add(new TrophyInfo { scoreRequirement = 50, title = "Diamond Trophy", icon = "💎", accentColor = new Color(0.7f, 0.5f, 1.0f), customTexture = diamondTrophySprite, animFrames = diamondFrames });
    }

    void CreateProceduralTextures()
    {
        whiteTexture = new Texture2D(1, 1);
        whiteTexture.SetPixel(0, 0, Color.white);
        whiteTexture.Apply();

        panelBgTexture = new Texture2D(1, 1);
        panelBgTexture.SetPixel(0, 0, new Color(0.05f, 0.08f, 0.14f, 0.94f));
        panelBgTexture.Apply();
    }

    public void ResetSessionMetrics()
    {
        sessionElapsedTime = 0f;
        timeUntilRecommendedBreak = recommendedBreakInterval;
        earnedTrophyScores.Clear();
        isTrophyAnimActive = false;
        trophyAnimTimer = 0f;

        if (BluetoothInputManager.Instance != null)
        {
            BluetoothInputManager.Instance.ResetPeakEnvelopes();
        }
    }

    void Update()
    {
        if (GUI.Instance == null || GUI.Instance.currentPanel != GUI.UIPanel.Gameplay || Time.timeScale <= 0f)
        {
            return;
        }

        sessionElapsedTime += Time.deltaTime;
        timeUntilRecommendedBreak = Mathf.Max(0f, timeUntilRecommendedBreak - Time.deltaTime);

        // Update Trophy Animation Timer
        if (isTrophyAnimActive)
        {
            trophyAnimTimer += Time.deltaTime;
            if (trophyAnimTimer >= TotalAnimDuration)
            {
                isTrophyAnimActive = false;
            }
        }

        // Check Score Milestone Trophies (Earned by scoring points)
        int currentScore = (GUI.Instance != null) ? GUI.Instance.currentScore : 0;
        CheckTrophyMilestones(currentScore);

        // Kinematic Smoothness (Jerk) Tracking
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null && Time.deltaTime > 0f)
        {
            Vector3 curPos = playerObj.transform.position;
            Vector3 curVel = (curPos - prevPlayerPos) / Time.deltaTime;
            Vector3 curAccel = (curVel - prevPlayerVel) / Time.deltaTime;
            Vector3 curJerk = (curAccel - prevPlayerAccel) / Time.deltaTime;

            currentJerkMagnitude = curJerk.magnitude;
            if (currentJerkMagnitude < 25f)
            {
                smoothnessStatus = "<color=lime>SMOOTH</color>";
            }
            else if (currentJerkMagnitude < 65f)
            {
                smoothnessStatus = "<color=yellow>MODERATE</color>";
            }
            else
            {
                smoothnessStatus = "<color=orange>ERRATIC</color>";
            }

            prevPlayerPos = curPos;
            prevPlayerVel = curVel;
            prevPlayerAccel = curAccel;
        }
    }

    private void CheckTrophyMilestones(int score)
    {
        foreach (var trophy in trophyTiers)
        {
            if (score >= trophy.scoreRequirement && !earnedTrophyScores.Contains(trophy.scoreRequirement))
            {
                earnedTrophyScores.Add(trophy.scoreRequirement);
                TriggerTrophyCelebration(trophy);
                break;
            }
        }
    }

    private void TriggerTrophyCelebration(TrophyInfo trophy)
    {
        activeAnimTrophy = trophy;
        isTrophyAnimActive = true;
        trophyAnimTimer = 0f;
    }

    void OnGUI()
    {
        if (DynamicScreenBoundaries.Instance == null || !DynamicScreenBoundaries.Instance.isPillarBoxed)
        {
            return;
        }

        if (GUI.Instance == null || GUI.Instance.currentPanel != GUI.UIPanel.Gameplay)
        {
            return;
        }

        if (handCoachTexture == null || trophyTiers == null || trophyTiers.Count == 0)
        {
            InitializeTrophyTiers();
        }
        if (whiteTexture == null || panelBgTexture == null)
        {
            CreateProceduralTextures();
        }

        float vpX = DynamicScreenBoundaries.Instance.viewportRect.x;
        float vpW = DynamicScreenBoundaries.Instance.viewportRect.width;

        float leftSidebarWidth = vpX * Screen.width;
        float rightSidebarX = (vpX + vpW) * Screen.width;
        float rightSidebarWidth = Screen.width - rightSidebarX;

        if (leftSidebarWidth < 140f || rightSidebarWidth < 140f)
        {
            return;
        }

        DrawLeftSidebar(new Rect(12f, 12f, leftSidebarWidth - 24f, Screen.height - 24f));
        DrawRightSidebar(new Rect(rightSidebarX + 12f, 12f, rightSidebarWidth - 24f, Screen.height - 24f));

        // Draw Front-and-Center Trophy Animation on top of gameplay
        if (isTrophyAnimActive)
        {
            DrawFlyingTrophyAnimation(leftSidebarWidth, rightSidebarX);
        }
    }

    // ==========================================
    // LEFT SIDEBAR: PATIENT MISSION & CAPACITOR
    // ==========================================
    private void DrawLeftSidebar(Rect area)
    {
        DrawBox(area, panelBgTexture, new Color(0.15f, 0.45f, 0.85f, 0.9f));

        GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));

        // Header
        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(14),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
        };
        GUILayout.Label("REHAB MISSION", titleStyle);

        GUIStyle subTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(10),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };
        GUILayout.Label("Patient Workout & Wellness Station", subTitleStyle);
        GUILayout.Space(S(5f));

        // --- 1. WEAPON CAPACITOR (EMG BIOFEEDBACK) ---
        DrawSectionHeader("⚡ WEAPON CAPACITOR");

        int liveEmg = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.emgValue : 0;
        bool is12Bit = (liveEmg > 1023) || (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.is12BitADC);
        float defaultMax = is12Bit ? 4095f : 1023f;
        float restBaseline = is12Bit ? 600f : 150f;
        float threshold = is12Bit ? 1600f : 400f;
        float maxContraction = is12Bit ? 3200f : 850f;

        if (EmgCalibrator.Instance != null && EmgCalibrator.Instance.isCalibrated)
        {
            restBaseline = EmgCalibrator.Instance.restBaseline;
            threshold = EmgCalibrator.Instance.sessionThreshold;
            maxContraction = Mathf.Max(EmgCalibrator.Instance.maxContraction, threshold + 100f);
        }
        else if (GameSettings.Instance != null)
        {
            threshold = (is12Bit && GameSettings.Instance.emgThreshold <= 1023) ? GameSettings.Instance.emgThreshold * 4 : GameSettings.Instance.emgThreshold;
        }

        // Live Charge Bar
        float barWidth = area.width - 24f;
        float barHeight = S(24f);
        Rect barRect = GUILayoutUtility.GetRect(barWidth, barHeight);
        DrawBox(barRect, whiteTexture, new Color(0.1f, 0.15f, 0.22f, 1f));

        float maxRange = Mathf.Max(defaultMax, maxContraction);
        float fillPct = Mathf.Clamp01(liveEmg / maxRange);
        Color barColor = (liveEmg >= threshold) ? new Color(1f, 0.25f, 0.25f, 1f) : new Color(0.2f, 0.8f, 1f, 1f);
        DrawBox(new Rect(barRect.x, barRect.y, barRect.width * fillPct, barRect.height), whiteTexture, barColor);

        // Markers
        float threshMarkerX = barRect.x + (threshold / maxRange) * barRect.width;
        DrawBox(new Rect(threshMarkerX - 1f, barRect.y - 2f, 3f, barRect.height + 4f), whiteTexture, Color.yellow);
        float restMarkerX = barRect.x + (restBaseline / maxRange) * barRect.width;
        DrawBox(new Rect(restMarkerX - 1f, barRect.y - 2f, 2f, barRect.height + 4f), whiteTexture, Color.cyan);

        // Weapon Status
        bool isArmed = true;
        bool isActivating = false;
        GameObject playerObj = GameObject.Find("Player");
        if (playerObj != null)
        {
            playerController pc = playerObj.GetComponent<playerController>();
            if (pc != null)
            {
                isArmed = pc.IsTriggerArmed;
                isActivating = pc.IsActivating;
            }
        }

        string triggerBadge;
        if (isActivating && !isArmed)
        {
            triggerBadge = "<color=orange><b>⚠ RELAX HAND TO RELOAD</b></color>";
        }
        else if (isActivating && isArmed)
        {
            triggerBadge = "<color=red><b>⚡ FIRING LASER!</b></color>";
        }
        else
        {
            triggerBadge = "<color=lime><b>✔ READY TO BLAST!</b></color>";
        }

        GUIStyle badgeStyle = new GUIStyle(UnityEngine.GUI.skin.box)
        {
            fontSize = F(11),
            richText = true,
            alignment = TextAnchor.MiddleCenter
        };
        GUILayout.Box(triggerBadge, badgeStyle, GUILayout.Height(S(28f)));
        GUILayout.Space(S(6f));

        // --- 2. WORKOUT SCORE & NEXT TROPHY GOAL ---
        DrawSectionHeader("🏆 SCORE & TROPHIES");

        int currentScore = (GUI.Instance != null) ? GUI.Instance.currentScore : 0;

        // Next Trophy Goal Calculation
        TrophyInfo nextTrophy = trophyTiers[trophyTiers.Count - 1];
        foreach (var t in trophyTiers)
        {
            if (currentScore < t.scoreRequirement)
            {
                nextTrophy = t;
                break;
            }
        }

        GUIStyle scoreCounterStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(11),
            richText = true,
            normal = { textColor = Color.white }
        };
        GUILayout.Label($"Total Score: <b><size={F(14)}><color=yellow>{currentScore}</color></size></b> pts", scoreCounterStyle);

        float goalProgress = Mathf.Clamp01((float)currentScore / nextTrophy.scoreRequirement);
        GUILayout.Label($"Next Target: <b>{nextTrophy.icon} {nextTrophy.title}</b> ({currentScore}/{nextTrophy.scoreRequirement} pts)", scoreCounterStyle);

        Rect progRect = GUILayoutUtility.GetRect(area.width - 24f, S(14f));
        DrawBox(progRect, whiteTexture, new Color(0.15f, 0.2f, 0.3f, 1f));
        DrawBox(new Rect(progRect.x, progRect.y, progRect.width * goalProgress, progRect.height), whiteTexture, nextTrophy.accentColor);
        GUILayout.Space(S(6f));

        // --- 3. TROPHY SHOWCASE RACK ---
        Rect dockRackRect = GUILayoutUtility.GetRect(area.width - 24f, S(78f));
        sidebarTargetDockRect = dockRackRect; // Target anchor for the fly-in animation
        DrawTrophyRack(dockRackRect);
        GUILayout.Space(S(8f));

        // --- 4. FLOW STREAK (EXPANDED DEDICATED CARD) ---
        DrawSectionHeader("🔥 FLOW STREAK & COMBO");
        DrawFlowStreakCard(area.width - 24f);
        GUILayout.Space(S(8f));

        // --- 5. PACING & WELLNESS REST STATION (EXPANDED DEDICATED CARD) ---
        DrawSectionHeader("☕ REST & PACING STATION");
        DrawRestStationCard(area.width - 24f);

        GUILayout.EndArea();
    }

    private void DrawFlowStreakCard(float width)
    {
        float cardHeight = S(112f);
        Rect r = GUILayoutUtility.GetRect(width, cardHeight);
        DrawBox(r, whiteTexture, new Color(0.08f, 0.12f, 0.18f, 0.95f));
        DrawWireBox(r, new Color(0.9f, 0.6f, 0.15f, 0.7f), 1f);

        int streak = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.currentFlowStreak : 0;
        int maxStreak = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.maxFlowStreak : 0;

        string streakTitle;
        string feedbackMsg;
        Color streakColor;

        if (streak >= 10)
        {
            streakTitle = $"🔥 <size={F(15)}><b>{streak}x HIT STREAK!</b></size>";
            feedbackMsg = "<color=lime><b>🌟 UNSTOPPABLE NEURO-MASTERY!</b></color>";
            streakColor = Color.yellow;
        }
        else if (streak >= 5)
        {
            streakTitle = $"🔥 <size={F(14)}><b>{streak}x HIT STREAK!</b></size>";
            feedbackMsg = "<color=cyan><b>⚡ SUPER MOTOR FLOW!</b></color>";
            streakColor = Color.yellow;
        }
        else if (streak >= 3)
        {
            streakTitle = $"🔥 <size={F(13)}><b>{streak}x HIT STREAK!</b></size>";
            feedbackMsg = "<color=yellow><b>🎯 GREAT AIM & TIMING!</b></color>";
            streakColor = Color.yellow;
        }
        else if (streak > 0)
        {
            streakTitle = $"<b>{streak}x Streak Started</b>";
            feedbackMsg = "<color=silver>Hit consecutive targets to reach 3x!</color>";
            streakColor = Color.white;
        }
        else
        {
            streakTitle = "<color=gray>Ready for Next Combo</color>";
            feedbackMsg = "<color=gray>Hit incoming aliens to build flow streak.</color>";
            streakColor = Color.gray;
        }

        GUIStyle streakTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(12),
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = streakColor }
        };
        UnityEngine.GUI.Label(new Rect(r.x + 6f, r.y + S(5f), r.width - 12f, S(24f)), streakTitle, streakTitleStyle);

        GUIStyle feedbackStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            alignment = TextAnchor.MiddleCenter,
            richText = true
        };
        UnityEngine.GUI.Label(new Rect(r.x + 6f, r.y + S(28f), r.width - 12f, S(18f)), feedbackMsg, feedbackStyle);

        // Next Streak Tier Progress Bar
        int nextTier = (streak < 3) ? 3 : ((streak < 5) ? 5 : ((streak < 10) ? 10 : streak + 5));
        int prevTier = (streak < 3) ? 0 : ((streak < 5) ? 3 : 5);
        float tierProgress = Mathf.Clamp01((float)(streak - prevTier) / Mathf.Max(1, nextTier - prevTier));

        Rect barRect = new Rect(r.x + 12f, r.y + S(48f), r.width - 24f, S(15f));
        DrawBox(barRect, whiteTexture, new Color(0.15f, 0.2f, 0.28f, 1f));
        DrawBox(new Rect(barRect.x, barRect.y, barRect.width * tierProgress, barRect.height), whiteTexture, new Color(1f, 0.6f, 0.1f, 1f));

        GUIStyle tierLabelStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        UnityEngine.GUI.Label(barRect, $"Goal: {nextTier}x Hits", tierLabelStyle);

        // Best Session Streak Banner
        GUIStyle bestStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(10),
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = Color.white }
        };
        UnityEngine.GUI.Label(new Rect(r.x + 6f, r.y + S(72f), r.width - 12f, S(24f)), $"⭐ Best Session Streak: <b><color=yellow>{maxStreak}x</color></b>", bestStyle);
    }

    private void DrawRestStationCard(float width)
    {
        float cardHeight = S(142f);
        Rect r = GUILayoutUtility.GetRect(width, cardHeight);
        DrawBox(r, whiteTexture, new Color(0.06f, 0.12f, 0.16f, 0.95f));
        DrawWireBox(r, new Color(0.2f, 0.7f, 0.85f, 0.65f), 1f);

        int bMin = Mathf.FloorToInt(timeUntilRecommendedBreak / 60f);
        int bSec = Mathf.FloorToInt(timeUntilRecommendedBreak % 60f);

        // Big Digital Clock
        GUIStyle clockStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(20),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = (timeUntilRecommendedBreak <= 0f) ? Color.yellow : Color.cyan }
        };
        UnityEngine.GUI.Label(new Rect(r.x + 6f, r.y + S(6f), r.width - 12f, S(26f)), $"{bMin:00}:{bSec:00}", clockStyle);

        GUIStyle subClockStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray }
        };
        UnityEngine.GUI.Label(new Rect(r.x + 6f, r.y + S(32f), r.width - 12f, S(16f)), "Time Until Recommended Break", subClockStyle);

        // Rehabilitation Advice Box
        GUIStyle adviceStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            alignment = TextAnchor.MiddleCenter,
            wordWrap = true,
            richText = true,
            normal = { textColor = Color.white }
        };

        if (timeUntilRecommendedBreak <= 0f)
        {
            UnityEngine.GUI.Label(new Rect(r.x + 8f, r.y + S(48f), r.width - 16f, S(44f)), "<color=yellow><b>☕ TIME FOR A QUICK BREATHER!</b>\nPause and gently stretch fingers open to release spasticity tone.</color>", adviceStyle);
        }
        else
        {
            UnityEngine.GUI.Label(new Rect(r.x + 8f, r.y + S(50f), r.width - 16f, S(38f)), "💡 <i>Pacing Tip: Keep forearm supported on table to minimize fatigue between rounds.</i>", adviceStyle);
        }

        // Interactive Button
        GUIStyle pauseBtnStyle = new GUIStyle(UnityEngine.GUI.skin.button)
        {
            fontSize = F(11),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.cyan }
        };

        Rect btnRect = new Rect(r.x + 10f, r.y + S(96f), r.width - 20f, S(34f));
        if (UnityEngine.GUI.Button(btnRect, "☕ TAKE A BREATHER (PAUSE)", pauseBtnStyle))
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }
    }

    // ==========================================
    // TROPHY SHOWCASE RACK
    // ==========================================
    private void DrawTrophyRack(Rect r)
    {
        DrawBox(r, whiteTexture, new Color(0.08f, 0.12f, 0.18f, 0.9f));
        DrawWireBox(r, new Color(0.2f, 0.4f, 0.6f, 0.6f), 1f);

        int count = trophyTiers.Count;
        float slotW = (r.width - 12f) / count;
        float slotH = r.height - 8f;

        for (int i = 0; i < count; i++)
        {
            var trophy = trophyTiers[i];
            bool isEarned = earnedTrophyScores.Contains(trophy.scoreRequirement);

            Rect sRect = new Rect(r.x + 6f + i * slotW, r.y + 4f, slotW - 4f, slotH);

            Color bgColor = isEarned ? new Color(trophy.accentColor.r * 0.3f, trophy.accentColor.g * 0.3f, trophy.accentColor.b * 0.3f, 0.9f) : new Color(0.12f, 0.15f, 0.2f, 0.6f);
            DrawBox(sRect, whiteTexture, bgColor);

            Color borderColor = isEarned ? trophy.accentColor : new Color(0.3f, 0.35f, 0.4f, 0.5f);
            DrawWireBox(sRect, borderColor, isEarned ? 1.5f : 1f);

            float iconDim = S(28f);
            Texture2D curFrame = trophy.GetCurrentFrame(Time.unscaledTime, 3.5f);
            if (isEarned && curFrame != null)
            {
                Rect imgRect = new Rect(sRect.x + (sRect.width - iconDim) / 2f, sRect.y + S(3f), iconDim, iconDim);
                UnityEngine.GUI.DrawTexture(imgRect, curFrame, ScaleMode.ScaleToFit);
            }
            else
            {
                GUIStyle iconStyle = new GUIStyle(UnityEngine.GUI.skin.label)
                {
                    fontSize = isEarned ? F(16) : F(12),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = isEarned ? Color.white : Color.gray }
                };

                string iconText = isEarned ? trophy.icon : "🔒";
                UnityEngine.GUI.Label(new Rect(sRect.x, sRect.y + S(3f), sRect.width, S(26f)), iconText, iconStyle);
            }

            GUIStyle nameStyle = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = F(9),
                fontStyle = isEarned ? FontStyle.Bold : FontStyle.Normal,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = isEarned ? trophy.accentColor : Color.gray }
            };
            string shortName = trophy.title.Split(' ')[0];
            UnityEngine.GUI.Label(new Rect(sRect.x, sRect.y + S(33f), sRect.width, S(16f)), shortName, nameStyle);

            GUIStyle scoreStyle = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = F(8),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            UnityEngine.GUI.Label(new Rect(sRect.x, sRect.y + S(50f), sRect.width, S(14f)), $"{trophy.scoreRequirement}p", scoreStyle);
        }
    }

    // ==========================================
    // FRONT-AND-CENTER FLYING TROPHY CELEBRATION
    // ==========================================
    private void DrawFlyingTrophyAnimation(float leftSidebarWidth, float rightSidebarX)
    {
        float centerGameWidth = rightSidebarX - leftSidebarWidth;
        Vector2 screenCenter = new Vector2(leftSidebarWidth + centerGameWidth / 2f, Screen.height / 2f - 40f);

        Rect animRect;

        if (trophyAnimTimer <= CenterCelebrationDuration)
        {
            // Phase 1: Center Celebration with Elastic Scale
            float normT = trophyAnimTimer / CenterCelebrationDuration;
            float scale = (normT < 0.2f) ? Mathf.Lerp(0f, 1.25f, normT / 0.2f) : Mathf.Lerp(1.25f, 1.0f, (normT - 0.2f) / 0.8f);

            float w = 250f * scale;
            float h = 135f * scale;
            animRect = new Rect(screenCenter.x - w / 2f, screenCenter.y - h / 2f, w, h);

            // Glowing Celebratory Card
            DrawBox(animRect, whiteTexture, new Color(0.04f, 0.08f, 0.16f, 0.96f));
            DrawWireBox(animRect, activeAnimTrophy.accentColor, 3f);

            GUIStyle unlockHeader = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(13 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.yellow }
            };
            UnityEngine.GUI.Label(new Rect(animRect.x, animRect.y + 8f * scale, animRect.width, 24f * scale), "✨ TROPHY UNLOCKED! ✨", unlockHeader);

            Texture2D curAnimFrame = activeAnimTrophy.GetCurrentFrame(trophyAnimTimer, 4f);
            if (curAnimFrame != null)
            {
                Rect imgRect = new Rect(animRect.x + (animRect.width - 56f * scale) / 2f, animRect.y + 26f * scale, 56f * scale, 56f * scale);
                UnityEngine.GUI.DrawTexture(imgRect, curAnimFrame, ScaleMode.ScaleToFit);
            }
            else
            {
                GUIStyle iconBig = new GUIStyle(UnityEngine.GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(34 * scale),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                UnityEngine.GUI.Label(new Rect(animRect.x, animRect.y + 30f * scale, animRect.width, 45f * scale), activeAnimTrophy.icon, iconBig);
            }

            GUIStyle trophyTitle = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(15 * scale),
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = activeAnimTrophy.accentColor }
            };
            UnityEngine.GUI.Label(new Rect(animRect.x, animRect.y + 78f * scale, animRect.width, 26f * scale), activeAnimTrophy.title, trophyTitle);

            GUIStyle scoreCongrats = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = Mathf.RoundToInt(10 * scale),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.cyan }
            };
            UnityEngine.GUI.Label(new Rect(animRect.x, animRect.y + 104f * scale, animRect.width, 20f * scale), $"{activeAnimTrophy.scoreRequirement} Points Reached! Targets Destroyed!", scoreCongrats);
        }
        else
        {
            // Phase 2: Smooth Glide to Sidebar Target Rack
            float flyT = (trophyAnimTimer - CenterCelebrationDuration) / FlyToSidebarDuration;
            flyT = Mathf.Clamp01(flyT);
            float smoothT = flyT * flyT * (3f - 2f * flyT); // Smooth cubic ease

            Vector2 targetPos = new Vector2(sidebarTargetDockRect.x + sidebarTargetDockRect.width / 2f, sidebarTargetDockRect.y + sidebarTargetDockRect.height / 2f);
            Vector2 curPos = Vector2.Lerp(screenCenter, targetPos, smoothT);

            float curW = Mathf.Lerp(250f, 48f, smoothT);
            float curH = Mathf.Lerp(135f, 48f, smoothT);

            animRect = new Rect(curPos.x - curW / 2f, curPos.y - curH / 2f, curW, curH);

            DrawBox(animRect, whiteTexture, new Color(activeAnimTrophy.accentColor.r * 0.3f, activeAnimTrophy.accentColor.g * 0.3f, activeAnimTrophy.accentColor.b * 0.3f, 0.95f));
            DrawWireBox(animRect, activeAnimTrophy.accentColor, 2f);

            Texture2D curFlyFrame = activeAnimTrophy.GetCurrentFrame(trophyAnimTimer, 4f);
            if (curFlyFrame != null)
            {
                Rect imgRect = new Rect(animRect.x + 4f, animRect.y + 4f, animRect.width - 8f, animRect.height - 8f);
                UnityEngine.GUI.DrawTexture(imgRect, curFlyFrame, ScaleMode.ScaleToFit);
            }
            else
            {
                GUIStyle flyingIcon = new GUIStyle(UnityEngine.GUI.skin.label)
                {
                    fontSize = Mathf.RoundToInt(Mathf.Lerp(30f, 16f, smoothT)),
                    alignment = TextAnchor.MiddleCenter,
                    normal = { textColor = Color.white }
                };
                UnityEngine.GUI.Label(animRect, activeAnimTrophy.icon, flyingIcon);
            }
        }
    }

    // ==========================================
    // RIGHT SIDEBAR: WRIST TILT COACH STATION
    // ==========================================
    private void DrawRightSidebar(Rect area)
    {
        DrawBox(area, panelBgTexture, new Color(0.15f, 0.45f, 0.85f, 0.9f));

        GUILayout.BeginArea(new Rect(area.x + 12f, area.y + 10f, area.width - 24f, area.height - 20f));

        // Station Header
        GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(14),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.cyan }
        };
        GUILayout.Label("WRIST TILT COACH", titleStyle);

        // MPU Sensor Connection Status Badge
        bool isBT = (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.isConnected && !BluetoothInputManager.Instance.useSimulation);
        string mpuStatusStr = isBT
            ? "<color=lime>● MPU ACTIVE (LIVE GLOVE)</color>"
            : "<color=yellow>○ MPU SIMULATION (WASD)</color>";

        GUIStyle mpuBadgeStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            richText = true,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.white }
        };
        GUILayout.Label(mpuStatusStr, mpuBadgeStyle);
        GUILayout.Space(S(5f));

        // --- 1. BIG WRIST TILT ARENA (UP, DOWN, LEFT, RIGHT) ---
        DrawSectionHeader("✋ LIVE 2-AXIS WRIST TILT");
        DrawBigWristTiltArena(area.width - 24f);
        GUILayout.Space(S(6f));

        // --- 2. DUAL-AXIS TILT METERS & GUIDANCE ---
        DrawDualAxisTiltMeters(area.width - 24f);
        GUILayout.Space(S(6f));

        // --- 3. 10-PIP DDA ACCURACY ROLLING QUEUE ---
        DrawSectionHeader("🎯 10-SHOT DDA ROLLING WINDOW");

        DrawDDA10PipQueue(area.width - 24f);
        GUILayout.Space(S(5f));

        // Adaptation Metrics
        float speed = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.CurrentSpeedMultiplier : 1.0f;
        float spawn = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.CurrentSpawnInterval : 5.0f;

        int sessHits = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalSessionHits : 0;
        int sessAtts = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.TotalSessionAttempts : 0;
        int accuracyPct = (sessAtts > 0) ? Mathf.RoundToInt((float)sessHits / sessAtts * 100f) : 100;

        GUIStyle ddaStatStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            richText = true,
            normal = { textColor = Color.white }
        };

        GUILayout.Label($"• Session Accuracy: <b>{accuracyPct}%</b> ({sessHits}/{sessAtts})", ddaStatStyle);
        GUILayout.Label($"• Game Speed: <b>{speed:F2}x</b>", ddaStatStyle);
        GUILayout.Label($"• Alien Spawn Interval: <b>{spawn:F1}s</b>", ddaStatStyle);
        GUILayout.Label($"• Movement Smoothness: {smoothnessStatus}", ddaStatStyle);

        GUILayout.EndArea();
    }

    private void DrawBigWristTiltArena(float availableWidth)
    {
        float arenaHeight = S(240f);
        Rect r = GUILayoutUtility.GetRect(availableWidth, arenaHeight);
        DrawBox(r, whiteTexture, new Color(0.06f, 0.09f, 0.15f, 0.95f));
        DrawWireBox(r, new Color(0.2f, 0.55f, 0.85f, 0.7f), 1.5f);

        float curPitch = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.currentDeltaPitch : 0f;
        float curRoll = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.currentDeltaRoll : 0f;
        float threshPitch = (GameSettings.Instance != null) ? GameSettings.Instance.deltaPitchThreshold : 30f;
        float threshRoll = (GameSettings.Instance != null) ? GameSettings.Instance.deltaRollThreshold : 40f;

        bool isUpTilt = curRoll > threshRoll;
        bool isDownTilt = curRoll < -threshRoll;
        bool isRightTilt = curPitch > threshPitch;
        bool isLeftTilt = curPitch < -threshPitch;

        // 1. TOP: UPPER TILT BANNER
        Color upBannerCol = isUpTilt ? new Color(1f, 0.85f, 0.1f, 0.9f) : new Color(0.12f, 0.18f, 0.25f, 0.6f);
        Rect upRect = new Rect(r.x + 8f, r.y + S(6f), r.width - 16f, S(22f));
        DrawBox(upRect, whiteTexture, upBannerCol);
        DrawWireBox(upRect, isUpTilt ? Color.yellow : new Color(0.3f, 0.4f, 0.5f, 0.4f), 1f);

        GUIStyle upStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(10),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = isUpTilt ? Color.black : (curRoll > 0f ? Color.cyan : Color.gray) }
        };
        string upText = isUpTilt
            ? $"▲ UPPER TILT ACTIVE (+{curRoll:F0}° EXTENSION)"
            : $"▲ UPPER TILT (UP: +{Mathf.Max(0f, curRoll):F0}° / {threshRoll:F0}°)";
        UnityEngine.GUI.Label(upRect, upText, upStyle);

        // 2. BOTTOM: DOWN TILT BANNER
        Color downBannerCol = isDownTilt ? new Color(1f, 0.5f, 0.1f, 0.9f) : new Color(0.12f, 0.18f, 0.25f, 0.6f);
        Rect downRect = new Rect(r.x + 8f, r.y + r.height - S(28f), r.width - 16f, S(22f));
        DrawBox(downRect, whiteTexture, downBannerCol);
        DrawWireBox(downRect, isDownTilt ? new Color(1f, 0.6f, 0.2f) : new Color(0.3f, 0.4f, 0.5f, 0.4f), 1f);

        GUIStyle downStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(10),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = isDownTilt ? Color.black : (curRoll < 0f ? Color.yellow : Color.gray) }
        };
        string downText = isDownTilt
            ? $"▼ DOWN TILT ACTIVE ({curRoll:F0}° FLEXION)"
            : $"▼ DOWN TILT (DOWN: {Mathf.Min(0f, curRoll):F0}° / -{threshRoll:F0}°)";
        UnityEngine.GUI.Label(downRect, downText, downStyle);

        // 3. FLANKS: LEFT & RIGHT TILT LABELS
        GUIStyle flankStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleLeft,
            normal = { textColor = isLeftTilt ? Color.green : Color.gray }
        };
        UnityEngine.GUI.Label(new Rect(r.x + 8f, r.y + S(32f), S(85f), S(18f)), isLeftTilt ? $"◄ LEFT {curPitch:F0}°" : "◄ LEFT", flankStyle);

        flankStyle.alignment = TextAnchor.MiddleRight;
        flankStyle.normal.textColor = isRightTilt ? Color.green : Color.gray;
        UnityEngine.GUI.Label(new Rect(r.x + r.width - S(93f), r.y + S(32f), S(85f), S(18f)), isRightTilt ? $"RIGHT +{curPitch:F0}° ►" : "RIGHT ►", flankStyle);

        // 4. CENTER AREA: NEUTRAL GUIDE CROSSHAIR & BIG HAND SPRITE
        float arenaCenterY = r.y + (r.height / 2f) - 2f;
        float arenaCenterX = r.x + (r.width / 2f);

        // Neutral origin crosshair
        DrawBox(new Rect(arenaCenterX - S(35f), arenaCenterY - 0.5f, S(70f), 1f), whiteTexture, new Color(0.25f, 0.35f, 0.45f, 0.5f));
        DrawBox(new Rect(arenaCenterX - 0.5f, arenaCenterY - S(35f), 1f, S(70f)), whiteTexture, new Color(0.25f, 0.35f, 0.45f, 0.5f));

        // Dynamic Hand Translation based on Upper/Down and Left/Right tilt
        float vertOffset = -Mathf.Clamp((curRoll / 60f) * S(28f), -S(28f), S(28f));
        float horizOffset = Mathf.Clamp((curPitch / 60f) * S(25f), -S(25f), S(25f));

        // Big Hand size: S(115f)
        float handSize = S(115f);
        Rect handRect = new Rect(arenaCenterX - handSize / 2f + horizOffset, arenaCenterY - handSize / 2f + vertOffset, handSize, handSize);
        Vector2 pivot = new Vector2(handRect.x + handRect.width / 2f, handRect.y + handRect.height / 2f);

        // Rotate hand with Pitch (steering left/right)
        float tiltAngle = Mathf.Clamp(curPitch, -55f, 55f);
        Matrix4x4 savedMat = UnityEngine.GUI.matrix;
        GUIUtility.RotateAroundPivot(tiltAngle, pivot);

        if (handCoachTexture != null)
        {
            UnityEngine.GUI.DrawTexture(handRect, handCoachTexture, ScaleMode.ScaleToFit);
        }
        else
        {
            GUIStyle fallbackStyle = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = F(48),
                alignment = TextAnchor.MiddleCenter
            };
            UnityEngine.GUI.Label(handRect, "✋", fallbackStyle);
        }
        UnityEngine.GUI.matrix = savedMat;
    }

    private void DrawDualAxisTiltMeters(float availableWidth)
    {
        float curPitch = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.currentDeltaPitch : 0f;
        float curRoll = (BluetoothInputManager.Instance != null) ? BluetoothInputManager.Instance.currentDeltaRoll : 0f;
        float threshPitch = (GameSettings.Instance != null) ? GameSettings.Instance.deltaPitchThreshold : 30f;
        float threshRoll = (GameSettings.Instance != null) ? GameSettings.Instance.deltaRollThreshold : 40f;

        // Current Action Status Banner
        string actionMsg;
        Color actionColor;

        if (curRoll > threshRoll && Mathf.Abs(curPitch) > threshPitch)
        {
            actionMsg = (curPitch > 0f) ? "↗ UPPER + RIGHT DIAGONAL TILT" : "↖ UPPER + LEFT DIAGONAL TILT";
            actionColor = Color.cyan;
        }
        else if (curRoll < -threshRoll && Mathf.Abs(curPitch) > threshPitch)
        {
            actionMsg = (curPitch > 0f) ? "↘ DOWN + RIGHT DIAGONAL TILT" : "↙ DOWN + LEFT DIAGONAL TILT";
            actionColor = Color.yellow;
        }
        else if (curRoll > threshRoll)
        {
            actionMsg = $"▲ UPPER TILT (+{curRoll:F0}°) - SHIP MOVING UP";
            actionColor = Color.yellow;
        }
        else if (curRoll < -threshRoll)
        {
            actionMsg = $"▼ DOWN TILT ({curRoll:F0}°) - SHIP MOVING DOWN";
            actionColor = new Color(1f, 0.6f, 0.2f);
        }
        else if (curPitch > threshPitch)
        {
            actionMsg = $"👉 RIGHT TILT (+{curPitch:F0}°) - STEERING RIGHT";
            actionColor = Color.green;
        }
        else if (curPitch < -threshPitch)
        {
            actionMsg = $"👈 LEFT TILT ({curPitch:F0}°) - STEERING LEFT";
            actionColor = Color.green;
        }
        else
        {
            actionMsg = "✋ NEUTRAL REST POSITION (0°, 0°)";
            actionColor = Color.white;
        }

        GUIStyle actionStyle = new GUIStyle(UnityEngine.GUI.skin.box)
        {
            fontSize = F(10),
            fontStyle = FontStyle.Bold,
            alignment = TextAnchor.MiddleCenter,
            richText = true,
            normal = { textColor = actionColor }
        };
        GUILayout.Box(actionMsg, actionStyle, GUILayout.Height(S(26f)));
        GUILayout.Space(S(4f));

        // 1. Roll Level Meter (Upper / Down)
        GUIStyle meterTitleStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(9),
            normal = { textColor = Color.gray }
        };
        GUILayout.Label($"Vertical Tilt (Roll / Up-Down): <b><color=yellow>{curRoll:F0}°</color></b>", meterTitleStyle);
        Rect rollBarRect = GUILayoutUtility.GetRect(availableWidth, S(11f));
        DrawBiDirectionalBar(rollBarRect, curRoll, -60f, 60f, (Mathf.Abs(curRoll) >= threshRoll) ? Color.yellow : Color.cyan);
        GUILayout.Space(S(3f));

        // 2. Pitch Level Meter (Left / Right)
        GUILayout.Label($"Horizontal Tilt (Pitch / Left-Right): <b><color=lime>{curPitch:F0}°</color></b>", meterTitleStyle);
        Rect pitchBarRect = GUILayoutUtility.GetRect(availableWidth, S(11f));
        DrawBiDirectionalBar(pitchBarRect, curPitch, -60f, 60f, (Mathf.Abs(curPitch) >= threshPitch) ? Color.green : Color.cyan);
        GUILayout.Space(S(4f));

        // 3. Peak ROM Records summary
        float peakL = 0f, peakR = 0f, peakU = 0f, peakD = 0f;
        if (BluetoothInputManager.Instance != null)
        {
            peakL = BluetoothInputManager.Instance.peakLeftPitch;
            peakR = BluetoothInputManager.Instance.peakRightPitch;
            peakU = BluetoothInputManager.Instance.peakUpRoll;
            peakD = BluetoothInputManager.Instance.peakDownRoll;
        }

        GUIStyle peakSummaryStyle = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(8),
            alignment = TextAnchor.MiddleCenter,
            normal = { textColor = Color.gray },
            richText = true
        };
        GUILayout.Label($"Session Max: <color=yellow>▲ Up: +{peakU:F0}°</color> | <color=yellow>▼ Down: {peakD:F0}°</color> | <color=lime>◄ {peakL:F0}°</color> to <color=lime>+{peakR:F0}° ►</color>", peakSummaryStyle);
    }

    private void DrawBiDirectionalBar(Rect rect, float value, float minVal, float maxVal, Color activeColor)
    {
        DrawBox(rect, whiteTexture, new Color(0.12f, 0.16f, 0.22f, 1f));
        float centerX = rect.x + rect.width / 2f;
        DrawBox(new Rect(centerX - 1f, rect.y, 2f, rect.height), whiteTexture, Color.gray);

        float maxRange = Mathf.Max(Mathf.Abs(minVal), Mathf.Abs(maxVal));
        float norm = Mathf.Clamp(value / maxRange, -1f, 1f);
        if (norm > 0f)
        {
            float w = (rect.width / 2f) * norm;
            DrawBox(new Rect(centerX, rect.y + 1f, w, rect.height - 2f), whiteTexture, activeColor);
        }
        else if (norm < 0f)
        {
            float w = (rect.width / 2f) * Mathf.Abs(norm);
            DrawBox(new Rect(centerX - w, rect.y + 1f, w, rect.height - 2f), whiteTexture, activeColor);
        }
    }

    // ==========================================
    // 10-PIP DDA ROLLING QUEUE WIDGET
    // ==========================================
    private void DrawDDA10PipQueue(float availableWidth)
    {
        int windowSize = 10;
        float pipSize = S(18f);
        float spacing = (availableWidth - (windowSize * pipSize)) / (windowSize - 1);
        spacing = Mathf.Clamp(spacing, 2f, 10f);

        Rect queueRect = GUILayoutUtility.GetRect(availableWidth, pipSize + 4f);
        float startX = queueRect.x + (availableWidth - (windowSize * pipSize + (windowSize - 1) * spacing)) / 2f;

        List<int> attempts = new List<int>();
        if (DifficultyManager.Instance != null)
        {
            attempts = DifficultyManager.Instance.GetRecentAttemptsList();
        }

        for (int i = 0; i < windowSize; i++)
        {
            float pipX = startX + i * (pipSize + spacing);
            Rect pRect = new Rect(pipX, queueRect.y + 2f, pipSize, pipSize);

            Color pipColor = new Color(0.2f, 0.25f, 0.35f, 0.8f);
            string pipText = "·";

            if (i < attempts.Count)
            {
                if (attempts[i] == 1)
                {
                    pipColor = new Color(0.15f, 0.85f, 0.3f, 1f);
                    pipText = "✔";
                }
                else
                {
                    pipColor = new Color(0.9f, 0.2f, 0.2f, 1f);
                    pipText = "✖";
                }
            }

            DrawBox(pRect, whiteTexture, pipColor);

            GUIStyle pipTextStyle = new GUIStyle(UnityEngine.GUI.skin.label)
            {
                fontSize = F(10),
                alignment = TextAnchor.MiddleCenter,
                normal = { textColor = Color.white }
            };
            UnityEngine.GUI.Label(pRect, pipText, pipTextStyle);
        }
    }

    // ==========================================
    // UTILITY DRAWING HELPERS
    // ==========================================
    private void DrawSectionHeader(string title)
    {
        GUIStyle style = new GUIStyle(UnityEngine.GUI.skin.label)
        {
            fontSize = F(10),
            fontStyle = FontStyle.Bold,
            normal = { textColor = Color.yellow }
        };
        GUILayout.Label(title, style);
        GUILayout.Space(S(2f));
    }

    private void DrawBox(Rect r, Texture2D tex, Color col)
    {
        Color oldCol = UnityEngine.GUI.color;
        UnityEngine.GUI.color = col;
        UnityEngine.GUI.DrawTexture(r, tex);
        UnityEngine.GUI.color = oldCol;
    }

    private void DrawWireBox(Rect r, Color col, float lineWidth)
    {
        DrawBox(new Rect(r.x, r.y, r.width, lineWidth), whiteTexture, col);
        DrawBox(new Rect(r.x, r.y + r.height - lineWidth, r.width, lineWidth), whiteTexture, col);
        DrawBox(new Rect(r.x, r.y, lineWidth, r.height), whiteTexture, col);
        DrawBox(new Rect(r.x + r.width - lineWidth, r.y, lineWidth, r.height), whiteTexture, col);
    }
}
