using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class Seekbar : MonoBehaviour, IPointerUpHandler
{

	public VideoController controller;
	public RectTransform seekbarBackground;
	public RectTransform seekbarPreview;
	public RectTransform seekbarCurrent;
	public GameObject compassBackground;
	public GameObject compassForeground;
	public Text timeText;

	public bool hovering = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.33f;
	public float seekbarAnimationDuration = 0.2f;
	public float startRotation;
	public float lastSmoothTime;

	public float timeSinceLastTextUpdate = 0;

	public static GameObject compass;

	void Awake()
	{
		compass = compassBackground;
	}
	public void Start()
	{
		curSeekbarHeight = maxSeekbarHeight;
		startRotation = 0;
		if (XRSettings.enabled)
		{
			ReattachCompass();
		}
	}

	public static void ReattachCompass()
	{
		// TODO(Lander): This is not tested enough
		var seekbar = GameObject.Find("Seekbar Canvas").transform;
		if (compass && seekbar)
		{
			compass.transform.parent = seekbar;
			compass.transform.localScale = new Vector3(0.5f, 0.5f, 0);
			compass.transform.localPosition = Vector3.zero;
			compass.transform.localEulerAngles = Vector3.zero;
			compass.transform.GetComponent<RectTransform>().anchoredPosition = new Vector3(-16, 16, 0);
		}
	}

	public void Update()
	{
		Vector2 mousePos = Input.mousePosition;
		float maxMousePos = GetComponent<RectTransform>().rect.width;
		float timeAtMouse = mousePos.x / maxMousePos;

		hovering = RectTransformUtility.RectangleContainsScreenPoint(seekbarBackground.parent.GetComponent<RectTransform>(), mousePos);
		bool onSeekbar = RectTransformUtility.RectangleContainsScreenPoint(seekbarBackground, mousePos);

		float newHeight = hovering
			? curSeekbarHeight + ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration))
			: curSeekbarHeight - ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration));

		float smoothedTime = Mathf.Lerp(lastSmoothTime, (float)controller.currentFractionalTime, 0.5f);

		curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
		seekbarCurrent.anchorMax = new Vector2(smoothedTime, seekbarCurrent.anchorMax.y);
		seekbarBackground.anchorMax = new Vector2(seekbarBackground.anchorMax.x, curSeekbarHeight);
		seekbarPreview.anchorMax = new Vector2(onSeekbar ? timeAtMouse : 0, seekbarPreview.anchorMax.y);

		if (onSeekbar)
		{
			timeText.text = $" {MathHelper.FormatSeconds(controller.TimeForFraction(timeAtMouse))} / {MathHelper.FormatSeconds(controller.videoLength)}";
		}
		else if (timeSinceLastTextUpdate > 0.5)
		{
			timeText.text = $" {MathHelper.FormatSeconds(controller.rawCurrentTime)} / {MathHelper.FormatSeconds(controller.videoLength)}";
			timeSinceLastTextUpdate = 0;
		}

		// TODO(Lander): Actually make use of the start position, and no hardcoded values
		float rotation = (XRSettings.enabled ? compass.transform.parent.eulerAngles.y : Camera.main.transform.rotation.eulerAngles.y) - startRotation;
		if (SceneManager.GetActiveScene().name.Equals("Player") && compass.transform.parent != Canvass.seekbar)
		{
			rotation -= 90;
		}

		compassForeground.transform.localEulerAngles = new Vector3(0, 0, -(rotation));

		timeSinceLastTextUpdate += Time.deltaTime;
		lastSmoothTime = float.IsNaN(smoothedTime) ? 0 : smoothedTime;
	}

	public void OnPointerUp(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;

		var fractionalTime = pos / max;

		controller.Seek(fractionalTime);
	}

	public static GameObject CreateBlip(float rotation, GameObject blip)
	{
		blip.transform.SetParent(compass.transform);
		blip.transform.localEulerAngles = new Vector3(0, 0, rotation);
		blip.transform.localScale = Vector3.one;
		blip.transform.localPosition = Vector3.zero;
		blip.transform.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
		return blip;
	}
}
