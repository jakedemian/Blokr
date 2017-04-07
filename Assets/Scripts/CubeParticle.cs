using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeParticle : MonoBehaviour {

	private const float lifeTimerStart = 1.5f;
	private float lifeTimer = lifeTimerStart;

	private float startingScale;

	void Start() {
		float speed = Random.Range(5f, 20f);

		Vector3 randomUnitOnSphere = Random.onUnitSphere;

		transform.position = transform.position + randomUnitOnSphere;
		GetComponent<Rigidbody>().velocity = randomUnitOnSphere * speed;
		GetComponent<Rigidbody>().angularVelocity = randomUnitOnSphere * speed;

		startingScale = transform.localScale.x; // same scale for x, y, and z, so just use x
	}
	
	// Update is called once per frame
	void Update() {
		if(GetComponent<Renderer>().isVisible) {
			lifeTimer -= Time.deltaTime;

			float newScale = startingScale * (lifeTimer / lifeTimerStart);
			transform.localScale = new Vector3(newScale, newScale, newScale);

			if(lifeTimer <= 0f) {
				Destroy(gameObject);
			}
		} else {
			Destroy(gameObject);
		}
	}
}
