using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour {

	public bool transparent = false;

	// type materials
	public Material cubeMaterial;
	public Material bombMaterial;
	public Material targetedMaterial;

	private Material currentMaterial;

	private bool markedForDestroy = false;

	public AudioClip targetSound;
	public AudioClip untargetSound;

	private bool targeted = false;
	private string type = "Cube";

	private bool targetDebouncer = false;

	// Use this for initialization
	void Start() {
	}
	
	// Update is called once per frame
	void Update() {
		Rigidbody rb = GetComponent<Rigidbody>();
		if(rb.velocity.y >= 0f) {
			rb.velocity = new Vector3(rb.velocity.x, 0f, rb.velocity.z);

			float roundedPositionY = ((float)((int)transform.position.y)) + 0.5f;
			transform.position = new Vector3(transform.position.x, roundedPositionY, transform.position.z);
		}


		if(targeted && !targetDebouncer) {
			targetDebouncer = true;
			GetComponent<Renderer>().material = targetedMaterial;
			AudioSource.PlayClipAtPoint(targetSound, transform.position);
		} else if(!targeted && targetDebouncer) {
			targetDebouncer = false;
			GetComponent<Renderer>().material = this.currentMaterial;
			AudioSource.PlayClipAtPoint(untargetSound, transform.position);
		}
	}

	public void setType(string type) {
		this.type = type;

		if(type.Equals("Cube")) {
			this.currentMaterial = cubeMaterial;
			GetComponent<Renderer>().material = this.currentMaterial;
		} else if(type.Equals("Bomb")) {
			this.currentMaterial = bombMaterial;
			GetComponent<Renderer>().material = this.currentMaterial;
		}
	}

	public string getType() {
		return this.type;
	}

	public bool isTargeted() {
		return this.targeted;
	}

	public void setIsTargeted(bool isTargeted) {
		this.targeted = isTargeted;
	}

	public bool isMarkedForDestroy() {
		return markedForDestroy;
	}

	public void setMarkedForDestroy(bool mfd) {
		this.markedForDestroy = mfd;
	}
}
