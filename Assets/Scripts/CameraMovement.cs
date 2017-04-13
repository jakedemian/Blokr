﻿using System.Collections;
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

	private float shakeTimer = 0f;
	private Vector3 shakeStartingPos = Vector3.zero;
	private const float SHAKE_STRENGTH = 0.2f;
	private const float MAX_SHAKE_OFFSET = 0.1f;
	private const float SHAKE_DURATION = 0.7f;

	public GameObject moveSpinningCube;

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
		updateMoveCountCubePosition();
		moveSpinningCube.transform.Rotate(new Vector3(35f * Time.deltaTime, 28f * Time.deltaTime, 40f * Time.deltaTime));

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

	void FixedUpdate() {
		if(shakeTimer != 0f) {
			if(shakeStartingPos == Vector3.zero) {
				shakeStartingPos = transform.position;
			}

			float percentComplete = 1 - (shakeTimer / SHAKE_DURATION);         
			float damper = 1.0f - percentComplete;

			float proposedX = transform.position.x + (Random.Range(-SHAKE_STRENGTH, SHAKE_STRENGTH) * damper);
			float proposedY = transform.position.y + (Random.Range(-SHAKE_STRENGTH, SHAKE_STRENGTH) * damper);
			float proposedZ = transform.position.z + (Random.Range(-SHAKE_STRENGTH, SHAKE_STRENGTH) * damper);

			if(Mathf.Abs(Mathf.Abs(shakeStartingPos.x) - Mathf.Abs(proposedX)) > MAX_SHAKE_OFFSET) {
				proposedX = transform.position.x;
			}
			if(Mathf.Abs(Mathf.Abs(shakeStartingPos.y) - Mathf.Abs(proposedY)) > MAX_SHAKE_OFFSET) {
				proposedY = transform.position.y;
			}
			if(Mathf.Abs(Mathf.Abs(shakeStartingPos.z) - Mathf.Abs(proposedZ)) > MAX_SHAKE_OFFSET) {
				proposedZ = transform.position.z;
			}


			transform.position = new Vector3(
				proposedX,
				proposedY,
				proposedZ
			);
	
			shakeTimer -= Time.deltaTime;

			if(shakeTimer <= 0f) {
				shakeTimer = 0f;
				transform.position = shakeStartingPos;
				shakeStartingPos = Vector3.zero;
			}
		}
	}

	/**
	 * LATE UPDATE
	 */
	void LateUpdate() {
		Vector3 focalPoint = new Vector3(target.position.x, 
			                     target.position.y + verticalPanOffset, 
			                     target.position.z);

		// don't want to be focusing on anything while the camera is shaking
		if(shakeTimer == 0f) {
			transform.LookAt(focalPoint);
		}
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

		updateMoveCountCubePosition();
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
		
		updateMoveCountCubePosition();
	}


	public void shakeCamera() {
		shakeTimer = SHAKE_DURATION;
	}

	private void updateMoveCountCubePosition() {
		Vector3 worldPoint = Camera.main.ViewportToWorldPoint(new Vector3(0.1f, 0.95f, 25f));
		moveSpinningCube.transform.position = worldPoint;
	}

}
