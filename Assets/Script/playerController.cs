/*
2D Space Shooter - Dual Input Player Controller (HC-05 Bluetooth Glove + WASD/Spacebar)
*/

using UnityEngine;

public class playerController : MonoBehaviour {

	public GameObject playerBullet;
	public bool playerIsImmortal = false;
	public int playerLives = 1;
	public bool isGameOver = false;

	[Header("Movement Settings")]
	public float moveSpeed = 2.0f;
	public float playerPadding = 0.4f;

	[Header("Shooting Settings")]
	public float playerBulletXOffset = 0f;
	public float playerBulletYOffset = 0.65f;
	public float timeBetweenShots = 0.2f;
	private float timestamp;

	private Rigidbody2D rb;

	void Start() {
		rb = GetComponent<Rigidbody2D>();
		if (rb != null) {
			rb.gravityScale = 0f;
			rb.freezeRotation = true;
		}
	}

	void Update () {
		if (isGameOver) {
			return;
		}

		HandleMovement();
		HandleShooting();
	}

	void HandleMovement () {
		Vector3 moveDir = Vector3.zero;

		// 1. Direct Keyboard WASD and Arrow Keys (for testing on laptop)
		float keyX = 0f;
		float keyY = 0f;
		if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) keyY += 1f;
		if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) keyY -= 1f;
		if (Input.GetKey(KeyCode.A) || Input.GetKey(KeyCode.LeftArrow)) keyX -= 1f;
		if (Input.GetKey(KeyCode.D) || Input.GetKey(KeyCode.RightArrow)) keyX += 1f;

		if (keyX != 0f || keyY != 0f) {
			moveDir = new Vector3(keyX, keyY, 0f).normalized;
		}
		// 2. Read HC-05 Bluetooth IMU Tilt direction if Bluetooth connected and no keyboard input
		else if (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.isConnected) {
			Vector2 btDir = BluetoothInputManager.Instance.GetMoveDirection();
			moveDir = new Vector3(btDir.x, btDir.y, 0f);
		}
		// 3. Fallback to Unity Input axes
		else {
			float h = Input.GetAxisRaw("Horizontal"); // A/D or Left/Right
			float v = Input.GetAxisRaw("Vertical");   // W/S or Up/Down
			moveDir = new Vector3(h, v, 0f).normalized;
		}

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

		if (rb != null) {
			rb.velocity = Vector2.zero;
		}
	}

	private bool isTriggerArmed = true;

	void HandleShooting () {
		// Evaluates shooting from Spacebar key, Fire1 button, or HC-05 EMG contraction
		bool isSpaceHeld = Input.GetKey(KeyCode.Space) || Input.GetButton("Fire1");
		bool isEMGContracted = (BluetoothInputManager.Instance != null && BluetoothInputManager.Instance.shoot == 1);
		bool isActivating = isSpaceHeld || isEMGContracted;

		if (isActivating) {
			if (isTriggerArmed) {
				// Fire exactly ONE bullet per contraction
				if (playerBullet != null) {
					Vector3 spawnPos = transform.position + new Vector3(playerBulletXOffset, playerBulletYOffset, 0f);
					GameObject bulletObj = Instantiate(playerBullet, spawnPos, Quaternion.identity);

					// Prevent bullet from colliding with player ship
					Collider2D shipCol = GetComponent<Collider2D>();
					Collider2D bulletCol = bulletObj != null ? bulletObj.GetComponent<Collider2D>() : null;
					if (shipCol != null && bulletCol != null) {
						Physics2D.IgnoreCollision(shipCol, bulletCol);
					}
				}
				// Disarm trigger: must relax muscle before firing again
				isTriggerArmed = false;
			}
		} else {
			// Muscle relaxed below threshold / key released -> rearm trigger
			isTriggerArmed = true;
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