using UnityEngine;
using UnityEngine.UI;

public class VideoControls : MonoBehaviour
{

	public float amount;
	public Texture iconPlay;
	public Texture iconPause;

	public static VideoController videoController;

	// Update is called once per frame
	void Update()
	{
		GetComponent<BoxCollider>().enabled = transform.root.GetComponent<Canvas>().enabled;

		if (gameObject.name.Equals("TogglePlay"))
		{
			GetComponent<RawImage>().texture = videoController.playing ? iconPause : iconPlay;
		}
	}

	public void Skip()
	{
		videoController.video.time += amount;
	}

	public void Toggle()
	{
		videoController.TogglePlay();
		VideoController.autoResume = videoController.playing;
	}

	public void OnHoverStart()
	{
		// TODO(Lander): change background instead of foreground 
		GetComponent<RawImage>().color = Color.red;
	}

	public void OnHoverEnd()
	{
		// TODO(Lander): change background instead of foreground 
		GetComponent<RawImage>().color = Color.white;
	}
}
