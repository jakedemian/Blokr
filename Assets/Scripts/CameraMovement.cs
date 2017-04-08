using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

	public Transform target;

	private const float ORBIT_SPEED = 0.1f;
	private const float VERTICAL_PAN_SPEED = 0.01f;
	private const float MAX_Y_CAMERA_OFFSET = 3f;

	private bool isOnMobileDevice;

	private Vector3 lastMousePos = new Vector3(0f, 0f, 0f);
	private float verticalPanOffset = 0f;
	private float cameraStartingYPos;
	private float maxCameraYPos;

	/**
	 * START
	 **/
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;

		cameraStartingYPos = transform.position.y;
		maxCameraYPos = cameraStartingYPos + MAX_Y_CAMERA_OFFSET;
	}

	/**
	 * UPDATE
	 **/
	void Update() {
		if(isInputDown()) {
			Vector2 delta = getInputPos();
			cameraOrbit(delta.x);
			panCameraVertically(delta.y);
		}

		// stupid bit of code i have to do because they dont have a 
		// function to handle this for me on desktop
		if(!isOnMobileDevice) {
			lastMousePos = Input.mousePosition;
		}

	}

	/**
	 * LATE UPDATE
	 */
	void LateUpdate() {
		Vector3 focalPoint = new Vector3(target.position.x, 
			                     target.position.y + verticalPanOffset, 
			                     target.position.z);
		
		transform.LookAt(focalPoint);
	}

	/**
	 * True if the device's camera activating input is held down, false otherwise
	 */
	bool isInputDown() {
		bool res = false;
		if(isOnMobileDevice) {
			res = Input.touchCount == 1;
		} else {
			res = Input.GetMouseButton(1);
		}

		return res;
	}

	/**
	 * Get the location of the devices input
	 */
	Vector2 getInputPos() {
		Vector2 res;
		if(isOnMobileDevice) {
			res = Input.GetTouch(0).deltaPosition;
		} else {
			Vector3 delta3 = (Input.mousePosition - lastMousePos);
			Vector2 delta2 = new Vector2(delta3.x, delta3.y);
			res = delta2;
		}

		return res;
	}

	/***********************************************************
	 * CAMERA ACTIONS
	 ***********************************************************/

	/**
	* Orbit the camera around the game board.
	* 
	* @param xDelta The input direction depicting the orbit direction.
	*/
	void cameraOrbit(float angle) {
		transform.RotateAround(target.position, new Vector3(0f, 1f, 0f), angle * ORBIT_SPEED);
	}


	/**
	* Pan the camera vertically, constraining it to upper and lower bounds.
	* 
	* @param amount The amount to pan the camera up/down.
	*/
	void panCameraVertically(float amount) {
		verticalPanOffset -= amount * VERTICAL_PAN_SPEED;

		// constrain the offset to its upper and lower limits
		if(verticalPanOffset < 0f) {
			verticalPanOffset = 0f;
		} else if(cameraStartingYPos + verticalPanOffset > maxCameraYPos) {
			verticalPanOffset = maxCameraYPos - cameraStartingYPos;
		}

		transform.position = new Vector3(
			transform.position.x,
			cameraStartingYPos + verticalPanOffset,
			transform.position.z);
	}




}
