using UnityEngine;
using UnityEngine.UI;
using System.IO;

public class AudioPanel : MonoBehaviour
{
	public Text title;
	public AudioControl audioControl;
	private string fullPath;
	private bool dirty;

	public void Init(string newTitle, string fullPath)
	{
		if (!File.Exists(fullPath))
		{
			Toasts.AddToast(5, "Corrupted audio, ABORT ABORT ABORT");
			return;
		}

		this.fullPath = fullPath;
		title.text = newTitle;
		dirty = true;
	}

	public void OnEnable()
	{
		if (dirty)
		{
			audioControl.Init(fullPath);
			dirty = false;
		}
	}
}
