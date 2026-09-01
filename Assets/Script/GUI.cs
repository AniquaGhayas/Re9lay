/*
2D Space Shooter - Responsive Matrix GUI (Perfect Scaling on Any Mobile Phone / Device)
*/

using UnityEngine;
using UnityEngine.UI;

public class GUI : MonoBehaviour {

	public static GUI Instance { get; private set; }

	public int currentScore = 0;
	public bool isGameOver;

	private Text livesText;
	private Text scoreText;
	private Text gameOverText;
	private Text instructionsText;

	// Virtual Reference Resolution for Matrix Scaling (Portrait Mode)
	private readonly float virtualWidth = 400f;
	private readonly float virtualHeight = 700f;

	void Awake() {
		if (Instance != null && Instance != this) {
			Destroy(this);
			return;
		}
		Instance = this;
	}

	void Start () {
		// Disable old Canvas UI text elements
		GameObject livesObj = GameObject.Find("Lives");
		if (livesObj != null) {
			livesText = livesObj.GetComponent<Text>();
			if (livesText != null) livesText.enabled = false;
		}

		GameObject scoreObj = GameObject.Find("Score");
		if (scoreObj != null) {
			scoreText = scoreObj.GetComponent<Text>();
			if (scoreText != null) scoreText.enabled = false;
		}

		GameObject gameOverObj = GameObject.Find("GameOver");
		if (gameOverObj != null) {
			gameOverText = gameOverObj.GetComponent<Text>();
		}

		GameObject instructObj = GameObject.Find("Instructions");
		if (instructObj != null) {
			instructionsText = instructObj.GetComponent<Text>();
			if (instructionsText != null) instructionsText.enabled = false;
		}
	}

	void Update () {
		GameObject player = GameObject.Find("Player");
		if (player != null) {
			playerController pc = player.GetComponent<playerController>();
			if (pc != null) {
				isGameOver = pc.isGameOver;
			}
		}

		if (gameOverText != null) gameOverText.enabled = isGameOver;
	}

	void OnGUI() {
		if (Instance != this) return;

		// Apply GUI Matrix Scaling so UI scales 100% perfectly on any Phone, Tablet or Monitor
		Matrix4x4 origMatrix = UnityEngine.GUI.matrix;
		Vector3 scale = new Vector3(Screen.width / virtualWidth, Screen.height / virtualHeight, 1.0f);
		UnityEngine.GUI.matrix = Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale);

		float totalSeconds = 0f;
		if (GameManager.Instance != null) {
			totalSeconds = GameManager.Instance.sessionTimer;
		} else {
			totalSeconds = Time.timeSinceLevelLoad;
		}

		int minutes = Mathf.FloorToInt(totalSeconds / 60f);
		int seconds = Mathf.FloorToInt(totalSeconds % 60f);
		string timeFormatted = string.Format("{0:00}:{1:00}", minutes, seconds);

		// 1. Single Top-Center Score & Time Display
		GUIStyle headerStyle = new GUIStyle(UnityEngine.GUI.skin.label);
		headerStyle.fontSize = 18;
		headerStyle.fontStyle = FontStyle.Bold;
		headerStyle.alignment = TextAnchor.MiddleCenter;
		headerStyle.normal.textColor = Color.yellow;

		string headerText = $"SCORE: {currentScore}    |    TIME: {timeFormatted}";
		UnityEngine.GUI.Label(new Rect(0f, 10f, virtualWidth, 30f), headerText, headerStyle);

		// 2. Compact Telemetry HUD Box (Positioned Top-Left below score header)
		GUILayout.BeginArea(new Rect(10f, 45f, 175f, 75f), UnityEngine.GUI.skin.box);
		
		GUIStyle titleStyle = new GUIStyle(UnityEngine.GUI.skin.label);
		titleStyle.fontSize = 11;
		titleStyle.fontStyle = FontStyle.Bold;
		titleStyle.normal.textColor = Color.white;
		GUILayout.Label("NEUROPLAY 2.0", titleStyle);

		float speed = 1.0f;
		if (DifficultyManager.Instance != null) {
			speed = DifficultyManager.Instance.CurrentSpeedMultiplier;
		}

		GUIStyle infoStyle = new GUIStyle(UnityEngine.GUI.skin.label);
		infoStyle.fontSize = 10;
		infoStyle.normal.textColor = Color.white;

		GUILayout.Label($"Speed: {speed:F2}x", infoStyle);

		bool isShooting = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");
		string shootStatus = isShooting ? "<color=green>SHOOTING</color>" : "<color=yellow>READY</color>";
		GUILayout.Label($"Weapon: {shootStatus}", infoStyle);
		GUILayout.Label("Controls: WASD", infoStyle);
		GUILayout.EndArea();

		// 3. Pause Menu Overlay
		if (GameManager.Instance != null && GameManager.Instance.isPaused) {
			UnityEngine.GUI.Box(new Rect(virtualWidth / 2f - 100f, virtualHeight / 2f - 50f, 200f, 100f), "PAUSED");
			if (UnityEngine.GUI.Button(new Rect(virtualWidth / 2f - 80f, virtualHeight / 2f, 160f, 30f), "Resume Session")) {
				GameManager.Instance.TogglePause();
			}
		}

		// Restore original GUI matrix
		UnityEngine.GUI.matrix = origMatrix;
	}
}