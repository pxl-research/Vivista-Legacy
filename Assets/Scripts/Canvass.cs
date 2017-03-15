using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canvass : MonoBehaviour
{ 
	public static Canvas main 
	{
		get { return _canvass ?? (_canvass = GameObject.Find("Canvas").GetComponent<Canvas>()); }
	}
	private static Canvas _canvass;
}
