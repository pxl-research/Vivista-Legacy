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
	public Sprite icon0;

	private bool muted;
	private bool buttonPressed;
	private bool isDragging;
	private bool volumeChanging;
	private bool hitClicked;
	private float oldAudioValue;
	private IEnumerator coroutineVolumeSlider;

	void Start()
	{
		muted = false;
		isDragging = false;

		slider.handleRect.gameObject.SetActive(false);
		slider.fillRect.gameObject.SetActive(false);
		background.gameObject.SetActive(false);

		coroutineVolumeSlider = ShowSlider(2f);
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
		if (RectTransformUtility.RectangleContainsScreenPoint(icon.GetComponent<RectTransform>(), Input.mousePosition))
		{
			background.gameObject.SetActive(true);
			slider.fillRect.gameObject.SetActive(true);
		}
		else
		{
			if (!isDragging
				&& !RectTransformUtility.RectangleContainsScreenPoint(slider.GetComponent<RectTransform>(), Input.mousePosition)
				&& !buttonPressed)
			{
				background.gameObject.SetActive(false);
				slider.fillRect.gameObject.SetActive(false);
			}

			if (volumeChanging)
			{
				RefreshSliderCoroutine();
			}

			if (hitClicked)
			{
				RefreshSliderCoroutine();
			}

			if (Input.GetMouseButtonUp(0))
			{
				volumeChanging = false;
			}
		}

		SetIconState();
	}

	private void SetIconState()
	{
		if (muted)
		{
			icon.sprite = iconMuted;
		}
		else if (slider.value <= 0.001)
		{
			icon.sprite = icon0;
		}
		else if (slider.value <= 0.33)
		{
			icon.sprite = icon33;
		}
		else if (slider.value <= 0.66)
		{
			icon.sprite = icon66;
		}
		else
		{
			icon.sprite = iconDefault;
		}
	}

	public void OnHit()
	{
		if (muted)
		{
			Mute();
		}
		hitClicked = true;
	}

	public void OnHitUp()
	{
		hitClicked = false;
	}

	public void OnDragSlider()
	{
		isDragging = true;
		muted = false;
	}

	public void OnPointerDownSlider()
	{
		oldAudioValue = -1f;

		if (muted)
		{
			Mute();
		}
	}

	public void OnPointerUpSlider()
	{
		isDragging = false;
	}

	public void Mute()
	{
		if (muted)
		{
			if (oldAudioValue != -1f)
			{
				slider.value = oldAudioValue;
			}
		}
		else
		{
			oldAudioValue = slider.value;
			slider.value = 0;
		}

		muted = !muted;
	}

	public void OnPointerDownVolumeButton()
	{
		if (muted)
		{
			Mute();
		}

		volumeChanging = true;
	}

	public void OnPointerUpVolumeButton()
	{
		volumeChanging = false;
	}

	private void RefreshSliderCoroutine()
	{
		StopCoroutine(coroutineVolumeSlider);
		coroutineVolumeSlider = ShowSlider(2f);
		StartCoroutine(coroutineVolumeSlider);
	}

	private IEnumerator ShowSlider(float delay)
	{
		background.gameObject.SetActive(true);
		slider.fillRect.gameObject.SetActive(true);

		buttonPressed = true;

		yield return new WaitForSeconds(delay);

		buttonPressed = false;
	}
}
