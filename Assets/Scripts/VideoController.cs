using System;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;

public class VideoController : MonoBehaviour 
{
	public VideoPlayer video;
	public bool playing = true;

	public double videoLength;
	public double currentTime;
	public double currentFractionalTime;

	public RectTransform seekbar;
	public Text timeText;

	void Start () 
	{
		video = GetComponent<VideoPlayer>();
		playing = video.isPlaying;
	}
	
	void Update () 
	{
		videoLength = video.frameCount / video.frameRate;
		currentTime = videoLength * (video.frame / (double)video.frameCount);
		currentFractionalTime = video.frame / (double)video.frameCount;

		seekbar.anchorMax = new Vector2((float) currentFractionalTime, seekbar.anchorMax.y);
		timeText.text = String.Format(" {0} / {1}", MathHelper.FormatSeconds(currentTime), MathHelper.FormatSeconds(videoLength));
	}

	
	public void Seek(float fractionalTime)
	{
		video.time = fractionalTime * videoLength;
	}

	public void TogglePlay()
	{
		if (!playing)
		{
			video.Play();
			playing = true;
		}
		else
		{
			video.Pause();
			playing = false;
		}
	}
}
