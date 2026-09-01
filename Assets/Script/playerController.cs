/*
2D Space Shooter - Player Controller (Zero Gravity WASD Movement)
*/

using UnityEngine;

public class playerController : MonoBehaviour {

	public GameObject playerBullet;
	public bool playerIsImmortal = false;
	public int playerLives = 1;
	public bool isGameOver = false;

	[Header("Movement Settings")]
	public float moveSpeed = 5.0f;
	public float playerPadding = 0.4f;

	[Header("Shooting Settings")]
	private float playerBulletXOffset = 0f;
	private float playerBulletYOffset = 0.4f;
	private float timeBetweenShots = 0.2f;
	private float timestamp;

	private Rigidbody2D rb;

	void Start() {
		rb = GetComponent<Rigidbody2D>();
		if (rb != null) {
			rb.gravityScale = 0f; // Zero gravity so player can move smoothly in Y-axis
			rb.freezeRotation = true; // Lock rotation physics
		}
	}

	void Update () {
		if (isGameOver) {
			if (Input.GetButtonDown("Fire1") || Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space)) {
				Time.timeScale = 1.0F;
				if (GameManager.Instance != null) {
					GameManager.Instance.StartNewSession();
				}
				UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().buildIndex);
			}
			return;
		}

		HandleWASDMovement();
		HandleSpacebarShooting();
	}

	void HandleWASDMovement () {
		float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
		float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down

		Vector3 moveDir = new Vector3(h, v, 0f).normalized;
		Vector3 newPos = transform.position + moveDir * moveSpeed * Time.deltaTime;

		float minX = -1.8f, maxX = 1.8f, minY = -3.6f, maxY = 3.6f;
		if (DynamicScreenBoundaries.Instance != null) {
			minX = DynamicScreenBoundaries.Instance.minWorldX + playerPadding;
			maxX = DynamicScreenBoundaries.Instance.maxWorldX - playerPadding;
			minY = DynamicScreenBoundaries.Instance.minWorldY + playerPadding;
			maxY = DynamicScreenBoundaries.Instance.maxWorldY - playerPadding;
		}

		newPos.x = Mathf.Clamp(newPos.x, minX, maxX);
		newPos.y = Mathf.Clamp(newPos.y, minY, maxY);

		transform.position = newPos;

		// Ensure velocity is reset so gravity physics doesn't interfere
		if (rb != null) {
			rb.velocity = Vector2.zero;
		}
	}

	void HandleSpacebarShooting () {
		bool isSpacePressed = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");

		if (BluetoothInputManager.Instance != null) {
			BluetoothInputManager.Instance.shoot = isSpacePressed ? 1 : 0;
			BluetoothInputManager.Instance.isContracted = isSpacePressed;
		}

		if (isSpacePressed && Time.time >= timestamp) {
			Instantiate(playerBullet, transform.position + new Vector3(playerBulletXOffset, playerBulletYOffset, 0), Quaternion.Euler(0, 0, 90f));
			timestamp = Time.time + timeBetweenShots;
		}
	}

	void OnTriggerEnter2D(Collider2D thisObject) {
		CheckEnemyCollision(thisObject.gameObject);
	}

	void OnCollisionEnter2D(Collision2D thisObject) {
		CheckEnemyCollision(thisObject.gameObject);
	}

	void CheckEnemyCollision(GameObject other) {
		if (other == null) return;
		string objName = other.name.ToLower();

		if (objName.Contains("enemy") || objName.Contains("alien")) {
			playerDidCollide();
		}
	}

	void playerDidCollide () {
		if (!playerIsImmortal) {
			playerLives = 0;
			gameOver();
		}
	}

	void gameOver () {
		isGameOver = true;
		Time.timeScale = 0.0F;

		if (GameManager.Instance != null) {
			GameManager.Instance.OnPlayerGameOver();
		}
	}
}