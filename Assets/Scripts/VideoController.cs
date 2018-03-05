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
	public int frameIndex;
}

public class VideoController : MonoBehaviour 
{
	public VideoPlayer video;
	public VideoPlayer screenshots;
	public bool playing = true;
	public RenderTexture baseRenderTexture;

	public bool videoLoaded;

	public double videoLength;
	public double currentTime;
	public double currentFractionalTime;

	public ScreenshotParams screenshotParams;

	public RectTransform seekbar;
	public Text timeText;

	void Start () 
	{
		var players = GetComponents<VideoPlayer>();
		if (players[0].playOnAwake)
		{
			screenshots = players[1];
			video = players[0];
		}
		else
		{
			screenshots = players[0];
			video = players[1];
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
		screenshots.enabled = true;
		screenshots.prepareCompleted += OnPrepared;
		screenshots.Prepare();
		
		screenshotParams = new ScreenshotParams
		{
			frameIndex = frameIndex,
			width = width,
			height = height,
			keepAspect = keepAspect,
			filename = filename
		};
	}

	public void OnPrepared(VideoPlayer vid)
	{
		screenshots.sendFrameReadyEvents = true;
		screenshots.frameReady += OnScreenshotRendered;
		screenshots.playbackSpeed = 0.01f;
		screenshots.Play();
		screenshots.frame = screenshotParams.frameIndex;
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

		Graphics.SetRenderTarget(screenshots.targetTexture);
		var tex = new Texture2D(screenshots.texture.width, screenshots.texture.height, TextureFormat.RGB24, false);
		tex.ReadPixels(new Rect(0, 0, screenshots.texture.width, screenshots.texture.height), 0, 0);
		TextureScale.Bilinear(tex, (int)screenshotParams.width, (int)screenshotParams.height);

		var data = tex.EncodeToJPG(50);

		screenshots.frameReady -= OnScreenshotRendered;
		screenshots.sendFrameReadyEvents = false;
		screenshots.prepareCompleted -= OnPrepared;

		using (var thumb = File.Create(screenshotParams.filename))
		{
			thumb.Write(data, 0, data.Length);
			thumb.Close();
		}

		screenshots.enabled = false;
		screenshots.Pause();
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
		video.Prepare();

		video.prepareCompleted += delegate
		{
			int videoWidth = video.texture.width;
			int videoHeight = video.texture.height;
			var perspective = Perspective.PerspectiveFlat;
			if (videoWidth == videoHeight * 2)
			{
				perspective = Perspective.Perspective360;
			}
			else if (videoWidth == videoHeight)
			{
				perspective = Perspective.Perspective180;
			}
			SetPerspective(perspective, videoWidth, videoHeight);

			videoLoaded = true;


			//NOTE(Kristof):loading the video at video.Time = 0, or loading it at video.frame = 1, and then pausing it will show a black screen, this is unclear for the user.
			video.frame = 2;
			video.Pause();
		};

		
		video.errorReceived += delegate(VideoPlayer player, string message)
		{
			videoLoaded = false;
			Debug.Log(message);
		};
		screenshots.errorReceived += delegate(VideoPlayer player, string message)
		{
			Debug.Log(message);
		};
	}

	public void SetPerspective(Perspective perspective, int width, int height)
	{
		switch (perspective)
		{
			case Perspective.Perspective360:
			{
				//currentCamera = Instantiate(camera360);
				//videoController.GetComponent<MeshFilter>().sharedMesh = Mesh360;

				Destroy(GetComponent<BoxCollider>());
				var coll = gameObject.AddComponent<SphereCollider>();
				//coll.radius = 0.75f;
				coll.radius = 90f;

				var descriptor = baseRenderTexture.descriptor;
				descriptor.sRGB = false;
				descriptor.width = width;
				descriptor.height = height;
				
				var renderTexture = new RenderTexture(descriptor);
				RenderSettings.skybox.mainTexture = renderTexture;
				video.targetTexture = renderTexture;

				//TODO(Simon) Fix colors, looks way too dark
				screenshots.targetTexture = new RenderTexture(descriptor);

				transform.localScale = Vector3.one;
				transform.position = Vector3.zero;
				break;
			}
			/*
			case Perspective.Perspective180:
			{
				//currentCamera = Instantiate(camera180);
				GetComponent<MeshFilter>().sharedMesh = Mesh180;

				Destroy(GetComponent<BoxCollider>());
				gameObject.AddComponent<SphereCollider>();

				GetComponent<MeshRenderer>().material = Material180;
				transform.localScale = Vector3.one;
				transform.position = Vector3.zero;
				break;
			}
			case Perspective.PerspectiveFlat:
			{
				//currentCamera = Instantiate(cameraFlat);
				GetComponent<MeshFilter>().sharedMesh = MeshFlat;

				Destroy(GetComponent<BoxCollider>());
				gameObject.AddComponent<BoxCollider>();

				GetComponent<MeshRenderer>().material = MaterialFlat;
				transform.localScale = new Vector3(1.536f, 0.864f, 1);
				transform.position = new Vector3(0, 0, 1);
				break;
			}
			*/
		}
	}

	public string VideoPath()
	{
		return video.url;
	}

	public void Pause()
	{
		video.Pause();
		playing = false;
	}
}
