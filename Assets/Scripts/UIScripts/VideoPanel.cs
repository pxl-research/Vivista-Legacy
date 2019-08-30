using System;
using System.IO;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanel : MonoBehaviour
{
	public RenderTexture videoRenderTexture;
	public GameObject controlButton;
	public Canvas canvas;
	public Texture iconPlay;
	public Texture iconPause;
	public Text title;
	public RawImage videoSurface;
	public VideoPlayer videoPlayer;
	public AudioSource audioSource;

	public static bool keepFileNames;
	public string url;
	
	void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera 
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
	}

	void Update()
	{
		if (!videoSurface) return;

		controlButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;

		// NOTE(Lander): Rotate the panels to the camera
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}
	}

	public void Init(string newTitle, string fullPath)
	{
		videoRenderTexture = Instantiate(videoRenderTexture);
		videoPlayer.targetTexture = videoRenderTexture;
		videoSurface.texture = videoRenderTexture;
		videoSurface.color = Color.white;

		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().enabled = true;
		}

		videoPlayer.url = fullPath;
		title.text = newTitle;

		audioSource = videoPlayer.gameObject.AddComponent<AudioSource>();
		audioSource.playOnAwake = false;

		videoPlayer.EnableAudioTrack(0, true);
		videoPlayer.SetTargetAudioSource(0, audioSource);
		videoPlayer.controlledAudioTrackCount = 1;

		//NOTE(Lander): duct tape
		videoPlayer.enabled = false;
		videoPlayer.enabled = true;
	}

	// NOTE(Lander): copied from image panel
	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}

	public void TogglePlay()
	{
		// HACK(Lander): toggle play
		(videoPlayer.isPlaying ? (Action)videoPlayer.Pause : videoPlayer.Play)();
		controlButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
	}
}
