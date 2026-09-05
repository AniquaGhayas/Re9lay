/*
Re9lay - Cloud PDF Report Uploader & Viewer
Connects Unity session CSV logs to the Render FastAPI report generation service.
*/

using System;
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ReportUploader : MonoBehaviour
{
    private static ReportUploader _instance;
    public static ReportUploader Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindObjectOfType<ReportUploader>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("ReportUploader");
                    _instance = go.AddComponent<ReportUploader>();
                    DontDestroyOnLoad(go);
                }
            }
            return _instance;
        }
    }

    [Header("Render API Endpoint")]
    public string apiBaseUrl = "https://report-maker-re9lay.onrender.com";

    public enum UploadStatus { Idle, Uploading, Success, Error }
    [Header("Live Status")]
    public UploadStatus currentStatus = UploadStatus.Idle;
    public string statusMessage = "";
    public string lastGeneratedPdfPath = "";

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

    /// <summary>
    /// Warm up the free-tier Render container early so it's awake before session ends.
    /// </summary>
    public void WarmUp()
    {
        StartCoroutine(WarmUpServer());
    }

    private IEnumerator WarmUpServer()
    {
        string healthUrl = $"{apiBaseUrl.TrimEnd('/')}/health";
        Debug.Log($"[ReportUploader] Pinging warm-up endpoint: {healthUrl}");

        using (UnityWebRequest request = UnityWebRequest.Get(healthUrl))
        {
            request.timeout = 60; // Allow 60s for Render free tier cold-start
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log($"[ReportUploader] Render Server is awake & ready: {request.downloadHandler.text}");
            }
            else
            {
                Debug.LogWarning($"[ReportUploader] Server warm-up ping note: {request.error}");
            }
        }
    }

    /// <summary>
    /// Generates report for the current session.
    /// </summary>
    public void GenerateReport()
    {
        if (currentStatus == UploadStatus.Uploading) return;

        string csvPath = "";
        string sessionLabel = DateTime.Now.ToString("yyyy_MM_dd_HH_mm");

        if (SessionLogger.Instance != null)
        {
            SessionLogger.Instance.StopLoggingSession(); // Ensure file is flushed to disk
            csvPath = SessionLogger.Instance.CurrentFilePath;
            sessionLabel = SessionLogger.Instance.CurrentSessionLabel;
        }

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            // Fallback: search candidate directories for newest session CSV
            csvPath = FindNewestSessionCsv();
            if (!string.IsNullOrEmpty(csvPath))
            {
                sessionLabel = Path.GetFileNameWithoutExtension(csvPath);
                Debug.Log($"[ReportUploader] Used newest found session CSV: {csvPath}");
            }
        }

        if (string.IsNullOrEmpty(csvPath) || !File.Exists(csvPath))
        {
            currentStatus = UploadStatus.Error;
            statusMessage = "Session CSV not found on disk.";
            Debug.LogError($"[ReportUploader] Cannot upload: CSV file missing at '{csvPath}'");
            return;
        }

        Debug.Log($"[ReportUploader] Starting report generation for: '{csvPath}' (Label: {sessionLabel})");
        StartCoroutine(GenerateAndSaveReport(csvPath, sessionLabel));
    }

    private string FindNewestSessionCsv()
    {
        var candidateDirs = new System.Collections.Generic.List<string>();
#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
        try {
            string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
            candidateDirs.Add(Path.Combine(userProfile, "Downloads", "game_csv"));
        } catch {}
#elif UNITY_ANDROID && !UNITY_EDITOR
        candidateDirs.Add("/storage/emulated/0/Documents/Re9layLogs");
#endif
        candidateDirs.Add(Path.Combine(Application.persistentDataPath, "Re9layLogs"));
        candidateDirs.Add(Application.persistentDataPath);

        string newestFile = null;
        DateTime newestTime = DateTime.MinValue;

        foreach (string dir in candidateDirs)
        {
            if (Directory.Exists(dir))
            {
                string[] files = Directory.GetFiles(dir, "*.csv");
                foreach (string f in files)
                {
                    DateTime t = File.GetLastWriteTime(f);
                    if (t > newestTime)
                    {
                        newestTime = t;
                        newestFile = f;
                    }
                }
            }
        }
        return newestFile;
    }

    public IEnumerator GenerateAndSaveReport(string csvFilePath, string sessionLabel)
    {
        currentStatus = UploadStatus.Uploading;
        statusMessage = "Uploading CSV & generating PDF...";

        byte[] csvBytes = null;
        try
        {
            csvBytes = File.ReadAllBytes(csvFilePath);
        }
        catch (Exception ex)
        {
            currentStatus = UploadStatus.Error;
            statusMessage = "Read error: " + ex.Message;
            yield break;
        }

        string csvFileName = Path.GetFileName(csvFilePath);
        string uploadUrl = $"{apiBaseUrl.TrimEnd('/')}/generate-report";

        WWWForm form = new WWWForm();
        form.AddBinaryData("csv_file", csvBytes, csvFileName, "text/csv");
        form.AddField("session_label", sessionLabel);

        int emgThresh = (GameSettings.Instance != null) ? GameSettings.Instance.emgThreshold : 400;
        form.AddField("emg_threshold", emgThresh.ToString());

        using (UnityWebRequest request = UnityWebRequest.Post(uploadUrl, form))
        {
            // Allow 90 seconds in case Render is spinning up from cold-start
            request.timeout = 90;
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                currentStatus = UploadStatus.Error;
                statusMessage = $"Server error: {request.error}";
                Debug.LogError($"[ReportUploader] Report generation failed: {request.error} | Response: {request.downloadHandler?.text}");
                yield break;
            }

            byte[] pdfBytes = request.downloadHandler.data;
            if (pdfBytes == null || pdfBytes.Length < 100)
            {
                currentStatus = UploadStatus.Error;
                statusMessage = "Received invalid PDF payload from server.";
                yield break;
            }

            string outPath = Path.Combine(Application.persistentDataPath, $"Re9lay_Report_{sessionLabel}.pdf");
            try
            {
                File.WriteAllBytes(outPath, pdfBytes);

#if UNITY_STANDALONE_WIN || UNITY_EDITOR_WIN
                try
                {
                    string userProfile = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
                    string pcDownloads = Path.Combine(userProfile, "Downloads", "game_csv");
                    if (Directory.Exists(pcDownloads))
                    {
                        string pcPdfPath = Path.Combine(pcDownloads, $"Re9lay_Report_{sessionLabel}.pdf");
                        File.WriteAllBytes(pcPdfPath, pdfBytes);
                        outPath = pcPdfPath; // Set active path to Downloads folder
                    }
                }
                catch { }
#elif UNITY_ANDROID && !UNITY_EDITOR
                try
                {
                    string publicDocs = "/storage/emulated/0/Documents/Re9layLogs";
                    if (Directory.Exists(publicDocs))
                    {
                        string publicPdf = Path.Combine(publicDocs, $"Re9lay_Report_{sessionLabel}.pdf");
                        File.WriteAllBytes(publicPdf, pdfBytes);
                        outPath = publicPdf;
                    }
                }
                catch { }
#endif

                lastGeneratedPdfPath = outPath;
                currentStatus = UploadStatus.Success;
                statusMessage = "Report generated and saved!";
                Debug.Log($"[ReportUploader] Successfully saved PDF report ({pdfBytes.Length} bytes) to: {outPath}");

                OpenPdf(outPath);
            }
            catch (Exception ex)
            {
                currentStatus = UploadStatus.Error;
                statusMessage = "Save error: " + ex.Message;
                Debug.LogError($"[ReportUploader] Failed to write PDF: {ex.Message}");
            }
        }
    }

    public void OpenLastReport()
    {
        if (!string.IsNullOrEmpty(lastGeneratedPdfPath) && File.Exists(lastGeneratedPdfPath))
        {
            OpenPdf(lastGeneratedPdfPath);
        }
    }

    private void OpenPdf(string path)
    {
        try
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            Application.OpenURL("file://" + path);
#else
            Application.OpenURL("file:///" + path.Replace("\\", "/"));
#endif
            Debug.Log($"[ReportUploader] Opened PDF viewer for: {path}");
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[ReportUploader] Could not automatically open PDF: {ex.Message}");
        }
    }
}
