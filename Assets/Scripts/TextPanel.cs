using UnityEngine;
using UnityEngine.UI;

public class TextPanel : MonoBehaviour 
{
	public Text title;
	public Text body;
	public RectTransform panel;
	public Canvas canvas;

	public void Init(GameObject newInteractionPoint, string newTitle, string newBody)
	{
		title.text = newTitle;
		body.text = newBody;

		canvas = GetComponent<Canvas>();

		var newPos = newInteractionPoint.transform.position;

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

		var width = Mathf.Max(200, newBody.Length / 2f);
		var height = Mathf.Max(200, newBody.Length / 3f);

		var canvasTransform = canvas.GetComponent<RectTransform>();
		canvasTransform.position = newPos;
		canvasTransform.sizeDelta = new Vector2(width, height);
	}
	
	public void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
