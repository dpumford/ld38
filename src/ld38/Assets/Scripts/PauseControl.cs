using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PauseControl : MonoBehaviour {
	public GameObject PauseScreen;
	public KeyCode PauseKey = KeyCode.E;

	private int PAUSED = 0;
	private int UNPAUSED = 1;

	// Use this for initialization
	void Start () {
	}
	
	// Update is called once per frame
	void Update () {
		if (Input.GetKeyDown (PauseKey)) {
			if (Time.timeScale == PAUSED) {
				PauseScreen.GetComponent<Canvas>().enabled = false;
				UnpauseGame ();
			} else {
				PauseScreen.GetComponent<Canvas>().enabled = true;
				PauseGame ();
			}
		}
	}

	private void PauseGame (){
		Time.timeScale = PAUSED;
	}

	private void UnpauseGame(){
		Time.timeScale = UNPAUSED;
	}
}
