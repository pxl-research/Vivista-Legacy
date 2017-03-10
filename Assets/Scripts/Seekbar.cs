using UnityEngine;
using UnityEngine.EventSystems;

public class Seekbar : MonoBehaviour, IPointerUpHandler 
{
	public VideoController controller;

	public void OnPointerUp(PointerEventData e)
	{
		var pos = e.pressPosition.x;
		var max = GetComponent<RectTransform>().rect.width;
		
		var time = pos / max;

		controller.Seek(time);
	}
}
