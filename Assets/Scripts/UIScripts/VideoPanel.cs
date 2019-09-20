using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanel : MonoBehaviour
{
	public RenderTexture videoRenderTexture;
	public GameObject controlButton;
	public Texture iconPlay;
	public Texture iconPause;
	public Text title;
	public RawImage videoSurface;
	public VideoPlayer videoPlayer;
	public AudioSource audioSource;

	public static bool keepFileNames;
	public string url;
	
	void Update()
	{
		if (!videoSurface)
		{
			return;
		}

		controlButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
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
		videoPlayer.Prepare();
		videoPlayer.prepareCompleted += OnPrepareComplete;
		title.text = newTitle;

		audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		videoPlayer.EnableAudioTrack(0, true);
		videoPlayer.SetTargetAudioSource(0, audioSource);
		videoPlayer.controlledAudioTrackCount = 1;
	}

	private void OnPrepareComplete(VideoPlayer source)
	{
		videoRenderTexture.width = (int)videoPlayer.clip.width;
		videoRenderTexture.height = (int)videoPlayer.clip.height;
		videoPlayer.Play();
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		GetComponent<Canvas>().GetComponent<RectTransform>().position = position;
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
	}
}
