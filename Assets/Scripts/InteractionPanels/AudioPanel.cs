using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class AudioPanel : MonoBehaviour
{
	public Text title;
	public AudioControl audioControl;

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
}
