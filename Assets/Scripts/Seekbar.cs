using UnityEngine;
using UnityEngine.EventSystems;

public class Seekbar : MonoBehaviour, IPointerUpHandler
{
	public VideoController controller;
	public RectTransform seekbar;

	public bool seekbarGrowing = false;
	public float minSeekbarHeight = 0.1f;
	public float curSeekbarHeight;
	public float maxSeekbarHeight = 0.25f;
	public float seekbarAnimationDuration = 0.2f;


	public void Start()
	{
		if (!controller){ Debug.LogError(string.Format("Hey you forgot to connect up a VideoController to the Seekbar Script on {0}", this.name)); }

		seekbar = GetComponent<RectTransform>();
		curSeekbarHeight = maxSeekbarHeight;
	}

	public void Update()
	{
		var coords = new Vector3[4];
		seekbar.parent.GetComponent<RectTransform>().GetWorldCorners(coords);

		var mouseInArea = Input.mousePosition.y < coords[1].y;

		if (mouseInArea)
		{
			var newHeight = curSeekbarHeight + ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration));
			curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
			seekbar.anchorMax = new Vector2(seekbar.anchorMax.x, curSeekbarHeight);
		}
		else
		{
			var newHeight = curSeekbarHeight - ((maxSeekbarHeight - minSeekbarHeight) * (Time.deltaTime / seekbarAnimationDuration));
			curSeekbarHeight = Mathf.Clamp(newHeight, minSeekbarHeight, maxSeekbarHeight);
			seekbar.anchorMax = new Vector2(seekbar.anchorMax.x, curSeekbarHeight);
		}
	}

	public void OnPointerUp(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;
		
		var time = pos / max;

		controller.Seek(time);
	}
}
