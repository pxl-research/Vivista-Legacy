using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MultipleChoiceImageEntry : MonoBehaviour
{
	public RawImage preview;
	public Text path;
	public Button deleteButton;
	public Toggle toggle;
	public string imageUrl;

	private bool initialized;
	private static Vector2 defaultImageSize = new Vector2(200, 200);

	public IEnumerator SetUrl(string newImageUrl, bool hideButtons = false)
	{
		if (initialized) { yield break; }
		imageUrl = newImageUrl;

		path.text = imageUrl;

		Texture2D texture;

		using (var request = UnityWebRequestTexture.GetTexture("file://" + imageUrl))
		{
			yield return request.SendWebRequest();

			texture = DownloadHandlerTexture.GetContent(request);
		}

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

		if (hideButtons)
		{
			deleteButton.gameObject.SetActive(false);
			toggle.interactable = false;
		}
		initialized = true;
	}
}
