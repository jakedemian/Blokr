using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenHelper : MonoBehaviour {

	public static Vector2 getScreenDimensions() {
		return new Vector2(Screen.width, Screen.height);
	}

	public static Vector2 getScreenMidpoint() {
		Vector2 screenDimensions = getScreenDimensions();
		return new Vector2(screenDimensions.x / 2, screenDimensions.y / 2);
	}
}
