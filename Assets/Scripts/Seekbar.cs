using UnityEngine;
using UnityEngine.EventSystems;

public class Seekbar : MonoBehaviour, IPointerUpHandler 
{
	public VideoController controller;

	public void Start()
	{
		if (!controller)
		{
			Debug.LogError(string.Format("Hey you forgot to connect up a VideoController to the Seekbar Script on {0}", this.name));
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
