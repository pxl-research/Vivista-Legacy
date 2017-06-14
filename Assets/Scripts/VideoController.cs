using System;
using System.Diagnostics;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

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

	//NOTE(Simon): Takes about 50 ms so dont go crazy!
	public void SaveScreenshotToDisk(string filename)
	{
		if (video.isPrepared)
		{
			var currentActiveRT = RenderTexture.active;

			RenderTexture.active = video.texture as RenderTexture;

			var tex = new Texture2D(video.texture.width, video.texture.height);
			tex.ReadPixels(new Rect(0, 0, tex.width, tex.height), 0, 0);

			var jpg = tex.EncodeToJPG();
			File.WriteAllBytes(filename, jpg);
			Debug.Log("Writing screenshot");
			
			RenderTexture.active = currentActiveRT;
		}
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

	public void Pause()
	{
		video.Pause();
		playing = false;
	}
}
