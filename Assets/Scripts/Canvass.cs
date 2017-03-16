using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Canvass : MonoBehaviour
{ 
	public static Canvas main 
	{
		get { return _canvas ?? (_canvas = GameObject.Find("Canvas").GetComponent<Canvas>()); }
	}
	private static Canvas _canvas;
}
