using System;
using UnityEngine;
using UnityEngine.UI;

public class ImagePanelImage : MonoBehaviour
{
	public string url;

	private WWW www;
	private bool downloading;
	private bool loaded = false;
	private static Vector2 defaultImageSize = new Vector2(500, 500);

	public void SetURL(string URL)
	{
		url = URL;
	}

	public void LoadImage()
	{
		if (!String.IsNullOrEmpty(url) && !loaded)
		{
			if (!url.StartsWith("file:///"))
			{
				url = "file:///" + url;
			}

			www = new WWW(url);
			downloading = true;
		}
	}

	public void Update()
	{
		if (downloading && www.isDone)
		{
			loaded = true;
			downloading = false;
			var texture = www.texture;
			var image = GetComponentInChildren<RawImage>();
			var size = defaultImageSize;
			Vector2 position;
			var ratio = (float)texture.height / texture.width;
			if (ratio >= 1)
			{
				size.x /= ratio;
				position = new Vector2((defaultImageSize.x - size.x) / 2, 0);
			}
			else
			{
				size.y *= ratio;
				//NOTE(Simon): Negative because of Unity UI Layout reasons
				position = new Vector2(0, -(defaultImageSize.y - size.y) / 2);
			}

			image.rectTransform.sizeDelta = size;
			image.rectTransform.anchoredPosition = position;

			image.texture = texture;
		}
	}
}
