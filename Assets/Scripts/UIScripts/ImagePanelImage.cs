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
