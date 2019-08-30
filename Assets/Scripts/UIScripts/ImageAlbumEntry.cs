using System;
using UnityEngine;
using UnityEngine.UI;

public class ImageAlbumEntry : MonoBehaviour
{
	public RawImage preview;
	public Text filename;
	public Button2 moveLeftButton;
	public Button2 moveRightButton;
	public Button2 deleteButton;
	public string url;

	private WWW www;
	private bool downloading;
	private static Vector2 defaultImageSize = new Vector2(200, 200);

	public void SetURL(string newUrl)
	{
		if (!String.IsNullOrEmpty(newUrl))
		{
			url = newUrl;
			if (!newUrl.StartsWith("http://") && !newUrl.StartsWith("https://"))
			{
				newUrl = "file:///" + newUrl;
			}

			filename.text = newUrl.Substring(newUrl.LastIndexOf("\\", StringComparison.Ordinal) + 1);

			www = new WWW(newUrl);
			downloading = true;
		}
	}

	public void Update()
	{
		if (downloading && www.isDone)
		{
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

			preview.texture = texture;
		}
	}
}
