using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Object3DPanel : MonoBehaviour
{
	public Text title;
	public Transform objectRenderer;

	private object object3d;

	public void Init(string newTitle, string fullPath)
	{
		title.text = newTitle;


	}
}
