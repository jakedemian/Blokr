using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour {

	public bool transparent = false;
	public Material cubeMaterial;
	public Material targetedMaterial;

	public AudioClip targetSound;
	public AudioClip untargetSound;

	public bool targeted = false;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		Rigidbody rb = GetComponent<Rigidbody>();
		if(rb.velocity.y > 0f) {
			rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);
		}

		if(targeted && GetComponent<Renderer>().sharedMaterial.Equals(cubeMaterial)) {
			GetComponent<Renderer>().material = targetedMaterial;
			AudioSource.PlayClipAtPoint(targetSound, transform.position);
		} else if(!targeted && GetComponent<Renderer>().sharedMaterial.Equals(targetedMaterial)) {
			GetComponent<Renderer>().material = cubeMaterial;
			AudioSource.PlayClipAtPoint(untargetSound, transform.position);
		}
	}
}
