using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour {

	public bool transparent = false;
	public Material cubeMaterial;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		Rigidbody rb = GetComponent<Rigidbody>();
		if(rb.velocity.y > 0f) {
			rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		}
	}
}
