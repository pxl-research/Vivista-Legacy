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

	public static Canvas seekbar
	{
		get { return _seekBarCanvas ?? (_seekBarCanvas = GameObject.Find("Seekbar Canvas").GetComponent<Canvas>()); }
	}
	private static Canvas _seekBarCanvas;

	public static GameObject modalBackground
	{
		get { return _modalBackground  ?? (_modalBackground = main.transform.Find("ModalBackground").gameObject); }
	}
	private static GameObject _modalBackground;
}
