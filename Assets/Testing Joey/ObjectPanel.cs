using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR;
using System;

public class ObjectPanel : MonoBehaviour {

	public Text title;
	public Text body;
	public GameObject objectToRotate;
	public RectTransform panel;
	public Canvas canvas;

	public string url;

	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}

	void Update()
	{
		// NOTE(Kristof): Turning every frame only needs to happen in Editor
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}
	}











	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}

	internal void Init(string answerTitle, string pathExt, string v1, bool v2)
	{
		throw new NotImplementedException();
	}
}
