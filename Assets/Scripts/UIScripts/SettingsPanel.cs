using UnityEngine;
using UnityEngine.UI;

public class SettingsPanel : MonoBehaviour
{
	public Slider mainVideoVolumeSlider;
	public Slider videoInteractionVolumeSlider;
	public Slider audioInteractionVolumeSlider;
	public Toggle showOnlyCurrentInteractionsToggle;
	public Toggle invertMouseVerticalToggle;
	public Toggle invertMouseHorizontalToggle;

	public void Start()
	{
		mainVideoVolumeSlider.SetValueWithoutNotify(Config.MainVideoVolume);
		audioInteractionVolumeSlider.SetValueWithoutNotify(Config.AudioInteractionVolume);
		videoInteractionVolumeSlider.SetValueWithoutNotify(Config.VideoInteractionVolume);

		showOnlyCurrentInteractionsToggle.SetIsOnWithoutNotify(Config.ShowOnlyCurrentInteractions);
		invertMouseVerticalToggle.SetIsOnWithoutNotify(Config.InvertMouseVertical);
		invertMouseHorizontalToggle.SetIsOnWithoutNotify(Config.InvertMouseHorizontal);
	}

	public void Update()
	{
		if (mainVideoVolumeSlider.value != Config.MainVideoVolume)
		{
			mainVideoVolumeSlider.SetValueWithoutNotify(Config.MainVideoVolume);
		}
		if (audioInteractionVolumeSlider.value != Config.AudioInteractionVolume)
		{
			audioInteractionVolumeSlider.SetValueWithoutNotify(Config.AudioInteractionVolume);
		}
		if (videoInteractionVolumeSlider.value != Config.VideoInteractionVolume)
		{
			videoInteractionVolumeSlider.SetValueWithoutNotify(Config.VideoInteractionVolume);
		}
	}

	public void UpdateMainVideoVolume(float value)
	{
		Config.MainVideoVolume = (value);
	}

	public void UpdateVideoInteractionVolume(float value)
	{
		Config.VideoInteractionVolume = (value);
	}

	public void UpdateAudioInteractionVolume(float value)
	{
		Config.AudioInteractionVolume = (value);
	}

	public void UpdateShowOnlyCurrentInteractions(bool value)
	{
		Config.ShowOnlyCurrentInteractions = value;
	}

	public void UpdateInvertMouseVertical(bool value)
	{
		Config.InvertMouseVertical = value;
	}

	public void UpdateInvertMouseHorizontal(bool value)
	{
		Config.InvertMouseHorizontal = value;
	}

	public void Close()
	{
		Destroy(gameObject);
	}
}
