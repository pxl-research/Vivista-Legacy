using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using UnityEngine.XR;

public class Seekbar : MonoBehaviour, IPointerDownHandler
{

	public VideoController controller;
	public RectTransform seekbarBackground;
	public RectTransform seekbar;
	public GameObject compassBackground;
	public GameObject compassForeground;
	public GameObject compassBlip;
	public Text timeText;

	public bool hovering = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.25f;
	public float seekbarAnimationDuration = 0.2f;
	public float startRotation;
	public double lastSmoothTime;

	public static GameObject compass;

	public void Start()
	{
		curSeekbarHeight = maxSeekbarHeight;
		startRotation = 0;
		if (UnityEngine.XR.XRSettings.enabled)
			ReattachCompass();
		compass = compassBackground;
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
		var infoPanelCoords = new Vector3[4];
		var seekbarCoords = new Vector3[4];
		seekbarBackground.parent.GetComponent<RectTransform>().GetWorldCorners(infoPanelCoords);
		seekbarBackground.GetComponent<RectTransform>().GetWorldCorners(seekbarCoords);

		hovering = Input.mousePosition.y < infoPanelCoords[1].y;
		var onSeekbar = Input.mousePosition.y > seekbarCoords[0].y && Input.mousePosition.y < seekbarCoords[1].y;

		var newHeight = hovering
			? curSeekbarHeight + ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration))
			: curSeekbarHeight - ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration));

		var smoothedTime = Mathf.Lerp((float)lastSmoothTime, (float)controller.currentFractionalTime, 0.33f);

		curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
		seekbar.anchorMax = new Vector2(smoothedTime, seekbar.anchorMax.y);
		seekbarBackground.anchorMax = new Vector2(seekbarBackground.anchorMax.x, curSeekbarHeight);

		lastSmoothTime = float.IsNaN(smoothedTime) ? 0 : smoothedTime;

		if (onSeekbar)
		{
			var pos = Input.mousePosition.x;
			var max = GetComponent<RectTransform>().rect.width;

			var time = pos / max;

			timeText.text = String.Format(" {0} / {1}", MathHelper.FormatSeconds(controller.TimeForFraction(time)), MathHelper.FormatSeconds(controller.videoLength));
		}
		else
		{
			//TODO(Simon): Maybe make this run less often because it generates garbage
			timeText.text = String.Format(" {0} / {1}", MathHelper.FormatSeconds(controller.rawCurrentTime), MathHelper.FormatSeconds(controller.videoLength));
		}

		// TODO(Lander): Actually make use of the start position, and no hardcoded values
		var rotation = (XRSettings.enabled ? compass.transform.parent.eulerAngles.y : Camera.main.transform.rotation.eulerAngles.y) - startRotation;
		if (SceneManager.GetActiveScene().name.Equals("Player") && compass.transform.parent != Canvass.seekbar) rotation -= 90;

		compassForeground.transform.localEulerAngles = new Vector3(0, 0, -(rotation));

	}

	public void OnPointerDown(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;

		var fractionalTime = pos / max;

		controller.Seek(fractionalTime);
	}

	public static GameObject CreateBlip(float rotation, GameObject blip)
	{
		blip.transform.SetParent(GameObject.Find("CompassBackground").transform);
		blip.transform.localEulerAngles = new Vector3(0, 0, rotation);
		blip.transform.localScale = Vector3.one;
		blip.transform.localPosition = Vector3.zero;
		blip.transform.GetComponent<RectTransform>().anchoredPosition = Vector3.zero;
		return blip;
	}
}
