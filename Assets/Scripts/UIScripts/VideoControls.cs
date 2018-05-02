using UnityEngine;
using UnityEngine.UI;

public class VideoControls : MonoBehaviour
{

	public float amount;
	public Texture iconPlay;
	public Texture iconPause;

	public static VideoController videoController;
	public static bool seekbarPaused;

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
		if (videoController.videoState > VideoController.VideoState.Intro)
		{
			videoController.video.time += amount;
		}
	}

	public void Toggle()
	{
		if (videoController.videoState > VideoController.VideoState.Intro)
		{
			videoController.TogglePlay();
			seekbarPaused = !videoController.playing;
		}
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
