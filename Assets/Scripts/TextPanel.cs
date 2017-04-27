using UnityEngine;
using UnityEngine.UI;

public class TextPanel : MonoBehaviour 
{
	public Text title;
	public Text body;
	public RectTransform panel;
	public Canvas canvas;

	public void Init(Vector3 position, string newTitle, string newBody)
	{
		title.text = newTitle;
		body.text = newBody;

		canvas = GetComponent<Canvas>();
		Move(position);
		
		var width = Mathf.Max(150, newBody.Length);
		var height = Mathf.Max(100, newBody.Length / 2);
		

		canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width, height);

	}

	public void Move(Vector3 position)
	{
		Vector3 newPos;

		if (!Camera.main.orthographic)
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.3f);
			newPos.y += 0.01f;
		}
		else
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
			newPos.y += 0.015f;
		}
	
		canvas.GetComponent<RectTransform>().position = newPos;
	}
	
	public void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
