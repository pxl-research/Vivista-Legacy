using System;
using System.IO;
using UnityEngine;
using UnityEngine.Video;
using UnityEngine.UI;
using Debug = UnityEngine.Debug;

public struct ScreenshotParams
{
	public float width;
	public float height;
	public bool keepAspect;
	public string filename;
}

public class VideoController : MonoBehaviour 
{
	public VideoPlayer video;
	public VideoPlayer screenshots;
	public bool playing = true;

	public double videoLength;
	public double currentTime;
	public double currentFractionalTime;

	public ScreenshotParams screenshotParams;

	public RectTransform seekbar;
	public Text timeText;

	void Start () 
	{
		var players = GetComponents<VideoPlayer>();
		if (players[0].renderMode == VideoRenderMode.RenderTexture)
		{
			screenshots = players[0];
			video = players[1];
		}
		else
		{
			screenshots = players[1];
			video = players[0];
		}
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

	//NOTE(Simon): if keepAspect == true, the screenshot will be resized to keep the correct aspectratio, and still fit within the requested size.
	//NOTE(Simon): This executes asynchronously. OnScreenshotRendered will eventually save the image
	public void Screenshot(string filename, int frameIndex, float width, float height, bool keepAspect = true)
	{
		while (!screenshots.isPrepared) {}

		screenshots.sendFrameReadyEvents = true;
		screenshots.frameReady += OnScreenshotRendered;

		screenshots.frame = frameIndex;
		screenshotParams = new ScreenshotParams
		{
			width = width,
			height = height,
			keepAspect = keepAspect,
			filename = filename
		};
	}

	public void OnScreenshotRendered(VideoPlayer vid, long number)
	{
		if (screenshotParams.keepAspect)
		{
			var widthFactor = screenshotParams.width / screenshots.texture.width;
			var heightFactor = screenshotParams.height / screenshots.texture.height;
			if (widthFactor > heightFactor)
			{
				screenshotParams.width = screenshots.texture.width * heightFactor;
			}
			else
			{
				screenshotParams.height = screenshots.texture.height * widthFactor;
			}
		}

		var tex = new Texture2D(screenshots.texture.width, screenshots.texture.height);
		tex.ReadPixels(new Rect(0, 0, screenshots.texture.width, screenshots.texture.height), 0, 0);
		TextureScale.Bilinear(tex, (int)screenshotParams.width, (int)screenshotParams.height);

		var data = tex.EncodeToJPG();

		screenshots.frameReady -= OnScreenshotRendered;
		screenshots.sendFrameReadyEvents = false;

		using (var thumb = File.Create(screenshotParams.filename))
		{
			thumb.Write(data, 0, data.Length);
			thumb.Close();
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

	public void PlayFile(string filename)
	{
		video.url = filename;
		screenshots.url = filename;
		screenshots.Prepare();

		video.time = 0;
		video.Pause();
		video.errorReceived += VideoErrorHandler;
		screenshots.errorReceived += VideoErrorHandler;
	}

	static void VideoErrorHandler(VideoPlayer player, string message)
	{
		Debug.Log(message);
	}

	public void Pause()
	{
		video.Pause();
		playing = false;
	}
}
