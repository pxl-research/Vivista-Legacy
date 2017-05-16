using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.VR;

public class Seekbar : MonoBehaviour, IPointerDownHandler
{
	public VideoController controller;
	public RectTransform seekbar;
	public GameObject compassBackground;
	public GameObject compassForeground;

	public bool hovering = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.25f;
	public float seekbarAnimationDuration = 0.2f;
	public float startRotation;

	public void Start()
	{
		seekbar = GetComponent<RectTransform>();
		curSeekbarHeight = maxSeekbarHeight;

		compassBackground = GameObject.Find("CompassBackground");
		compassForeground = GameObject.Find("CompassForeground");
		startRotation = 0;
	}

	public void Update()
	{
		var coords = new Vector3[4];
		seekbar.parent.GetComponent<RectTransform>().GetWorldCorners(coords);

		hovering = Input.mousePosition.y < coords[1].y;

		var newHeight = hovering
			? curSeekbarHeight + ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration))
			: curSeekbarHeight - ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration));

		curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
		seekbar.anchorMax = new Vector2(seekbar.anchorMax.x, curSeekbarHeight);

		//TODO(Simon): Verify that this works.
		if (!VRSettings.enabled)
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

		var time = pos / max;

		controller.Seek(time);
	}
}
