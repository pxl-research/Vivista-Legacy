using UnityEngine;
using UnityEngine.UI;

public class VideoControls : MonoBehaviour
{
	public Texture iconPlay;
	public Texture iconPause;

	public RawImage playImage;

	public static VideoController videoController;

	void Update()
	{
		GetComponent<BoxCollider>().enabled = transform.root.GetComponent<Canvas>().enabled;

		playImage.texture = videoController.playing ? iconPause : iconPlay;
	}

	public void Skip(float amount)
	{
		videoController.SeekRelative(amount);
	}

	public void Toggle()
	{
		videoController.TogglePlay();
	}
}
