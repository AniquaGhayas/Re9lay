/*
2D Space Shooter - Dynamic Device Enemy Generator
Spawns enemies across the top boundary of any target screen device.
*/

using UnityEngine;

public class enemyGenerator : MonoBehaviour {

	public GameObject alien;

	public float timeBetweenEnemies = 7.0f; // 1 enemy every 7 seconds
	private int secondsBeforeFirstEnemyAppears = 1;
	private float timeLastAlien;

	void FixedUpdate () {
		if (Time.realtimeSinceStartup > secondsBeforeFirstEnemyAppears && Time.time >= timeLastAlien) {
			if (alien != null) {
				GameObject enemyObj = GameObject.Instantiate(alien);

				float spawnMinX = -1.4f, spawnMaxX = 1.4f, spawnY = 3.5f;
				if (DynamicScreenBoundaries.Instance != null) {
					spawnMinX = DynamicScreenBoundaries.Instance.minWorldX + 0.4f;
					spawnMaxX = DynamicScreenBoundaries.Instance.maxWorldX - 0.4f;
					spawnY = DynamicScreenBoundaries.Instance.maxWorldY - 0.5f;
				}

				enemyObj.transform.position = new Vector3(Random.Range(spawnMinX, spawnMaxX), spawnY, 0f);
				enemyObj.transform.eulerAngles = Vector3.zero;
			}
			float interval = (DifficultyManager.Instance != null) ? DifficultyManager.Instance.CurrentSpawnInterval : timeBetweenEnemies;
			timeLastAlien = Time.time + interval;
		}
	}
}