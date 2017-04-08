using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraMovement : MonoBehaviour {

	public Transform target;

	private const float REQUIRED_ROTATION_ANGLE_OFFSET = 0.1f;
	private const float REQUIRED_ROTATION_FINGER_SEPARATION = 500f;
	private const float REQUIRED_PINCH_SPEED = 0.1f;

	private bool isOnMobileDevice;

	private const float CAM_FOV_MIN = 25f;
	private const float CAM_FOV_MAX = 25f;

	private const float ZOOM_SPEED = 0.1f;
	private const float ORBIT_SPEED = 4.0f;

	private Vector2 previousVector;
	private bool twoFingerEvent = false;

	/**
	 * START
	 **/
	void Start() {
		isOnMobileDevice = Application.platform == RuntimePlatform.Android
		|| Application.platform == RuntimePlatform.IPhonePlayer;
	}

	void LateUpdate() {
		transform.LookAt(target);
	}

	/**
	 * UPDATE
	 **/
	void Update() {
		if(isOnMobileDevice) {
			if(Input.touchCount == 2 && !twoFingerEvent) {
				twoFingerEvent = true;

				// set our initial vector
				previousVector = Input.GetTouch(1).position - Input.GetTouch(0).position;
			} else if(Input.touchCount == 2 && twoFingerEvent) {
				checkForInputEvents();
				previousVector = Input.GetTouch(1).position - Input.GetTouch(0).position;
			} else {
				twoFingerEvent = false;
			}
		}
	}

	/**
	 * Get the input direction on a mobile device
	 */
	void checkForInputEvents() {
		
		// rotate
		Vector2 currVector = Input.GetTouch(1).position - Input.GetTouch(0).position;
		float angleOffset = Vector2.Angle(previousVector, currVector);
		Vector3 LR = Vector3.Cross(previousVector, currVector);

		if(angleOffset > REQUIRED_ROTATION_ANGLE_OFFSET
		   && previousVector.sqrMagnitude > Mathf.Pow(REQUIRED_ROTATION_FINGER_SEPARATION,	2)) {
			float direction = LR.z > 0f ? 1.0f : -1.0f;
			cameraOrbit(angleOffset, direction);
		}

		// vertical pan
		// TODO

	}

	/***********************************************************
	 * ACTIONS
	 ***********************************************************/

	/**
	 * Orbit the camera around the game board.
	 * 
	 * @param xDelta The input direction depicting the orbit direction.
	 */
	void cameraOrbit(float angle, float direction) {
		transform.RotateAround(target.position, new Vector3(0f, 1f, 0f), direction * angle * ORBIT_SPEED);
	}




}
