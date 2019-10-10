using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanel : MonoBehaviour
{
	public RenderTexture videoRenderTexture;
	public Button controlButton;
	public Button bigButton;
	public Slider progressBar;
	public Texture iconPlay;
	public Texture iconPause;
	public Text title;
	public Text timeDisplay;
	public RawImage videoSurface;
	public VideoPlayer videoPlayer;
	public AudioSource audioSource;

	public static bool keepFileNames;
	public string url;

	public void Update()
	{
		float time = (float)videoPlayer.time;
		float length = videoPlayer.frameCount / videoPlayer.frameRate;
		progressBar.value = time;
		progressBar.maxValue = length;
		timeDisplay.text = $"{MathHelper.FormatSeconds(time)} / {MathHelper.FormatSeconds(length)}";
	}

	public void Init(string newTitle, string fullPath)
	{
		videoRenderTexture = Instantiate(videoRenderTexture);
		videoPlayer.targetTexture = videoRenderTexture;
		videoSurface.texture = videoRenderTexture;
		videoSurface.color = Color.white;

		if (Player.hittables != null)
		{
			//GetComponentInChildren<Hittable>().enabled = true;
		}

		videoPlayer.url = fullPath;
		videoPlayer.playOnAwake = false;
		videoPlayer.Prepare();
		videoPlayer.prepareCompleted += OnPrepareComplete;
		title.text = newTitle;

		audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		videoPlayer.EnableAudioTrack(0, true);
		videoPlayer.SetTargetAudioSource(0, audioSource);
		videoPlayer.controlledAudioTrackCount = 1;

		controlButton.onClick.AddListener(TogglePlay);
		bigButton.onClick.AddListener(TogglePlay);
		progressBar.on.AddListener(OnSeek);
	}

	private void OnPrepareComplete(VideoPlayer source)
	{
		videoRenderTexture.width = source.texture.width;
		videoRenderTexture.height = source.texture.height;
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		GetComponent<Canvas>().GetComponent<RectTransform>().position = position;
	}

	private void OnSeek(float value)
	{
	}

	public void TogglePlay()
	{
		if (videoPlayer.isPlaying)
		{
			videoPlayer.Pause();
		}
		else
		{
			videoPlayer.Play();
		}

		controlButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
		bigButton.GetComponent<RawImage>().enabled = !videoPlayer.isPlaying;
	}
}
