using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour 
{
	public Text title;
	public RawImage image;
	public Canvas canvas;
	public GameObject interactionPoint;
	
	private bool downloading = false;
	private WWW www;

	public void Init(Vector3 position, string newTitle, string newImageURL)
	{
		title.text = newTitle;
		www = new WWW(newImageURL);
		downloading = true;

		var newPos = position;

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
		if (downloading && www.isDone)
		{
			var texture = www.texture;
			image.texture = texture;
			var width = image.rectTransform.rect.width;
			var ratio = texture.width / width;
			var height = texture.height / ratio;
			//image.rectTransform.sizeDelta = new Vector2(width, height);

			//NOTE(Simon): Title + Triangle + bottomMargin
			const float extraHeight = 40 + 16 + 10;
			//NOTE(Simon): LeftMargin + RightMargin;
			const float extraWidth = 10 + 10;
			/*
			var textureRatio = texture.width / (float)texture.height;

			canvasTransform.sizeDelta = textureRatio > 1 
				? new Vector2(300 * textureRatio + extraWidth, 300 + extraHeight) 
				: new Vector2(300 + extraWidth, 300 * (1 / textureRatio) + extraHeight);
			*/
			canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width + extraWidth, height + extraHeight);
			downloading = false;
		}

		canvas.transform.rotation = Camera.main.transform.rotation;
	}
}
