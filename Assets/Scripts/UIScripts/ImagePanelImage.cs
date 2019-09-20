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

			float heightRatio = texture.height / defaultImageSize.y;
			float widthRatio = texture.width / defaultImageSize.x;
			float biggestRatio = Mathf.Max(heightRatio, widthRatio);

			var newSize = new Vector2(texture.width / biggestRatio, texture.height / biggestRatio);

			image.rectTransform.sizeDelta = newSize;
			image.rectTransform.localPosition = new Vector2(-(defaultImageSize.x - newSize.x), -(defaultImageSize.y - newSize.y) / 2);

			image.texture = texture;
		}
	}

	public void SetMaxSize(Vector2 size)
	{
		defaultImageSize = size;
		GetComponent<RectTransform>().sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.localPosition = Vector2.zero;
	}
}
