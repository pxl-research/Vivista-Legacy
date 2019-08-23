using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Networking;

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
	private float fullLength;
	private float audioplayTime;
	private int seconds;
	private int minutes;
	public Text clipTimetext;

	private bool prepared;

	public string url;

	void Awake()
	{
		audioSource = gameObject.GetComponent<AudioSource>();
		//NOTE(Kristof): Initial rotation towards the camera 
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		audioSource.playOnAwake = false;
		
		
	}

	// Update is called once per frame
	void Update()
	{
		playButton.GetComponent<RawImage>().texture = !audioSource.isPlaying ? iconPause : iconPlay;

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

		var folder = Path.Combine(Application.persistentDataPath, guid);

		if (!File.Exists(fullPath))
		{
			Toasts.AddToast(5, "Corrupted video, ABORT ABORT ABORT");
		}

		url = fullPath;

		title.text = newTitle;
		audioSource.playOnAwake = false;
		ShowAudioPlayTime();
		TogglePlay();

	}

	public void TogglePlay()
	{
		if (clip == null)
		{
			StartCoroutine(GetAudioClip(url));
			return;
		}

		if (audioSource.isPlaying)
		{
			audioSource.Pause();
		}
		else
		{
			audioSource.Play();
		}
	}

	IEnumerator GetAudioClip(string url)
	{
		var extension = Path.GetExtension(url);
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

		//using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("file://" + url, audioType))
		using (UnityWebRequest www = UnityWebRequestMultimedia.GetAudioClip("https://freewavesamples.com/files/Yamaha-V50-Rock-Beat-120bpm.wav", AudioType.WAV))
		{
			yield return www.SendWebRequest();

			if (www.isNetworkError)
			{
				Debug.Log(www.error);
			}
			else if (www.isHttpError)
			{
				Debug.Log(www.error);
			}
			else
			{
				clip = DownloadHandlerAudioClip.GetContent(www);
				clip.LoadAudioData();
				audioSource.clip = clip;
				ShowAudioPlayTime();
				audioSource.Play();

				fullLength = clip.length;
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
		audioplayTime = (int)audioSource.time;
		seconds = (int)audioplayTime % 60;
		minutes = (int)(audioplayTime / 60) % 60;
		clipTimetext.text = $"{minutes}:{seconds} / {(fullLength / 60) % 60}:{fullLength % 60}";
		//clipTimetext.text = minutes + ":" + seconds.ToString("D2") + "/" + ((fullLength / 60) % 60) + ":" + (fullLength % 60).ToString("D2");
	}
}
