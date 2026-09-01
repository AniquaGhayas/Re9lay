/*
2D Space Shooter - Vertical Alien Controller (No Shooting, Constant Speed)
*/

using UnityEngine;
using System.Collections;

public class alienController : MonoBehaviour {

	public float constantEnemySpeed = 1.5f;
	private Rigidbody2D rb;

	void Start () {
		rb = GetComponent<Rigidbody2D>();
		GetComponent<Collider2D>().enabled = true;
	}

	void FixedUpdate () {
		float speedMult = 1.0f;
		if (DifficultyManager.Instance != null) {
			speedMult = DifficultyManager.Instance.CurrentSpeedMultiplier;
		}

		// Constant downward movement velocity
		if (rb != null) {
			rb.velocity = new Vector2(0f, -constantEnemySpeed * speedMult);
		}

		// Destroy alien if it moves past bottom screen boundary
		float destroyY = -4.8f;
		if (DynamicScreenBoundaries.Instance != null) {
			destroyY = DynamicScreenBoundaries.Instance.minWorldY - 1.0f;
		}

		if (transform.position.y < destroyY) {
			Destroy(gameObject);
		}
	}

	void OnBecameInvisible() {  
		Destroy(gameObject);
	}

	void OnTriggerEnter2D(Collider2D thisObject) {
		HandleHit(thisObject.gameObject);
	}

	void OnCollisionEnter2D(Collision2D thisObject) {
		HandleHit(thisObject.gameObject);
	}

	void HandleHit(GameObject hitObj) {
		if (hitObj == null) return;
		string hitName = hitObj.name.ToLower();

		// Ignore boundary wall collisions
		if (hitName.Contains("ceiling") || hitName.Contains("floor") || hitName.Contains("wall")) {
			return;
		}

		// Award points and destroy alien when hit by player bullet or ship
		if (hitName.Contains("player_bullet") || hitName.Contains("bullet") || hitName.Contains("player")) {
			GUI guiScript = Camera.main.GetComponent<GUI>();
			if (guiScript != null) {
				guiScript.currentScore++;

				if (DifficultyManager.Instance != null) {
					DifficultyManager.Instance.UpdateScore(guiScript.currentScore);
				}
			}

			if (hitName.Contains("bullet")) {
				Destroy(hitObj);
			}

			StartCoroutine(blinkUponCollisionAndDestroy(gameObject));
		}
	}

	IEnumerator blinkUponCollisionAndDestroy(GameObject thisPlayer) {
		thisPlayer.GetComponent<Renderer>().material.SetColor("_Color", Color.cyan);
		yield return new WaitForSeconds(0.1f);
		Destroy(gameObject);
	}
}