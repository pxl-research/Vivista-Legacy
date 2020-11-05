using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.UI;

public class AudioControl : MonoBehaviour
{
	public Button playButton;
	public Slider audioTimeSlider;
	public Text clipTimeText;

	private RawImage playButtonImage;
	public Texture iconPlay;
	public Texture iconPause;

	public Slider volumeSlider;
	public Button decreaseVolumeButton;
	public Button increaseVolumeButton;

	public AudioMixer mixer;

	private AudioSource audioSource;
	private AudioClip clip;

	private string url;
	private float fullClipLength;
	private float currentClipTime;

	private bool volumeChanging;
	private bool increaseButtonPressed;
	private bool decreaseButtonPressed;
	private float volumeButtonClickTime;

	void Awake()
	{
		audioSource = GetComponent<AudioSource>();
		audioSource.playOnAwake = false;
		playButtonImage = playButton.GetComponentInChildren<RawImage>();
	}

	public void Init(string url)
	{
		if (audioSource == null)
		{
			audioSource = GetComponent<AudioSource>();
		}

		audioSource.Stop();
		clip = null;
		this.url = url;
		StartCoroutine(GetAudioClip(url));

		//NOTE(Jitse): The volume buttons are only used in the Player.
		//NOTE(cont.): This check prevents null reference errors.
		if (decreaseVolumeButton != null && increaseVolumeButton != null)
		{
			decreaseVolumeButton.onClick.AddListener(DecreaseVolume);
			increaseVolumeButton.onClick.AddListener(IncreaseVolume);
		}
		
		volumeSlider.onValueChanged.AddListener(_ => VolumeValueChanged());
		volumeSlider.value = Config.AudioInteractionVolume;
		mixer.SetFloat(Config.audioInteractionMixerChannelName, MathHelper.LinearToLogVolume(Config.AudioInteractionVolume));
	}

	void Update()
	{
		CheckButtonStates();

		playButtonImage.texture = audioSource.isPlaying ? iconPause : iconPlay;
		ShowAudioPlayTime();
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
			
			if (Input.GetMouseButtonUp(0))
			{
				increaseButtonPressed = false;
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

			if (Input.GetMouseButtonUp(0))
			{
				decreaseButtonPressed = false;
			}
		}
	}

	private void OnEnable()
	{
		mixer.SetFloat(Config.audioInteractionMixerChannelName, MathHelper.LinearToLogVolume(Config.AudioInteractionVolume));
		volumeSlider.value = Config.AudioInteractionVolume;
	}

	public void TogglePlay()
	{
		if (clip == null)
		{
			StartCoroutine(GetAudioClip(url));
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

	public void Restart()
	{
		if (clip != null)
		{
			audioSource.Stop();
			audioSource.Play();
		}
	}

	IEnumerator GetAudioClip(string urlToLoad)
	{
		var audioType = AudioHelper.AudioTypeFromFilename(urlToLoad);

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

	private void ShowAudioPlayTime()
	{
		currentClipTime = audioSource.time;
		clipTimeText.text = $"{MathHelper.FormatSeconds(currentClipTime)} / {MathHelper.FormatSeconds(fullClipLength)}";
		audioTimeSlider.maxValue = fullClipLength;
		audioTimeSlider.value = currentClipTime;
	}

	public void DecreaseVolume()
	{
		volumeSlider.value -= 0.1f;
	}

	public void IncreaseVolume()
	{
		volumeSlider.value += 0.1f;
	}

	public void VolumeValueChanged()
	{
		Config.AudioInteractionVolume = volumeSlider.value;
		mixer.SetFloat(Config.audioInteractionMixerChannelName, MathHelper.LinearToLogVolume(volumeSlider.value));
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
