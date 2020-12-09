using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;
using UnityEngine.XR;

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
	public enum VideoState
	{
		Watching
	}

	public VideoPlayer video;
	public VideoPlayer screenshots;
	public bool playing = true;
	public RenderTexture baseRenderTexture;
	public AudioSource audioSource;
	public AudioMixer mixer;

	private Hittable decreaseVolumeButton;
	private Hittable increaseVolumeButton;
	private Slider volumeSlider;
	private Slider volumeSliderVR;

	private bool volumeChanging;
	private bool increaseButtonPressed;
	private bool decreaseButtonPressed;
	private float volumeButtonClickTime;
	private bool volumeSliderDisabled;

	public bool videoLoaded;

	public double videoLength;
	public double currentFractionalTime;

	public delegate void SeekEvent(double time);
	public SeekEvent OnSeek;

	public ScreenshotParams screenshotParams;

	public VideoState videoState;

	//NOTE(Kristof): Keep the variable public so that other classes can use it instead of using the property
	//NOTE(Kristof): Better way to do this?
	public double rawCurrentTime;
	public double currentTime
	{
		get
		{
			if (videoState >= VideoState.Watching)
			{
				return rawCurrentTime;
			}

			return -1;
		}
	}

	void Start()
	{
		var players = GetComponents<VideoPlayer>();
		video = players[0].playOnAwake ? players[0] : players[1];
		screenshots = players[0].playOnAwake ? players[1] : players[0];
		if (!SceneManager.GetActiveScene().name.Equals("Editor"))
		{
			Destroy(screenshots);
		}

		audioSource = video.gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		audioSource.outputAudioMixerGroup = mixer.FindMatchingGroups("Video")[0];
		mixer.SetFloat(Config.mainVideoMixerChannelName, MathHelper.LinearToLogVolume(Config.MainVideoVolume));

		playing = video.isPlaying;

		volumeSlider = GameObject.Find("VolumeControl").GetComponentInChildren<Slider>();
		var volumeControlVR = GameObject.Find("VolumeControlVR");
		var decreaseVolumeGo = GameObject.Find("DecreaseVolume");
		var increaseVolumeGo = GameObject.Find("IncreaseVolume");

		if (volumeControlVR != null)
		{
			volumeSliderVR = volumeControlVR.GetComponentInChildren<Slider>();
			volumeSliderVR.interactable = false;
			volumeSliderVR.value = Config.MainVideoVolume;
			volumeSliderVR.onValueChanged.AddListener(_ => VolumeValueChangedVR());
		}
		if (decreaseVolumeGo != null && increaseVolumeGo != null)
		{
			decreaseVolumeButton = decreaseVolumeGo.GetComponent<Hittable>();
			increaseVolumeButton = increaseVolumeGo.GetComponent<Hittable>();
			decreaseVolumeButton.onHit.AddListener(DecreaseVolume);
			increaseVolumeButton.onHit.AddListener(IncreaseVolume);
			decreaseVolumeButton.onHitDown.AddListener(OnPointerDownDecreaseButton);
			increaseVolumeButton.onHitDown.AddListener(OnPointerDownIncreaseButton);
			decreaseVolumeButton.onHitUp.AddListener(OnPointerUpVolumeButton);
			increaseVolumeButton.onHitUp.AddListener(OnPointerUpVolumeButton);

		}

		volumeSlider.value = Config.MainVideoVolume;
		volumeSlider.onValueChanged.AddListener(_ => VolumeValueChanged());
	}

	void Update()
	{
		CheckButtonStates();

		//TODO(Jitse): Is this the best place to do this? => 
		//NOTE(Jitse): Remove the volume slider in PlayerInfo and fix position of Time
		//NOTE(cont.): Doing this here because XRDevice is not yet active in Start()
		//NOTE(cont.): Using boolean to only run code inside if-check once
		if (XRSettings.isDeviceActive && !volumeSliderDisabled)
		{
			var timeGo = GameObject.Find("Time");
			Vector3 newPosition = timeGo.transform.localPosition;
			newPosition.y = 12.5f;
			timeGo.transform.localPosition = newPosition;
			volumeSlider.transform.parent.gameObject.SetActive(false);
			volumeSliderDisabled = true;
		}

		videoLength = video.frameCount / video.frameRate;
		rawCurrentTime = videoLength * (video.frame / (double)video.frameCount);
		currentFractionalTime = video.frameCount > 0 ? video.frame / (double)video.frameCount : 0;
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
		var tex = new Texture2D(screenshots.texture.width, screenshots.texture.height, TextureFormat.RGB24, false, true);
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

	public void SeekFractional(float fractionalTime)
	{
		var newTime = fractionalTime * videoLength;
		video.time = newTime;
		OnSeek.Invoke(newTime);
	}

	public void SeekNoTriggers(double time)
	{
		video.time = time;
	}

	public void SetPlaybackSpeed(float speed)
	{
		video.playbackSpeed = speed;
	}

	public double TimeForFraction(float fractionalTime)
	{
		return fractionalTime * videoLength;
	}

	public void TogglePlay()
	{
		videoState = VideoState.Watching;

		if (!playing)
		{
			Play();
		}
		else
		{
			Pause();
		}
	}

	public void Play()
	{
		video.Play();
		audioSource.Play();
		playing = true;
	}

	public void Pause()
	{
		video.Pause();
		audioSource.Pause();
		playing = false;
	}

	public void PlayFile(string filename)
	{
		video.source = VideoSource.Url;
		video.controlledAudioTrackCount = 1;
		video.EnableAudioTrack(0, true);
		video.audioOutputMode = VideoAudioOutputMode.AudioSource;
		video.SetTargetAudioSource(0, audioSource);

		video.Prepare();
		video.url = filename;

		if (screenshots != null)
		{
			screenshots.url = filename;
		}

		video.prepareCompleted += _ =>
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

			video.frame = 2;
			video.Pause();
		};


		video.errorReceived += (player, message) =>
		{
			videoLoaded = false;
			Debug.LogError(message);
		};
		screenshots.errorReceived += (player, message) =>
		{
			Debug.LogError(message);
		};
	}

	public void SetPerspective(Perspective perspective, int width, int height)
	{
		switch (perspective)
		{
			case Perspective.Perspective360:
			{
				var descriptor = baseRenderTexture.descriptor;
				descriptor.sRGB = false;
				descriptor.width = width;
				descriptor.height = height;

				var renderTexture = new RenderTexture(descriptor);
				RenderSettings.skybox.mainTexture = renderTexture;
				video.targetTexture = renderTexture;

				//TODO(Simon) Fix colors, looks way too dark
				if (screenshots != null)
				{
					screenshots.targetTexture = new RenderTexture(descriptor);
				}

				transform.localScale = Vector3.one;
				transform.position = Vector3.zero;
				break;
			}
			/*
			NOTE(Simon): This was used when I special cased types of video. Might be needed in the future.
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

	private void CheckButtonStates()
	{
		if (increaseButtonPressed)
		{
			//NOTE(Simon): When button is down, immediately change volume
			if (!volumeChanging)
			{
				IncreaseVolume();
				volumeChanging = true;
			}

			//NOTE(Simon): Every {time interval} change volume
			if (Time.realtimeSinceStartup > volumeButtonClickTime + 0.15)
			{
				volumeChanging = false;
				volumeButtonClickTime = Time.realtimeSinceStartup;
			}
		}
		else if (decreaseButtonPressed)
		{
			//NOTE(Simon): When button is down, immediately change volume
			if (!volumeChanging)
			{
				DecreaseVolume();
				volumeChanging = true;
			}

			//NOTE(Simon): Every {time interval} change volume
			if (Time.realtimeSinceStartup > volumeButtonClickTime + 0.15)
			{
				volumeChanging = false;
				volumeButtonClickTime = Time.realtimeSinceStartup;
			}
		}
	}

	public string VideoPath()
	{
		return video.url;
	}

	public void DecreaseVolume()
	{
		volumeSlider.value -= 0.1f;
		volumeSliderVR.value -= 0.1f;
	}

	public void IncreaseVolume()
	{
		volumeSlider.value += 0.1f;
		volumeSliderVR.value += 0.1f;
	}

	public void VolumeValueChanged()
	{
		mixer.SetFloat(Config.mainVideoMixerChannelName, MathHelper.LinearToLogVolume(volumeSlider.value));
		Config.MainVideoVolume = volumeSlider.value;
	}

	public void VolumeValueChangedVR()
	{
		mixer.SetFloat(Config.mainVideoMixerChannelName, MathHelper.LinearToLogVolume(volumeSliderVR.value));
		Config.MainVideoVolume = volumeSliderVR.value;
	}

	public void OnPointerDownIncreaseButton()
	{
		if (!increaseButtonPressed)
		{
			volumeButtonClickTime = Time.realtimeSinceStartup;
		}
		increaseButtonPressed = true;
	}

	public void OnPointerDownDecreaseButton()
	{
		if (!decreaseButtonPressed)
		{
			volumeButtonClickTime = Time.realtimeSinceStartup;
		}
		decreaseButtonPressed = true;
	}

	public void OnPointerUpVolumeButton()
	{
		decreaseButtonPressed = false;
		increaseButtonPressed = false;
	}

}
