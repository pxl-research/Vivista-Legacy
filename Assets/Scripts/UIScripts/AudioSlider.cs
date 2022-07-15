using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class AudioSlider : MonoBehaviour
{
	public RectTransform background;
	public Slider slider;
	public Image icon;
	public Sprite iconMuted;
	public Sprite iconDefault;
	public Sprite icon66;
	public Sprite icon33;

	private bool muted;
	private bool volumeSliderHovered;
	private bool isDragging;
	private float oldAudioValue;
	private Coroutine coroutineVolumeSlider;

	void Start()
	{
		slider.handleRect.gameObject.SetActive(false);
		slider.fillRect.gameObject.SetActive(false);
		background.gameObject.SetActive(false);
	}

	private void OnEnable()
	{
		if (coroutineVolumeSlider != null)
		{
			StopCoroutine(coroutineVolumeSlider);
		}

		slider.handleRect.gameObject.SetActive(false);
		slider.fillRect.gameObject.SetActive(false);
		background.gameObject.SetActive(false);
	}

	void Update()
	{
		if (RectTransformUtility.RectangleContainsScreenPoint(icon.GetComponent<RectTransform>(), Input.mousePosition) || isDragging)
		{
			RefreshSliderCoroutine();
		}
		else if (!isDragging
				&& !RectTransformUtility.RectangleContainsScreenPoint(slider.GetComponent<RectTransform>(), Input.mousePosition)
				&& !volumeSliderHovered)
		{
			background.gameObject.SetActive(false);
			slider.fillRect.gameObject.SetActive(false);
		}

		if (muted || slider.value <= 0.001) { icon.sprite = iconMuted; }
		else if (slider.value <= 0.33) { icon.sprite = icon33; }
		else if (slider.value <= 0.66) { icon.sprite = icon66; }
		else { icon.sprite = iconDefault; }
	}

	public void Mute()
	{
		if (muted)
		{
			slider.value = oldAudioValue;
		}
		else
		{
			oldAudioValue = slider.value;
			slider.value = 0;
		}

		muted = !muted;
	}

	public void OnBeginDragSlider()
	{
		oldAudioValue = slider.value;
		isDragging = true;
		muted = false;
	}

	public void OnEndDragSlider()
	{
		isDragging = false;
		if (slider.value > .001f)
		{
			oldAudioValue = slider.value;
		}
		else
		{
			muted = true;
			slider.value = 0;
		}
	}


	//Note(Simon): Happens when pressing down VR volume button
	public void OnPointerDownVolumeButton()
	{
		if (muted)
		{
			Mute();
		}
		
		RefreshSliderCoroutine();
	}

	private void RefreshSliderCoroutine()
	{
		if (coroutineVolumeSlider != null)
		{
			StopCoroutine(coroutineVolumeSlider);
		}

		coroutineVolumeSlider = StartCoroutine(ShowSlider(2f));
	}

	private IEnumerator ShowSlider(float delay)
	{
		background.gameObject.SetActive(true);
		slider.fillRect.gameObject.SetActive(true);

		volumeSliderHovered = true;

		yield return new WaitForSeconds(delay);

		volumeSliderHovered = false;
	}
}
