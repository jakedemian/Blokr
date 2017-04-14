using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour {

	/***************************
	* PUBLIC MEMBERS
	***************************/
	// The material for a plain cube
	public Material cubeMaterial;

	// The material for a bomb cube
	public Material bombMaterial;

	// The material for a cube that is targeted
	public Material targetedMaterial;

	// The sound played when a cube is targeted
	public AudioClip targetSound;

	// The sound played when a cube is untargeted
	public AudioClip untargetSound;

	/***************************
	* PRIVATE MEMBERS
	***************************/
	// The material currently applied to this cube
	private Material currentMaterial;

	// True if this cube is targeted, false otherwise
	private bool targeted = false;

	// Denotes this cube's type (defaults to 'Cube')
	private string type = "Cube";

	// Debouncing boolean used for targeting
	private bool targetDebouncer = false;



	/**
	 * UPDATE
	 */
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

	/**
	 * Set the type of this cube.
	 * 
	 * @param type The type we will set this cube to.
	 */
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

	/**
	 * Get this cube's type.
	 */
	public string getType() {
		return this.type;
	}

	/**
	 * True if this cube is currently targeted, false otherwise.
	 */
	public bool isTargeted() {
		return this.targeted;
	}

	/**
	 * Set this cubes targeted property.
	 */
	public void setIsTargeted(bool isTargeted) {
		this.targeted = isTargeted;
	}
}
