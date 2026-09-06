# Re9lay - Cloud Report Generation: Deployment & Unity Integration

## Context
The Python report pipeline (`metrics.py`, `charts.py`, `report.py`) is
already built and wrapped in a FastAPI server (`server.py`). This doc
covers two things:
1. Deploying `server.py` to Render (free tier) so it has a public URL.
2. Wiring the Unity app to upload the session CSV to that URL and save
   the returned PDF on the phone.

## Part 1: Deploy the API to Render

### Files needed in the deploy repo
```
server.py
metrics.py
charts.py
report.py
requirements.txt
```
All already exist except this needs to be a proper Git repo pushed to
GitHub (Render deploys from a Git repo, not a local zip).

### Steps
1. Create a new GitHub repo (e.g. `re9lay-report-api`) and push the
   5 files above to it.
2. Go to [render.com](https://render.com), sign up (no credit card
   needed for the free tier), click **New > Web Service**.
3. Connect the GitHub repo.
4. Configure:
   - **Runtime**: Python 3
   - **Build Command**: `pip install -r requirements.txt`
   - **Start Command**: `uvicorn server:app --host 0.0.0.0 --port $PORT`
   - **Instance Type**: Free
5. Deploy. Render gives you a public URL like
   `https://re9lay-report-api.onrender.com`.
6. Verify it's live: open `https://<your-url>/health` in a browser —
   should return `{"status": "ok"}`.

### Important: free tier cold starts
Free Render services spin down after ~15 min of inactivity. The first
request after idle takes 30-50s to wake up. For demos: hit `/health`
a few seconds before you need to generate a report to "warm" it, or
just build a small loading state into the Unity UI for the upload call
(see Part 2) so it doesn't look frozen.

### API contract
`POST /generate-report` (multipart/form-data):
| Field            | Type   | Required | Notes                                  |
|-------------------|--------|----------|-----------------------------------------|
| csv_file          | file   | yes      | the session CSV                         |
| session_label     | text   | no       | defaults to the CSV filename            |
| emg_threshold     | number | no       | defaults to 400.0                       |
| logo              | file   | no       | logo image for the report header        |

Response: the generated PDF file (binary, `application/pdf`).

`GET /health` -> `{"status": "ok"}` for the warm-up ping.

## Part 2: Unity integration

### Flow
1. Session ends, Unity has already written the session CSV locally
   (existing behavior - keep this, it's still useful as a local backup).
2. Unity POSTs that CSV to `https://<your-render-url>/generate-report`.
3. Server responds with PDF bytes.
4. Unity writes those bytes to `Application.persistentDataPath` on the
   phone.
5. Unity opens/shares the PDF using a native viewer intent.

### C# - uploading the CSV and saving the PDF

```csharp
using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class ReportUploader : MonoBehaviour
{
    private const string ApiBaseUrl = "https://<your-render-url>"; // no trailing slash

    public IEnumerator GenerateAndSaveReport(string csvFilePath, string sessionLabel)
    {
        byte[] csvBytes = File.ReadAllBytes(csvFilePath);
        string csvFileName = Path.GetFileName(csvFilePath);

        WWWForm form = new WWWForm();
        form.AddBinaryData("csv_file", csvBytes, csvFileName, "text/csv");
        form.AddField("session_label", sessionLabel);
        // form.AddField("emg_threshold", "400"); // optional override

        using (UnityWebRequest request = UnityWebRequest.Post(
            $"{ApiBaseUrl}/generate-report", form))
        {
            request.timeout = 60; // allow for cold start
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Report generation failed: {request.error}");
                yield break;
            }

            byte[] pdfBytes = request.downloadHandler.data;
            string outPath = Path.Combine(
                Application.persistentDataPath,
                $"report_{sessionLabel}.pdf");
            File.WriteAllBytes(outPath, pdfBytes);

            Debug.Log($"Report saved to {outPath}");
            OpenPdf(outPath);
        }
    }

    private void OpenPdf(string path)
    {
        // Android: open with the system's default PDF viewer via intent
        Application.OpenURL("file://" + path);
    }
}
```

### Optional: warm-up ping before upload
To avoid a cold-start delay right when the player wants their report,
fire a `/health` GET as soon as the session starts (not when it ends),
so the server is likely awake by the time the CSV is ready to upload.

```csharp
public IEnumerator WarmUpServer()
{
    using (UnityWebRequest request = UnityWebRequest.Get($"{ApiBaseUrl}/health"))
    {
        yield return request.SendWebRequest();
    }
}
```
Call `StartCoroutine(WarmUpServer())` when the session begins, and
`StartCoroutine(GenerateAndSaveReport(csvPath, label))` when it ends.

### Android permissions
No special storage permission is needed for
`Application.persistentDataPath` (it's app-scoped storage). If you
want to let the user share/export the PDF outside the app (e.g. to
Drive or WhatsApp), that needs Android's native share sheet - a
separate native plugin call, not covered here.

## Non-goals for this pass
- No authentication on the API yet (fine for a hackathon demo; add an
  API key header before any public/production use).
- No retry/offline queue if the upload fails - Unity should just show
  an error and let the player retry manually for now.
