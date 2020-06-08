using UnityEngine;
using UnityEngine.UI;

public class VideoControls : MonoBehaviour
{
	public float amount;
	public Texture iconPlay;
	public Texture iconPause;

	public RawImage buttonImage;

	public static VideoController videoController;

	void Update()
	{
		GetComponent<BoxCollider>().enabled = transform.root.GetComponent<Canvas>().enabled;

		buttonImage.texture = videoController.playing ? iconPause : iconPlay;
	}

	public void Skip()
	{
		videoController.video.time += amount;
	}

	public void Toggle()
	{
		videoController.TogglePlay();
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
