using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class Level {
	public int id;
	public int numOfLayers;
	public int moves;
	public Cube[] cubes;
}

[System.Serializable]
public class Cube {
	public float x;
	public float y;
	public float z;
	public string type;
}