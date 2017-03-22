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
		if (!(Input.GetKey(KeyCode.LeftShift) || Input.GetKey(KeyCode.RightShift))
			&& Input.GetKeyDown(KeyCode.Space))
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

		videoLength = video.frameCount / video.frameRate;
		currentTime = videoLength * (video.frame / (double)video.frameCount);
		currentFractionalTime = video.frame / (double)video.frameCount;

		seekbar.anchorMax = new Vector2((float) currentFractionalTime, seekbar.anchorMax.y);
		timeText.text = String.Format(" {0} / {1}", FormatSeconds(currentTime), FormatSeconds(videoLength));
	}

	string FormatSeconds(double time)
	{
		var hours = (int)(time / (60 * 60));
		time -= hours * 60;
		var minutes = (int)(time / 60);
		time -= minutes * 60;
		var seconds = (int) time;

		var formatted = "";
		if (hours > 0)
		{
			formatted += hours + ":";
		}

		formatted += minutes.ToString("D2");
		formatted += ":";
		formatted += seconds.ToString("D2");

		return formatted;
	}

	public void Seek(float fractionalTime)
	{
		video.time = fractionalTime * videoLength;
	}
}
