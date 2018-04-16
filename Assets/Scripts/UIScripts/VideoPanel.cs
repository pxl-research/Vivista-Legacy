using System;
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


	//public void Init(Vector3 positon, string title, string filename)
	public void Init(Vector3 position, string newTitle, string filename, bool prepareNow = false)
	{
		// HACK(Lander): make sure the panel is initialised
		if (!videoPlayer)
		{
			Start();
		}

		//videoPlayer.playOnAwake = prepareNow;

		videoPlayer.url = filename;
		videoPlayer.Prepare();
		//videoPlayer.frame = 0; // NOTE(Lander): redundancy
		videoPlayer.Play();
		//transform.parent.localScale = new Vector3(1,0.2f,0.2f);
		transform.localPosition = position;
		//Move
		//gameObject.SetActive(true);
		title.text = newTitle;
	}

	void Start()
	{

		videoSurface = videoContainer.GetComponent<RawImage>();
		videoPlayer = videoContainer.GetComponent<VideoPlayer>();
		videoPlayer.targetTexture = Instantiate(videoRenderTexture);
		videoSurface.texture = videoPlayer.targetTexture;
		controllButton = GetComponentsInChildren<RawImage>()[1].gameObject;

		// HACK(Lander): temporary fix for badly looping videos, despite loop being enabled.
		//videoPlayer.loopPointReached += (obj) => { obj.time = 0; obj.Play(); };

		transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		//transform.eulerAngles = new Vector3(0, 90);
		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().gameObject.SetActive(true);
		}
	}

	// Update is called once per frame
	void Update()
	{
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
		controllButton.GetComponent<RawImage>().texture  = videoPlayer.isPlaying ? iconPause : iconPlay;
	}

	public void TogglePlay()
	{
		// HACK(Lander): toggle play
		(videoPlayer.isPlaying ? (Action)videoPlayer.Pause : videoPlayer.Play)();

		controllButton.GetComponent<RawImage>().texture  = videoPlayer.isPlaying ? iconPause : iconPlay;

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
}
