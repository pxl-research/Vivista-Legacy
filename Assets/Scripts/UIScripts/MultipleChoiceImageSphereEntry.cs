using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MultipleChoiceImageSphereEntry : MonoBehaviour
{
	public RawImage preview;
	public Button button;

	private bool initialized;
	private static Vector2 defaultImageSize = new Vector2(300, 300);

	public IEnumerator SetUrl(string newImageUrl)
	{
		if (initialized) { yield break; }

		Texture2D texture;

		using (var request = UnityWebRequestTexture.GetTexture("file:///" + newImageUrl))
		{
			yield return request.SendWebRequest();

			texture = DownloadHandlerTexture.GetContent(request);
		}

		var image = GetComponentInChildren<RawImage>();

		var maxSize = GetComponent<RectTransform>().sizeDelta;
		var size = maxSize;
		Vector2 position;
		float ratio = (float)texture.height / texture.width;

		if (ratio >= 1)
		{
			size.x /= ratio;
			position = new Vector2((maxSize.x - size.x) / 2, 0);
		}
		else
		{
			size.y *= ratio;
			position = new Vector2(0, -(maxSize.y - size.y) / 2);
		}

		image.rectTransform.sizeDelta = size;
		image.rectTransform.anchoredPosition = position;
		preview.texture = texture;

		initialized = true;
	}
}
