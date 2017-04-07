using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeParticle : MonoBehaviour {

	private float lifeTimer = 1f;

	void Start() {
		float speed = Random.Range(4f, 10f);
		GetComponent<Rigidbody>().velocity = Random.onUnitSphere * speed;
		GetComponent<Rigidbody>().angularVelocity = Random.onUnitSphere * speed;
	}
	
	// Update is called once per frame
	void Update() {
		lifeTimer -= Time.deltaTime;

		if(lifeTimer <= 0f) {
			Destroy(gameObject);
		}
	}
}
