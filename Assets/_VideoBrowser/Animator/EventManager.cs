using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour 
{
	public delegate void AnimateMenu();
	public static AnimateMenu OnSpace;

	void Update ()
	{
		if (Input.GetKeyDown (KeyCode.Space)) {
			if (OnSpace != null) {
				OnSpace ();
			}
		}
	}
}
