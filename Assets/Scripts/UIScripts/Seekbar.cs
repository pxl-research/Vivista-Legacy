using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class Seekbar : MonoBehaviour, IPointerUpHandler
{
	public GameObject blipPrefab;

	public VideoController videoController;
	public RectTransform seekbarBackground;
	public RectTransform seekbarPreview;
	public RectTransform seekbarCurrent;
	public GameObject compassBackground;
	public GameObject compassForeground;
	public Text timeText;
	public Text blipCounter;
	private bool isEditor;

	public bool hovering = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.33f;
	public float seekbarAnimationDuration = 0.2f;
	public float startRotation;
	public float lastSmoothTime;

	public float timeSinceLastTextUpdate = 0;

	public static GameObject compass;
	public static Seekbar instance;

	private static List<GameObject> activeBlips;
	private static Stack<GameObject> inactiveBlips;

	void Awake()
	{
		compass = compassBackground;
		if (instance != null)
		{
			Debug.LogError("There can only be one seekbar");
		}
		instance = this;
	}
	
	public void Start()
	{
		curSeekbarHeight = maxSeekbarHeight;
		startRotation = 0;
		if (XRSettings.enabled)
		{
			ReattachCompass();
		}

		activeBlips = new List<GameObject>();
		inactiveBlips = new Stack<GameObject>();
		blipCounter = compass.GetComponentInChildren<Text>();
		isEditor = SceneManager.GetActiveScene().name.Equals("Editor");
	}

	public static void ReattachCompass()
	{
		var seekbar = GameObject.Find("Seekbar Canvas").transform;
		if (compass && seekbar)
		{
			compass.transform.SetParent(seekbar);
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

		float smoothedTime = Mathf.Lerp(lastSmoothTime, (float)videoController.currentFractionalTime, 0.5f);

		curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
		seekbarCurrent.anchorMax = new Vector2(smoothedTime, seekbarCurrent.anchorMax.y);
		seekbarBackground.anchorMax = new Vector2(seekbarBackground.anchorMax.x, curSeekbarHeight);
		seekbarPreview.anchorMax = new Vector2(onSeekbar ? timeAtMouse : 0, seekbarPreview.anchorMax.y);

		if (onSeekbar)
		{
			timeText.text = $" {MathHelper.FormatSeconds(videoController.TimeForFraction(timeAtMouse))} / {MathHelper.FormatSeconds(videoController.videoLength)}";
		}
		else if (timeSinceLastTextUpdate > 0.5)
		{
			timeText.text = $" {MathHelper.FormatSeconds(videoController.rawCurrentTime)} / {MathHelper.FormatSeconds(videoController.videoLength)}";
			timeSinceLastTextUpdate = 0;
		}

		// TODO(Lander): Actually make use of the start position, and no hardcoded values
		float rotation = (XRSettings.enabled ? compass.transform.parent.eulerAngles.y : Camera.main.transform.rotation.eulerAngles.y) - startRotation;
		if (!isEditor && compass.transform.parent != Canvass.seekbar)
		{
			rotation -= 90;
		}

		compassForeground.transform.localEulerAngles = new Vector3(0, 0, -(rotation));

		timeSinceLastTextUpdate += Time.deltaTime;
		lastSmoothTime = smoothedTime;
	}

	public void OnPointerUp(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;

		var fractionalTime = pos / max;

		videoController.SeekFractional(fractionalTime);
	}

	public GameObject CreateBlip()
	{
		var blip = Instantiate(blipPrefab);
		blip.transform.SetParent(compass.transform);
		blip.transform.localEulerAngles = Vector3.zero;
		blip.transform.localScale = Vector3.one;
		blip.transform.localPosition = Vector3.zero;
		blip.transform.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
		return blip;
	}

	public static void ClearBlips()
	{
		while (activeBlips.Count > 0)
		{
			var inactive = activeBlips[activeBlips.Count - 1];
			activeBlips.RemoveAt(activeBlips.Count - 1);
			inactiveBlips.Push(inactive);
			inactive.SetActive(false);
		}
	}

	public void RenderBlips(List<InteractionPointPlayer> interactionPoints, Controller left, Controller right)
	{
		float forwardAngle;
		//Note(lander): Compass rotation
		{
			if (left || right)
			{
				forwardAngle = right.compassAttached
					? right.transform.eulerAngles.y
					: left.transform.eulerAngles.y;
			}
			else
			{
				forwardAngle = compass.transform.parent.localEulerAngles.y;
			}
		}

		var activePoints = new List<InteractionPointPlayer>();

		//NOTE(Simon): Count active points
		for (int i = 0; i < interactionPoints.Count; i++)
		{
			var point = interactionPoints[i];

			if (point.point.activeSelf && !point.isSeen)
			{
				activePoints.Add(point);
			}
		}

		//NOTE(Simon): Update counter text
		blipCounter.text = activeBlips.Count != 0
			? activeBlips.Count.ToString()
			: "";

		//NOTE(Simon): Update blips, and active/create new blips when necessary
		for (int i = 0; i < activePoints.Count; i++)
		{
			var point = activePoints[i];

			if (activeBlips.Count < activePoints.Count)
			{
				if (inactiveBlips.Count == 0)
				{
					inactiveBlips.Push(CreateBlip());
				}

				var blip = inactiveBlips.Pop();
				blip.SetActive(true);
				activeBlips.Add(blip);
			}

			float blipAngle = point.point.transform.eulerAngles.y;
			// TODO(Lander): Rely on a start position of a video instead
			float angle = (XRSettings.enabled ? forwardAngle : 90) - blipAngle;
			activeBlips[i].transform.localEulerAngles = new Vector3(0, 0, angle);
		}

		//NOTE(Simon): Deactivate unneeded blips
		while (activeBlips.Count > activePoints.Count)
		{
			var inactive = activeBlips[activeBlips.Count - 1];
			activeBlips.RemoveAt(activeBlips.Count - 1);
			inactive.SetActive(false);
			inactiveBlips.Push(inactive);
		}
	}
}
