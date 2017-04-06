using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CubeController : MonoBehaviour {

	public bool transparent = false;
	public Material cubeMaterial;
	public Material grayTransparentMaterial;

	// Use this for initialization
	void Start() {
		
	}
	
	// Update is called once per frame
	void Update() {
		if(transparent) {
			GetComponent<Renderer>().material = grayTransparentMaterial;
		} else {
			GetComponent<Renderer>().material = cubeMaterial;
		}
	}
}
