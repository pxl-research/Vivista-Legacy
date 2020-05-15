using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class ImagePanelImage : MonoBehaviour
{
	public string url;

	private UnityWebRequest www;
	private bool loaded = false;
	private static Vector2 defaultImageSize = new Vector2(500, 500);

	public void SetURL(string URL)
	{
		url = URL;
	}

	public IEnumerator LoadImage()
	{
		if (!String.IsNullOrEmpty(url) && !loaded)
		{
			if (!url.StartsWith("file:///"))
			{
				url = "file:///" + url;
			}

			Texture2D texture;

			using (www = UnityWebRequestTexture.GetTexture(url))
			{
				yield return www.SendWebRequest();

				loaded = true;
				texture = DownloadHandlerTexture.GetContent(www);
			}

			var image = GetComponentInChildren<RawImage>();
			var newSize = MathHelper.ScaleRatio(new Vector2(texture.width, texture.height), defaultImageSize);

			image.rectTransform.sizeDelta = newSize;
			image.texture = texture;
		}
	}

	public void SetMaxSize(Vector2 size)
	{
		defaultImageSize = size;
		GetComponent<RectTransform>().sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.sizeDelta = defaultImageSize;
		GetComponentInChildren<RawImage>().rectTransform.anchoredPosition = Vector2.zero;
	}
}
