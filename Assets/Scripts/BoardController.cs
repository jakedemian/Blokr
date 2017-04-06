using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class BoardController : MonoBehaviour {
	/////////////////////////////////////////////////
	// PUBLIC

	// Prefab for board square
	public GameObject boardSquarePrefab;

	// prefab for template cube
	public GameObject templateCubePrefab;

	// prefab for placed cube
	public GameObject cubePrefab;

	// material of the template cube when the user hasn't held it in place long enough to place it
	public Material notConfirmedTemplateMaterial;

	// material of the template cube when the user HAS held it in place long enough to place it
	public Material confirmTemplateMaterial;


	/////////////////////////////////////////////////
	// CONSTANTS

	// the layer that houses board squares.  used to prevent Raycast from hitting other objects
	private const int BOARD_SQUARE_LAYER = 8;

	// time delay to place a block
	private const float BLOCK_PLACEMENT_TIME_DELAY = 0.5f;

	/////////////////////////////////////////////////
	// PRIVATE MEMBERS

	// true if user is on a mobile device, false if on desktop
	private bool isOnMobileDevice;

	// the template cube object
	private GameObject templateCube;

	// true if releasing cube-placement input will place a cube, false otherwise
	private bool placementReady = false;

	// the current grid square the user is attempting to place a block at
	private GameObject currentBoardSquare;

	// timer to help users pervent accidental block placement
	private float placementTimer;

	// a 2d list of all grid square GameObjects
	private List<List<GameObject>> grid;

	// true when the first block has been placed
	private bool firstBlockPlaced = false;

	public Material lockedGridSquare;
	public Material unlockedGridSquare;

	/**
	 * START
	 */
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;

		createGameBoard();
	}

	/**
	 * UPDATE
	 */
	void Update() {
		// if user is holding down the place-block input
		if(isPlaceBlockInputEventActive()) {
			Ray ray = Camera.main.ScreenPointToRay(getInputPosition());
			RaycastHit hit;

			int layer = 1 << BOARD_SQUARE_LAYER; // bit shift for some reason..

			// if our raycast hits a board square that is unlocked (or it's the first turn)
			if(Physics.Raycast(ray, out hit, Mathf.Infinity, layer)
			   && (!hit.collider.gameObject.GetComponent<BoardSquareController>().locked || !firstBlockPlaced)) {
				currentBoardSquare = hit.collider.gameObject;
				int currentCubesInBoardSquare = currentBoardSquare.GetComponent<BoardSquareController>().cubesInThisSpace;

				// if there is no template cube, create one
				if(templateCube == null) { 
					placementTimer = BLOCK_PLACEMENT_TIME_DELAY;
					templateCube = Instantiate(templateCubePrefab,
						new Vector3(hit.collider.transform.position.x,
							(float)currentCubesInBoardSquare + 0.5f,
							hit.collider.transform.position.z),
						Quaternion.identity);
				
				} 
				// otherwise, if there is a template cube but the raycast is hitting a different square
				else if(templateCube != null && (hit.collider.transform.position.x != templateCube.transform.position.x
				        || hit.collider.transform.position.z != templateCube.transform.position.z)) {
					// move the template cube and restart the confirm timer/texture
					placementTimer = BLOCK_PLACEMENT_TIME_DELAY;
					templateCube.GetComponent<Renderer>().material = notConfirmedTemplateMaterial;
					placementReady = false;
					templateCube.transform.position = new Vector3(hit.collider.transform.position.x,
						(float)currentCubesInBoardSquare + 0.5f,
						hit.collider.transform.position.z);
				}
			} else {
				removeTemplateCube();
			}

			updatePlacementTimer();
			
		} else if(templateCube != null && currentBoardSquare != null) {
			if(placementReady) {
				// place a cube at the template's location
				Instantiate(cubePrefab, templateCube.transform.position, Quaternion.identity);
				firstBlockPlaced = true;

				// update the board square's cube count
				currentBoardSquare.GetComponent<BoardSquareController>().cubesInThisSpace++;

				int visionDistance = currentBoardSquare.GetComponent<BoardSquareController>().cubesInThisSpace;
				int thisSquareX = currentBoardSquare.GetComponent<BoardSquareController>().gridX;
				int thisSquareY = currentBoardSquare.GetComponent<BoardSquareController>().gridY;

				for(int i = 0; i < grid.Count; i++) {
					for(int j = 0; j < grid[i].Count; j++) {
						int gridX = grid[i][j].GetComponent<BoardSquareController>().gridX;
						int gridY = grid[i][j].GetComponent<BoardSquareController>().gridY;

						float distBetweenGridSquares = Mathf.Sqrt(
							                               Mathf.Pow((gridX - thisSquareX), 2) +
							                               Mathf.Pow((gridY - thisSquareY), 2)
						                               );

						if(distBetweenGridSquares <= visionDistance) {
							grid[i][j].GetComponent<BoardSquareController>().locked = false;
						}
					}
				}
			}
			removeTemplateCube();
		}
	}

	/**
	 * Update the cube placement timer.
	 */
	void updatePlacementTimer() {
		if(!placementReady) {
			if(placementTimer > 0f) {
				placementTimer -= Time.deltaTime;
			} else {
				if(templateCube != null) {
					templateCube.GetComponent<Renderer>().material = confirmTemplateMaterial;
					placementTimer = 0f;
					placementReady = true;
				}
			}
		}
	}

	/**
	 * Destroy the template cube object and reset related values.
	 */
	void removeTemplateCube() {
		placementReady = false;
		currentBoardSquare = null;
		Destroy(templateCube);
	}

	/**
	 * Create the game board square by square, storing the grid in our 2D grid List.
	 */
	void createGameBoard() {
		grid = new List<List<GameObject>>();
		for(int i = 0; i < 16; i++) {
			grid.Add(new List<GameObject>());
			for(int j = 0; j < 16; j++) {
				GameObject newSquare = Instantiate(boardSquarePrefab, 			
					                       new Vector3((float)i, 0.0f, (float)j), 
					                       Quaternion.identity);
				newSquare.GetComponent<BoardSquareController>().gridX = i;
				newSquare.GetComponent<BoardSquareController>().gridY = j;
				grid[i].Add(newSquare);
			}
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
			res = Input.GetTouch(0).position;
		} else {
			res = Input.mousePosition;
		}

		int offset = (int)(Screen.dpi / 3.0f);
		res = new Vector3(res.x, res.y + offset, res.z);

		return res;
	}

	/**
	 * Determine if the user is holding down the place-block input.
	 * 
	 * @return True if the user is holding down the correct place-block input, false otherwise.
	 */
	bool isPlaceBlockInputEventActive() {
		bool isPlaceBlockInputEventActive = false;

		if(isOnMobileDevice) {
			isPlaceBlockInputEventActive = Input.touchCount == 1;
		} else {
			isPlaceBlockInputEventActive = Input.GetMouseButton(0) && !Input.GetMouseButton(1);
		}

		return isPlaceBlockInputEventActive;
	}


}
