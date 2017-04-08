﻿using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class MainController : MonoBehaviour {
	/////////////////////////////////////////////////
	// PUBLIC

	// Prefab for board square
	public GameObject boardSquarePrefab;

	// prefab for placed cube
	public GameObject cubePrefab;

	/////////////////////////////////////////////////
	// CONSTANTS
	private const float CUBE_PARTICLE_COUNT = 35;
	/////////////////////////////////////////////////
	// PRIVATE MEMBERS

	// true if user is on a mobile device, false if on desktop
	private bool isOnMobileDevice;

	// a 2d list of all grid square GameObjects
	private List<List<GameObject>> grid;

	private int RAYCAST_LAYER = 1 << 8;

	private bool inputDown = false;

	public GameObject cubeParticlePrefab;

	public List<AudioClip> blockSmashSounds;
	private int lastSmashSoundIdx = -1;
	private int currentLevelIdx = -1;

	private List<GameObject> objectInstances = new List<GameObject>();

	private GameObject targetCube = null;

	private Vector2 lastTouchInputPosition = new Vector2(0f, 0f);

	/**
	 * START
	 */
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;

		// TODO FIXME this should probably be done in initLevel()
		createGameBoard();

		// TODO FIXME make level loading dynamic
		initLevel(0);
	}

	/**
	 * UPDATE
	 */
	void Update() {
		handleCubeTargeting();
	}

	void handleCubeTargeting() {
		// if theyre currently holding down the input
		if(isInputDown()) {
			Ray ray = Camera.main.ScreenPointToRay(getInputPosition());
			RaycastHit hit;

			if(!inputDown) {
				inputDown = true;
				if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
					targetCube = hit.collider.gameObject;
					targetCube.GetComponent<CubeController>().targeted = true;
				}
			} else if(targetCube != null) {
				if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
					GameObject obj = hit.collider.gameObject;
					targetCube.GetComponent<CubeController>().targeted = obj.Equals(targetCube);
				} else { 
					targetCube.GetComponent<CubeController>().targeted = false;
				}
			}
		}
		// theyve released the input
		else if(inputDown) {
			// we get in here ONCE after user releases finger
			inputDown = false;

			Ray ray = Camera.main.ScreenPointToRay(getInputPosition());
			RaycastHit hit;

			if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
				if(hit.collider.gameObject.Equals(targetCube)) {

					// get cube's current position so we have it after it's destroyed
					Vector3 cubePosition = targetCube.transform.position;

					Destroy(targetCube);
					generateCubeParticles(cubePosition);
					generateBlockDestroySound(cubePosition);
				}

			}
		}
	}

	void generateBlockDestroySound(Vector3 sourcePos) {
		int soundIdx = Random.Range(0, blockSmashSounds.Count - 1);

		if(soundIdx == lastSmashSoundIdx) {
			soundIdx++;
			if(soundIdx >= blockSmashSounds.Count) {
				soundIdx = 0;
			}
		}

		AudioSource.PlayClipAtPoint(blockSmashSounds[soundIdx], sourcePos);
		lastSmashSoundIdx = soundIdx;
	}

	void generateCubeParticles(Vector3 pos) {
		for(int i = 0; i < CUBE_PARTICLE_COUNT; i++) {
			objectInstances.Add(Instantiate(cubeParticlePrefab, pos, Quaternion.identity));
		}
	}

	/**
	 * Create the game board square by square, storing the grid in our 2D grid List.
	 */
	void createGameBoard() {
		grid = new List<List<GameObject>>();
		for(int i = 0; i < 5; i++) {
			grid.Add(new List<GameObject>());
			for(int j = 0; j < 5; j++) {
				GameObject newSquare = Instantiate(boardSquarePrefab, 			
					                       new Vector3((float)i, 0.0f, (float)j), 
					                       Quaternion.identity);
				newSquare.GetComponent<BoardSquareController>().gridX = i;
				newSquare.GetComponent<BoardSquareController>().gridY = j;
				grid[i].Add(newSquare);

				// FIXME setting the camera focal point should be done in the camera movement script, not here. the camera obj should store 
				// the vertical offset from this object, rather than directly use this objects current position
				// set the position of the controller
				transform.position = new Vector3(2f, 3f, 2f);
			}
		}
	}

	void initLevel(int levelIdx) {
		for(int i = 0; i < 5; i++) {
			objectInstances.Add(Instantiate(cubePrefab, new Vector3(2f, (float)i + 0.5f, 2f), Quaternion.identity));
		}
		currentLevelIdx = levelIdx;
	}

	public void resetLevel() {
		if(currentLevelIdx != -1) {
			for(int i = 0; i < objectInstances.Count; i++) {
				Destroy(objectInstances[i]);
			}
			objectInstances = new List<GameObject>();

			lastSmashSoundIdx = -1;
			inputDown = false;
			targetCube = null;

			initLevel(currentLevelIdx);
		}

	}

	/**
	 * Get the position of the user's input.
	 * 
	 * @return A vector of the input position.
	 */
	Vector3 getInputPosition() {
		Vector3 res;

		if(isOnMobileDevice) {
			if(Input.touchCount == 1) {
				res = Input.GetTouch(0).position;
				lastTouchInputPosition = Input.GetTouch(0).position;
			}
			res = lastTouchInputPosition;
		} else {
			res = Input.mousePosition;
		}

		return res;
	}

	/**
	 * Determine if the user is holding down the place-block input.
	 * 
	 * @return True if the user is holding down the correct place-block input, false otherwise.
	 */
	bool isInputDown() {
		bool isInputDown = false;

		if(isOnMobileDevice) {
			isInputDown = Input.touchCount == 1;
		} else {
			isInputDown = Input.GetMouseButton(0);
		}

		return isInputDown;
	}


}