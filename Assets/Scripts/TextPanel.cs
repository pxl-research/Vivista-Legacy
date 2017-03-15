using UnityEngine;
using UnityEngine.UI;

public class TextPanel : MonoBehaviour 
{
	public Text title;
	public Text body;
	public Canvas canvas;
	public GameObject interactionPoint;

	public void Init(GameObject interactionPoint, string title, string body)
	{
		this.title.text = title;
		this.body.text = body;

		this.canvas = GetComponent<Canvas>();
		this.interactionPoint = interactionPoint;

		var newPos = interactionPoint.transform.position;

		if (!Camera.main.orthographic)
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.3f);
			newPos.y += 0.01f;
		}
		else
		{
			newPos = Vector3.Lerp(newPos, Camera.main.transform.position, 0.001f);
			newPos.y += 0.015f;
		}
		canvas.GetComponent<RectTransform>().position = newPos;

	}
	
	public void Update()
	{
		this.canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
