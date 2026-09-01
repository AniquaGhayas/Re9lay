/*
2D Space Shooter - Vertical Bullet Controller (Self-Destruct on Walls/Offscreen)
*/

using UnityEngine;

public class Bullet : MonoBehaviour {

	public int bulletSpeed = 30;
	public int bulletDirection = 1; // +1 moves UPWARD

	void FixedUpdate () {
		GetComponent<Rigidbody2D>().AddForce(new Vector2(0, bulletDirection * 0.1f * bulletSpeed), ForceMode2D.Impulse);

		// Self-destruct if bullet flies beyond top or bottom bounds
		float maxY = 5.0f;
		if (DynamicScreenBoundaries.Instance != null) {
			maxY = DynamicScreenBoundaries.Instance.maxWorldY + 0.5f;
		}

		if (Mathf.Abs(transform.position.y) > maxY) {
			Destroy(gameObject);
		}
	}

	void OnTriggerEnter2D(Collider2D other) {
		CheckWallCollision(other.gameObject);
	}

	void OnCollisionEnter2D(Collision2D collision) {
		CheckWallCollision(collision.gameObject);
	}

	void CheckWallCollision(GameObject hitObj) {
		if (hitObj == null) return;
		string n = hitObj.name.ToLower();

		// Destroy bullet immediately when hitting boundaries (Ceiling, Floor, LeftWall, RightWall)
		if (n.Contains("ceiling") || n.Contains("floor") || n.Contains("wall")) {
			Destroy(gameObject);
		}
	}

	void OnBecameInvisible() {  
		Destroy(gameObject);
	}
}