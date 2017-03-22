using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour 
{
	public Text title;
	public RawImage image;
	public Canvas canvas;
	public GameObject interactionPoint;

	public void Init(GameObject newInteractionPoint, string newTitle, string newImageURL)
	{
		title.text = newTitle;

		var data = File.ReadAllBytes(newImageURL);
		var texture = new Texture2D(0, 0);
		texture.LoadImage(data);
		
		image.texture = texture;
		canvas = GetComponent<Canvas>();
		interactionPoint = newInteractionPoint;

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

		var canvasTransform = canvas.GetComponent<RectTransform>();
		canvasTransform.position = newPos;

		//NOTE(Simon): Title + Triangle + bottomMargin
		const float extraHeight = 40 + 16 + 10;
		//NOTE(Simon): LeftMargin + RightMargin;
		const float extraWidth = 10 + 10;

		var ratio = texture.width / (float)texture.height;

		canvasTransform.sizeDelta = ratio > 1 
			? new Vector2(300 * ratio + extraWidth, 300 + extraHeight) 
			: new Vector2(300 + extraWidth, 300 * (1 / ratio) + extraHeight);
	}

	public void Update()
	{
		canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
