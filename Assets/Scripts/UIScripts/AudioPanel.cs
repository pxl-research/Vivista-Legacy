using UnityEngine;
using UnityEngine.UI;
using System.IO;
using UnityEngine.XR;

public class AudioPanel : MonoBehaviour
{
	public Canvas canvas;
	public Text title;
	public AudioControl audioControl;

	void Awake()
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
		// NOTE(Lander): Rotate the panels to the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
	}

	public void Init(string newTitle, string fullPath)
	{
		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().enabled = true;
		}

		if (!File.Exists(fullPath))
		{
			Toasts.AddToast(5, "Corrupted video, ABORT ABORT ABORT");
			return;
		}

		audioControl.Init(fullPath);

		title.text = newTitle;
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}
}
