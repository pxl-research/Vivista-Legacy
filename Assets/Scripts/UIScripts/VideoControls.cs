using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class VideoControls : Hittable {

	public float ammount;
	public Texture iconPlay;
	public Texture iconPause;
	public static VideoController videoController;
	
	// Update is called once per frame
	//void Update () {
		// TODO(Lander): update icon depending on play state	
	//}

	public void Skip() 
	{
		videoController.video.time += ammount;
	}

	public void Toggle()
	{
		videoController.TogglePlay();
		GetComponent<RawImage>().texture = videoController.playing ? iconPause : iconPlay;
	}

	public void OnHoverStart()
	{
		// TODO(Lander): change background instead of foreground 
		GetComponent<RawImage>().color = Color.red;
	}

	public void OnHoverEnd()
	{
		// TODO(Lander): change background instead of foreground 
		GetComponent<RawImage>().color = Color.black;
	}
}
