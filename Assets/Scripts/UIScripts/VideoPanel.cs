using System;
using System.IO;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class VideoPanel : MonoBehaviour
{
	public RenderTexture videoRenderTexture;
	public GameObject videoContainer;
	public GameObject controllButton;
	public Canvas canvas;
	public Texture iconPlay;
	public Texture iconPause;

	public Text title;
	public string url;

	private RawImage videoSurface;
	private VideoPlayer videoPlayer;


	public void Init(Vector3 position, string newTitle, string fullPath, string guid, bool prepareNow = false)
	{
		foreach (var img in GetComponentsInChildren<RawImage>())
		{
			Debug.Log(img);
		}
		videoPlayer = videoContainer.GetComponent<VideoPlayer>();
		videoSurface = videoContainer.GetComponent<RawImage>();
		videoRenderTexture = Instantiate(videoRenderTexture);
		videoPlayer.targetTexture = videoRenderTexture;
		videoSurface.texture = videoRenderTexture;
		videoSurface.color = Color.white;
		//controllButton = GetComponentsInChildren<RawImage>()[2].gameObject;


		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().enabled = true;
		}

		// HACK(Lander): temporary fix for badly looping videos, despite loop being enabled.
		//videoPlayer.loopPointReached += (obj) => { obj.time = 0; obj.Play(); };


		var folder = Path.Combine(Application.persistentDataPath, guid);

		if (!File.Exists(fullPath))
		{
			var pathNoExtension = Path.Combine(Path.Combine(folder, "extra"), Path.GetFileNameWithoutExtension(fullPath));
			if (!File.Exists(pathNoExtension))
			{
				Debug.LogWarningFormat("Cannot find extension-less file: {1} {0}", pathNoExtension, File.Exists(pathNoExtension));
				return;
			}

			try
			{
				File.Move(pathNoExtension, fullPath);
			}
			catch (IOException e)
			{
				Debug.LogWarningFormat("Cannot add extension to file: {0}\n{2}\n{1}", pathNoExtension, e.Message, fullPath);
				return;
			}

		}
		videoPlayer.url = fullPath;
		//videoPlayer.Prepare();
		//videoPlayer.prepareCompleted += OnPrepared;
		//videoPlayer.frame = 0; // NOTE(Lander): redundancy
		//transform.parent.localScale = new Vector3(1,0.2f,0.2f);
		transform.localPosition = position;
		//Move
		//gameObject.SetActive(true);
		title.text = newTitle;
	}

	void Start()
	{
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
	}
	//public void OnPrepared(VideoPlayer player)
	//{
	//	player.Play();
	//}

	// Update is called once per frame
	void Update()
	{
		if (!videoSurface) return;
		var texture = videoSurface.texture;

		//NOTE(Simon): Title + Triangle + bottomMargin
		const float extraHeight = 40 + 16 + 10;
		//NOTE(Simon): LeftMargin + RightMargin;
		const float extraWidth = 10 + 10;

		float newWidth = (Screen.width / 2f);
		float newHeight = (Screen.height / 2f);
		float imageRatio = newWidth / newHeight;

		//NOTE(Simon): Portrait
		if (imageRatio <= 1)
		{
			float ratio = (texture.width + extraWidth) / newWidth;
			newHeight = (texture.height + extraHeight) / ratio;
		}
		//NOTE(Simon): Landscape
		else
		{
			float ratio = (texture.height + extraHeight) / newHeight;
			newWidth = (texture.width + extraWidth) / ratio;
		}

		//canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHeight);
		controllButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
	}

	public void TogglePlay()
	{
		// HACK(Lander): toggle play
		(videoPlayer.isPlaying ? (Action)videoPlayer.Pause : videoPlayer.Play)();

		controllButton.GetComponent<RawImage>().texture = videoPlayer.isPlaying ? iconPause : iconPlay;
		//Debug.LogFormat("Videopanel {0}, state: {1}", this.title, videoPlayer.isPlaying);

	}

	// NOTE(Lander): copied from image panel
	public void Move(Vector3 position)
	{
		Vector3 newPos;

		newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
		newPos.y += 0.015f;

		canvas.GetComponent<RectTransform>().position = newPos;
		canvas.transform.rotation = Camera.main.transform.rotation;
	}
	private void OnDestroy()
	{
		// TODO(Lander): this can be empty in some cases.
		var filename = videoPlayer.url;
		if (File.Exists(filename) && Path.GetExtension(filename) != "")
		{
			var newfilename = Path.Combine(Path.GetDirectoryName(filename), Path.GetFileNameWithoutExtension(filename));
			try
			{
				Debug.LogFormat("Moving {0} to {1}", filename, newfilename);
				File.Move(filename, newfilename);
			}
			catch (IOException e)
			{
				try
				{
					Debug.LogFormat("File Already exists? deleting: {0}", newfilename);
					File.Delete(filename);
				}
				catch (IOException e2)
				{
					Debug.LogErrorFormat("Something went wrong while moving the file. Stopping. \n{} ", e2.Message);
				}
			}
		}
	}
}
