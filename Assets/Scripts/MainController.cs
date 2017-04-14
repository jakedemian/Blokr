using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class MainController : MonoBehaviour {
	
	/***************************
	* PUBLIC MEMBERS
	***************************/
	// Prefab for board square
	public GameObject boardSquarePrefab;

	// prefab for placed cube
	public GameObject cubePrefab;

	// prefab for cube explosion particle
	public GameObject cubeParticlePrefab;

	// list of block break sounds
	public List<AudioClip> blockSmashSounds;

	// sounds for bomb block explosion
	public AudioClip bombSmashSound;

	// sound for an invalid move / not enough moves left
	public AudioClip invalidMoveSound;

	// move count text
	public Text guiMoveCount;

	// text displayed when you lose
	public Text loseText;

	// text displayed when you win
	public Text winText;

	// the reset button in the upper-right of the screen
	public Button resetButton;

	// group of UI objects invoked when player loses
	public GameObject loseUIGroup;

	// group of UI objects invoked when player wins
	public GameObject winUIGroup;

	/***************************
	* CONSTANTS
	***************************/
	// true if user is on a mobile device, false if on desktop
	private bool isOnMobileDevice;

	// the number of particles that are spawned when destroying a block
	private const float CUBE_PARTICLE_COUNT = 20;

	// the scaling factor for font size, based on screen dpi (set in Start() method)
	private float FONT_SCALING_FACTOR;

	// the scaling factor for button size, based on screen dpi (set in Start() method)
	private float BUTTON_SCALING_FACTOR;

	// the fall speed for blocks when there are no blocks below them
	private const float CUBE_FALL_SPEED = -10f;

	// bit shifted integer for targeting only Cube objects when using RayCasts
	private int RAYCAST_LAYER = 1 << 8;

	/***************************
	* PRIVATE MEMBERS
	***************************/
	// a 2d list of all grid square GameObjects
	private List<List<GameObject>> grid;

	// a list of all cube objects currently alive
	private List<GameObject> cubes = new List<GameObject>();

	// a reference to the cube currently being targeted by the player
	private GameObject targetCube = null;

	// debouncing boolean for touch / mouse input
	private bool inputDown = false;

	// used to make sure we never play the same explosion sound twice in a row
	private int lastCubeSmashSoundIdx = -1;

	// the index of the current level
	private int currentLevelIdx = 0;

	// the position of the user's input device on the previous frame
	private Vector2 lastTouchInputPosition = new Vector2(0f, 0f);

	// a data object containing all data for the current level
	private Level level;

	// the number of moves the user has left
	private int moves;

	// ensures that there are no debouncing problems when the user resets the level
	private bool touchLockedAfterReset = false;

	/**
	 * START
	 */
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;

		// init UI values
		FONT_SCALING_FACTOR = Screen.dpi / 4.5f;
		BUTTON_SCALING_FACTOR = Screen.dpi / 48f;

		// TODO move this into init, doesn't really belong here BUT you need to delete
		// all of the grid squares on reset if you move it there
		createGameBoard();

		// TODO in another function like onWake() or something i need to pull some info out
		// the device's storage, including the level they're currently on
		initLevel(currentLevelIdx);

		initUiElements();
	}

	/**
	 * UPDATE
	 */
	void Update() {
		handleCubeTargeting();

		// clean up the cubes list, there might be some null values in there somewhere
		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] == null) {
				cubes.RemoveAt(i);
				i--;
			}
		}
	}

	/**
	 * ON GUI
	 */
	void OnGUI() {
		guiMoveCount.text = moves.ToString();

		int cubesLeft = 0;
		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null) {
				cubesLeft++;
			}
		}

		if(moves <= 0) {
			if(cubesLeft > 0) {
				// you lose :(
				loseUIGroup.SetActive(true);
			} else {
				// you win!
				winUIGroup.SetActive(true);
			}
		} else if(cubesLeft == 0) {
			winUIGroup.SetActive(true);
		}

		// keep the move counter in the correct position
		guiMoveCount.transform.position = new Vector2(
			ScreenHelper.getScreenDimensions().x * 0.22f,
			ScreenHelper.getScreenDimensions().y * 0.95f
		);
	}

	/**
	 * Initialize the games UI positioning, size, etc
	 */
	void initUiElements() {
		// init the font size of all text
		loseText.fontSize = (int)FONT_SCALING_FACTOR;
		winText.fontSize = (int)FONT_SCALING_FACTOR;
		guiMoveCount.fontSize = (int)FONT_SCALING_FACTOR;

		// init items that are centered on the screen
		initCenteredUIElement(loseText.gameObject, 0);
		initCenteredUIElement(winText.gameObject, 0);
		initCenteredUIElement(loseUIGroup.transform.FindChild("RetryButton").gameObject, 3 * loseText.fontSize);
		initCenteredUIElement(winUIGroup.transform.FindChild("NextButton").gameObject, 3 * winText.fontSize);

		// init the scale of all buttons
		initButtonScale(resetButton.gameObject);
		initButtonScale(loseUIGroup.transform.FindChild("RetryButton").gameObject);
		initButtonScale(winUIGroup.transform.FindChild("NextButton").gameObject);
	}

	/**
	 * Initialize the UI elements that are centered on the screen.
	 * 
	 * @param go 				The GameObject that is to be centered
	 * @param verticalOffset 	The vertical offset to apply to the UI element
	 */
	void initCenteredUIElement(GameObject go, int verticalOffset) {
		go.transform.position = new Vector2(
			go.transform.position.x,
			ScreenHelper.getScreenMidpoint().y + (ScreenHelper.getScreenDimensions().y / 5) - verticalOffset
		);
	}

	/**
	 * Initialize the UI elements that are centered on the screen.
	 * 
	 * @param go The button (of GameObject type) to be scaled.
	 */
	void initButtonScale(GameObject go) {
		// TODO FIXME these should either be constants or this class or 
		//     a helper should have getters for these
		go.transform.localScale = new Vector2(BUTTON_SCALING_FACTOR, BUTTON_SCALING_FACTOR);
	}


	/**
	 * This method handles cube targeting and beginning a cube destruction chain.
	 */
	void handleCubeTargeting() {
		if(allBlocksAreStopped() && moves > 0) {
			// if theyre currently holding down the input
			if(isInputDown() && !touchLockedAfterReset) {
				Ray ray = Camera.main.ScreenPointToRay(getInputPosition());
				RaycastHit hit;

				if(!inputDown) {
					inputDown = true;
					if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
						targetCube = hit.collider.gameObject;
						targetCube.GetComponent<CubeController>().setIsTargeted(true);
					}
				} else if(targetCube != null) {
					if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
						GameObject obj = hit.collider.gameObject;
						targetCube.GetComponent<CubeController>().setIsTargeted(obj.Equals(targetCube));
					} else { 
						targetCube.GetComponent<CubeController>().setIsTargeted(false);
					}
				}
			}
			// theyve released the input
			else if(inputDown && !touchLockedAfterReset) {
				// we get in here ONCE after user releases finger
				inputDown = false;

				Ray ray = Camera.main.ScreenPointToRay(getInputPosition());
				RaycastHit hit;

				if(Physics.Raycast(ray, out hit, Mathf.Infinity, RAYCAST_LAYER)) {
					if(hit.collider.gameObject.Equals(targetCube)) {
						if(decrementMoveCount(targetCube.GetComponent<CubeController>().getType())) {
							destroyCubeWithCascade(targetCube);
						} else {
							AudioSource.PlayClipAtPoint(invalidMoveSound, targetCube.transform.position);
							targetCube.GetComponent<CubeController>().setIsTargeted(false);
							targetCube = null;
						}
					}
				}
			} else if(touchLockedAfterReset) {
				touchLockedAfterReset = false;
			}
		}
	}

	/**
	 * Decrement the move counter depending on the type of cube being destroyed.
	 * 
	 * @param cubeType The target cube's type
	 */
	bool decrementMoveCount(string cubeType) {
		bool success = false;
		int currentMoves = this.moves;

		if(cubeType.Equals("Cube")) {
			currentMoves--;
		} else if(cubeType.Equals("Bomb")) {
			currentMoves -= 3;
		} else {
			Debug.LogError("Invalid cube type was passed to decrementMoveCount()");
			return false;
		}

		if(currentMoves >= 0) {
			success = true;
			this.moves = currentMoves;
		}

		return success;
	}

	/**
	 * Destroy a cube and all other cubes it destroys, depending on the type of cube.
	 * 
	 * @param cube The cube we are going to destroy.
	 */
	void destroyCubeWithCascade(GameObject cube) {
		string cubeType = cube.GetComponent<CubeController>().getType();
		Vector3 cubePosition = targetCube.transform.position;

		if(cubeType.Equals("Cube")) {
			destroyCubeType(cube, cubePosition);
		} else if(cubeType.Equals("Bomb")) {
			destroyBombType(cube, cubePosition);
		}
	}

	/**
	 * Destroy a cube object, without affecting any other objects.
	 * 
	 * @param cube The cube to be destroyed.
	 */
	private void destroySingleCube(GameObject cube) {
		Vector3 cubePos = cube.transform.position;
		triggerAboveCubesToFall(cubePos);

		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null && cubes[i].Equals(cube)) {
				Destroy(cubes[i]);

				// set to null, will be removed at the end of Update()
				cubes[i] = null;
			}
		}
	}

	/**
	 * Set all cubes above the given position to the fall velocity.
	 * 
	 * @param cubePos The position of the cube that was destroyed.
	 */
	void triggerAboveCubesToFall(Vector3 cubePos) {
		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null) {
				Vector3 p = cubes[i].transform.position;

				if(p.y > cubePos.y && p.x == cubePos.x && p.z == cubePos.z) {
					Rigidbody rb = cubes[i].GetComponent<Rigidbody>();
					if(rb.velocity.y == 0f) {
						rb.velocity = new Vector3(rb.velocity.x, CUBE_FALL_SPEED, rb.velocity.z);
					}
				}
			}
		}
	}

	/**
	 * Destroy a plain cube.
	 * 
	 * @param cube 		The plain cube to destroy.
	 * @param cubePos 	The position of the plain cube.
	 */
	void destroyCubeType(GameObject cube, Vector3 cubePos) {
		// get cube's current position so we have it after it's destroyed
		destroySingleCube(cube);
		generateCubeParticles(cubePos);
		generateBlockDestroySound(cubePos);
	}

	/**
	 * Destroy a bomb cube, as well as all cubes in a 1 unit radius.
	 * 
	 * @param cube 		The bomb cube to destroy.
	 * @param cubePos 	The position of the bomb cube.
	 */
	void destroyBombType(GameObject cube, Vector3 cubePos) {
		destroySingleCube(cube);

		Camera.main.GetComponent<CameraMovement>().shakeCamera();

		// TODO FIXME this should be different
		generateCubeParticles(cubePos, cube.GetComponent<CubeController>().bombMaterial);
		AudioSource.PlayClipAtPoint(bombSmashSound, cubePos);

		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null) {
				GameObject thisCube = cubes[i];
				Vector3 thisPos = thisCube.transform.position;

				int maxDistance = 1;
				if((int)Mathf.Sqrt(
					   Mathf.Pow(cubePos.x - thisPos.x, 2) +
					   Mathf.Pow(cubePos.y - thisPos.y, 2) +
					   Mathf.Pow(cubePos.z - thisPos.z, 2)
				   ) <= maxDistance) {
					destroyCubeWithCascade(thisCube);
				}
			}
		}

	}

	/**
	 * Used to determine if all blocks in the game have zero velocity. (i.e. are not falling)
	 * 
	 * @return True if no blocks are moving, false if at least 1 block is still moving.
	 */
	bool allBlocksAreStopped() {
		// TODO I need to POSSIBLY also ensure that no particles exist
		bool allBlocksAreStopped = true;
		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null && cubes[i].GetComponent<Rigidbody>().velocity.y != 0) {
				allBlocksAreStopped = false;
				break;
			}
		}
		return allBlocksAreStopped;
	}

	/**
	 * Plays a random block destroy sound from the available list of possible sounds.
	 * 
	 * @param sourcePos The position which the sound should come from.
	 */
	void generateBlockDestroySound(Vector3 sourcePos) {
		int soundIdx = Random.Range(0, blockSmashSounds.Count - 1);

		if(soundIdx == lastCubeSmashSoundIdx) {
			soundIdx++;
			if(soundIdx >= blockSmashSounds.Count) {
				soundIdx = 0;
			}
		}

		AudioSource.PlayClipAtPoint(blockSmashSounds[soundIdx], sourcePos);
		lastCubeSmashSoundIdx = soundIdx;
	}

	/**
	 * Generate particle explosion effect when a block is destroyed.
	 * 
	 * @param pos 				The position of the destroyed block.
	 * @param particleMaterial 	The material to set the particles to.  Depends on 
	 * 								the block that was destroyed.
	 */
	void generateCubeParticles(Vector3 pos, Material particleMaterial = null) {
		for(int i = 0; i < CUBE_PARTICLE_COUNT; i++) {
			GameObject particle = Instantiate(cubeParticlePrefab, pos, Quaternion.identity);
			if(particleMaterial != null) {
				particle.GetComponent<Renderer>().material = particleMaterial;
			}
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
			}
		}
	}

	/**
	 * Initialize the level.
	 * 
	 * @param levelIdx The index of the level we wish to load.
	 */
	void initLevel(int levelIdx) {
		string levelJsonName = "level_" + levelIdx;

		TextAsset asset = Resources.Load(Path.Combine("Data", levelJsonName)) as TextAsset;
		if(asset != null) {
			this.level = JsonUtility.FromJson<Level>(asset.text);
			this.moves = level.moves;


			loseUIGroup.SetActive(false);
			winUIGroup.SetActive(false);

			for(int i = 0; i < level.cubes.Length; i++) {
				Cube c = level.cubes[i];
				GameObject newCube = Instantiate(cubePrefab, new Vector3(c.x, c.y, c.z), Quaternion.identity);

				//set the cube type
				newCube.GetComponent<CubeController>().setType(c.type);

				// add the cube to our stored list of cube GameObjects
				cubes.Add(newCube);
			}
		} else {
			Debug.LogError("There was a problem loading the level with levelIdx=" + levelIdx);
		}
	}

	/**
	 * Reset the current level, clearing the game of all objects 
	 * 		and recreating the current level from the start.
	 */
	public void resetLevel() {
		if(currentLevelIdx != -1) {
			for(int i = 0; i < cubes.Count; i++) {
				Destroy(cubes[i]);
			}
			cubes = new List<GameObject>();

			lastCubeSmashSoundIdx = -1;
			inputDown = false;
			targetCube = null;
			touchLockedAfterReset = true;

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
			if(Input.touchCount >= 1) {
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
			isInputDown = Input.touchCount >= 1;
		} else {
			isInputDown = Input.GetMouseButton(0);
		}

		return isInputDown;
	}

	/**
	 * Go to the provided level.
	 * 
	 * @param lvlIdx The index of the level we will go to.
	 */
	public void goToLevel(int lvlIdx) {
		this.currentLevelIdx = lvlIdx;
		resetLevel();
	}

	/**
	 * Go to the next level.
	 */
	public void goToNextLevel() {
		// FIXME this needs to be smarter and not blindly go to the next level without thinking
		this.currentLevelIdx++;
		resetLevel();
	}


}
