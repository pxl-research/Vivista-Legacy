using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class Seekbar : MonoBehaviour, IPointerDownHandler
{
	public VideoController controller;
	public RectTransform seekbarBackground;
	public RectTransform seekbar;
	public GameObject compassBackground;
	public GameObject compassForeground;
	public Text timeText;

	public bool hovering = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.25f;
	public float seekbarAnimationDuration = 0.2f;
	public float startRotation;
	public double lastSmoothTime;

	public void Start()
	{
		curSeekbarHeight = maxSeekbarHeight;
		startRotation = 0;
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
			timeText.text = String.Format(" {0} / {1}", MathHelper.FormatSeconds(controller.currentTime), MathHelper.FormatSeconds(controller.videoLength));
		}

		//TODO(Simon): Verify that this works.
		if (!UnityEngine.XR.XRSettings.enabled)
		{
			compassForeground.SetActive(true);
			compassBackground.SetActive(true);

			var rotation = Camera.main.transform.rotation.eulerAngles.y - startRotation;
			compassForeground.transform.rotation = Quaternion.Euler(0, 0, -rotation);
		}
		else
		{
			compassForeground.SetActive(false);
			compassBackground.SetActive(false);
		}
	}

	public void OnPointerDown(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;

		var fractionalTime = pos / max;

		controller.Seek(fractionalTime);
	}
}
