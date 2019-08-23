using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;
using UnityEngine.XR;

public class AudioPanel : MonoBehaviour
{

	public GameObject playButton;
	public Canvas canvas;
	public Texture iconPlay;
	public Texture iconPause;
	public Text title;

	private AudioSource audioSource;
	private AudioClip clip;
	public Slider audioTimeSlider;

	//added
	private float fullClipLength;
	private float currentClipTime;
	public Text clipTimetext;

	public string url;

	void Awake()
	{
		audioSource = gameObject.GetComponent<AudioSource>();
		//NOTE(Kristof): Initial rotation towards the camera 
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		audioSource.playOnAwake = false;

		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}

	// Update is called once per frame
	void Update()
	{
		if (clip == null)
		{
			StartCoroutine(GetAudioClip(url));
		}
		playButton.GetComponent<RawImage>().texture = audioSource.isPlaying ? iconPause : iconPlay;

		// NOTE(Lander): Rotate the panels to the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		ShowAudioPlayTime();
	}

	public void Init(string newTitle, string fullPath, string guid)
	{
		if (Player.hittables != null)
		{
			GetComponentInChildren<Hittable>().enabled = true;
		}

		if (!File.Exists(fullPath))
		{
			Toasts.AddToast(5, "Corrupted video, ABORT ABORT ABORT");
		}

		url = fullPath;

		title.text = newTitle;
		audioSource.playOnAwake = false;
	}

	public void TogglePlay()
	{
		if (audioSource.isPlaying)
		{
			audioSource.Pause();
		}
		else
		{
			audioSource.Play();
		}
	}

	IEnumerator GetAudioClip(string urlToLoad)
	{
		var extension = Path.GetExtension(urlToLoad);
		var audioType = AudioType.UNKNOWN;
		if (extension == ".mp3")
		{
			audioType = AudioType.MPEG;
		}
		if (extension == ".ogg")
		{
			audioType = AudioType.OGGVORBIS;
		}
		if (extension == ".aif" || extension == ".aiff")
		{
			audioType = AudioType.AIFF;
		}
		if (extension == ".wav")
		{
			audioType = AudioType.WAV;
		}

		using (var www = UnityWebRequestMultimedia.GetAudioClip("file://" + urlToLoad, audioType))
		{
			www.timeout = 2;
			www.SendWebRequest();

			while (!www.isDone)
			{
				yield return null;
			}

			if (www.isDone)
			{
				clip = DownloadHandlerAudioClip.GetContent(www);
				clip.LoadAudioData();
				audioSource.clip = clip;
				ShowAudioPlayTime();
				audioSource.Play();

				fullClipLength = clip.length;
			}
			else if (www.isNetworkError)
			{
				Debug.Log(www.error);
			}
			else if (www.isHttpError)
			{
				Debug.Log(www.error);
			}
			else
			{
				Debug.Log("Something went wrong while downloading audio file. No errors, but not done either.");
			}
		}
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}

	private void ShowAudioPlayTime()
	{
		currentClipTime = audioSource.time;
		clipTimetext.text = $"{MathHelper.FormatSeconds(currentClipTime)} / {MathHelper.FormatSeconds(fullClipLength)}";
		audioTimeSlider.maxValue = fullClipLength;
		audioTimeSlider.value = currentClipTime;
	}
}
