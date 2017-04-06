﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardSquareController : MonoBehaviour {

	public int cubesInThisSpace = 0;
	public int gridX;
	public int gridY;
	public bool locked = true;

	public Material lockedSquare;
	public Material unlockedSquare;

	void Update() {
		if(locked) {
			GetComponent<Renderer>().material = lockedSquare;
		} else {
			GetComponent<Renderer>().material = unlockedSquare;
		}
	}
}
