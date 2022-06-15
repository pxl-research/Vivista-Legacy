using System.Collections;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.Networking;
using UnityEngine.UI;
using UnityEngine.XR;
using UnityEngine.XR.Management;

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

	private Controller controllerLeft;
	private Controller controllerRight;

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

		if (XRSettings.isDeviceActive)
		{
			controllerLeft = GameObject.Find("LeftHand").GetComponentInChildren<Controller>();
			controllerRight = GameObject.Find("RightHand").GetComponentInChildren<Controller>();
		}

		volumeSlider.onValueChanged.AddListener(_ => VolumeValueChanged());
		volumeSlider.SetValueWithoutNotify(Config.AudioInteractionVolume);
		mixer.SetFloat(Config.audioInteractionMixerChannelName, MathHelper.LinearToLogVolume(Config.AudioInteractionVolume));
	}

	void Update()
	{
		CheckButtonStates();

		playButtonImage.texture = audioSource.isPlaying ? iconPause : iconPlay;
		ShowAudioPlayTime();

		if (volumeSlider.value != Config.AudioInteractionVolume)
		{
			volumeSlider.SetValueWithoutNotify(Config.AudioInteractionVolume);
			mixer.SetFloat(Config.audioInteractionMixerChannelName, MathHelper.LinearToLogVolume(volumeSlider.value));
		}
	}
	
	private void CheckButtonStates()
	{
		if (XRSettings.isDeviceActive
			&& controllerLeft != null && controllerRight != null 
			&& !(controllerLeft.triggerDown || controllerRight.triggerDown) && (increaseButtonPressed || decreaseButtonPressed))
		{
			increaseButtonPressed = false;
			decreaseButtonPressed = false;
			increaseVolumeButton.enabled = false;
			decreaseVolumeButton.enabled = false;
		}

		if (increaseButtonPressed)
		{
			//NOTE(Simon): When button is down, immediately change volume
			if (!volumeChanging)
			{
				IncreaseVolume();
				volumeChanging = true;
			}
	
			//NOTE(Simon): Every {time interval} change volume
			if (Time.time > volumeButtonClickTime + 0.5)
			{
				volumeChanging = false;
				volumeButtonClickTime = Time.time;
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
			if (Time.time > volumeButtonClickTime + 0.5)
			{
				volumeChanging = false;
				volumeButtonClickTime = Time.time;
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
			yield return www.SendWebRequest();

			if (www.result != UnityWebRequest.Result.Success)
			{
				Debug.LogError(www.error);
				yield break;
			}

			clip = DownloadHandlerAudioClip.GetContent(www);
			clip.LoadAudioData();
			audioSource.clip = clip;
			ShowAudioPlayTime();

			fullClipLength = clip.length;
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
