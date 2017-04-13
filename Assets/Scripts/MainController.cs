using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class MainController : MonoBehaviour {
	/////////////////////////////////////////////////
	// PUBLIC

	// Prefab for board square
	public GameObject boardSquarePrefab;

	// prefab for placed cube
	public GameObject cubePrefab;

	/////////////////////////////////////////////////
	// CONSTANTS
	private const float CUBE_PARTICLE_COUNT = 20;
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
	private int lastCubeSmashSoundIdx = -1;

	public AudioClip bombSmashSound;

	private int currentLevelIdx = 0;

	private List<GameObject> cubes = new List<GameObject>();

	private GameObject targetCube = null;

	private Vector2 lastTouchInputPosition = new Vector2(0f, 0f);

	private float blockFallSpeed = -10f;

	private Level level;
	private int moves;

	public Text guiMoveLabelText;
	public Text guiMoveCount;
	public Text loseText;
	public Text winText;
	public Button resetButton;
	public Button nextLevelButton;

	public GameObject loseUIGroup;
	public GameObject winUIGroup;

	private GameObject loseRetryButton;
	private GameObject winNextLevelButton;

	private bool touchLockedAfterReset = false;


	/**
	 * START
	 */
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;

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

		Vector2 screenDimensions = new Vector2(Screen.width, Screen.height);
		float dpi = Screen.dpi;

		Vector2 midPoint = new Vector2(screenDimensions.x / 2, screenDimensions.y / 2);
		float fontScalingFactor = dpi / 4.5f;
		guiMoveCount.transform.position = new Vector2(screenDimensions.x * 0.22f, screenDimensions.y * 0.95f);
	}

	void initUiElements() {
		Vector2 screenDimensions = new Vector2(Screen.width, Screen.height);
		float dpi = Screen.dpi;

		Vector2 midPoint = new Vector2(screenDimensions.x / 2, screenDimensions.y / 2);
		float fontScalingFactor = dpi / 4.5f;

		// main text displays
		loseText.fontSize = (int)fontScalingFactor;
		winText.fontSize = (int)fontScalingFactor;

		guiMoveCount.fontSize = (int)fontScalingFactor;

		Debug.Log(guiMoveCount.GetComponent<RectTransform>().anchoredPosition.y);

		Vector2 mainTxtPos = new Vector2(midPoint.x, midPoint.y + (screenDimensions.y / 5));
		loseText.transform.position = mainTxtPos;
		winText.transform.position = mainTxtPos;

		// Buttons
		loseRetryButton = loseUIGroup.transform.FindChild("RetryButton").gameObject;
		loseRetryButton.transform.position = new Vector2(
			loseRetryButton.transform.position.x,
			midPoint.y + (screenDimensions.y / 5) - (3 * loseText.fontSize));

		winNextLevelButton = winUIGroup.transform.FindChild("NextButton").gameObject;
		winNextLevelButton.transform.position = new Vector2(
			winNextLevelButton.transform.position.x,
			midPoint.y + (screenDimensions.y / 5) - (3 * winText.fontSize));

		// button scale factor
		float buttonScaleFactor = dpi / 48f;

		resetButton.transform.localScale = new Vector2(buttonScaleFactor, buttonScaleFactor);
		loseRetryButton.transform.localScale = new Vector2(buttonScaleFactor, buttonScaleFactor);
		winNextLevelButton.transform.localScale = new Vector2(buttonScaleFactor, buttonScaleFactor);
	}

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
						this.moves--;
						destroyCubeWithCascade(targetCube);
					}
				}
			} else if(touchLockedAfterReset) {
				touchLockedAfterReset = false;
			}
		}
	}

	void destroyCubeWithCascade(GameObject cube) {
		string cubeType = cube.GetComponent<CubeController>().getType();
		Vector3 cubePosition = targetCube.transform.position;

		if(cubeType.Equals("Cube")) {
			destroyCubeType(cube, cubePosition);
		} else if(cubeType.Equals("Bomb")) {
			destroyBombType(cube, cubePosition);
		}
	}

	private void destroySingleCube(GameObject cube) {
		// set all cubes above this cube to fall
		Vector3 cubePos = cube.transform.position;
		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null) {
				Vector3 p = cubes[i].transform.position;

				if(p.y > cubePos.y && p.x == cubePos.x && p.z == cubePos.z) {
					Rigidbody rb = cubes[i].GetComponent<Rigidbody>();
					if(rb.velocity.y == 0f) {
						rb.velocity = new Vector3(rb.velocity.x, blockFallSpeed, rb.velocity.z);
					}
				}
			}
		}

		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null && cubes[i].Equals(cube)) {
				Destroy(cubes[i]);

				// set to null, will be removed at the end of Update()
				cubes[i] = null;
			}
		}
	}

	void destroyCubeType(GameObject cube, Vector3 cubePosition) {
		// get cube's current position so we have it after it's destroyed
		destroySingleCube(cube);
		generateCubeParticles(cubePosition);
		generateBlockDestroySound(cubePosition);
	}

	void destroyBombType(GameObject cube, Vector3 cubePosition) {
		destroySingleCube(cube);

		Camera.main.GetComponent<CameraMovement>().shakeCamera();

		// TODO FIXME this should be different
		generateCubeParticles(cubePosition, cube.GetComponent<CubeController>().bombMaterial);
		AudioSource.PlayClipAtPoint(bombSmashSound, cubePosition);

		for(int i = 0; i < cubes.Count; i++) {
			if(cubes[i] != null) {
				GameObject thisCube = cubes[i];
				Vector3 thisPos = thisCube.transform.position;

				int maxDistance = 1;
				if((int)Mathf.Sqrt(
					   Mathf.Pow(cubePosition.x - thisPos.x, 2) +
					   Mathf.Pow(cubePosition.y - thisPos.y, 2) +
					   Mathf.Pow(cubePosition.z - thisPos.z, 2)
				   ) <= maxDistance) {
					destroyCubeWithCascade(thisCube);
				}
			}
		}

	}

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

	void initLevel(int levelIdx) {
		string levelJsonName = "level_" + levelIdx;
		TextAsset asset = Resources.Load(Path.Combine("Data", levelJsonName)) as TextAsset;
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
	}

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

	public void goToLevel(int lvlIdx) {
		this.currentLevelIdx = lvlIdx;
		resetLevel();
	}

	public void goToNextLevel() {
		// FIXME this needs to be smarter and not blindly go to the next level without thinking
		this.currentLevelIdx++;
		resetLevel();
	}


}
