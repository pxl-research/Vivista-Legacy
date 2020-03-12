using System.Collections;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

public class MultipleChoiceAreaEntry : MonoBehaviour
{
	public RawImage preview;
	public Text numPoints;
	public Button deleteButton;
	public Button editButton;
	public Toggle toggle;
	public string miniatureUrl;
	public Area area;

	private bool initialized;
	private static Vector2 defaultImageSize = new Vector2(200, 200);


	public IEnumerator SetArea(Area NewArea, string newMiniatureUrl, bool hideButtons = false)
	{
		if (initialized) { yield break; }
		miniatureUrl = newMiniatureUrl;
		area = NewArea;
		numPoints.text = NewArea.vertices.Count + " points";

		Texture2D texture;

		using (var request = UnityWebRequestTexture.GetTexture("file:///" + miniatureUrl))
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
			deleteButton.transform.parent.gameObject.SetActive(false);
		}
		initialized = true;
	}
}
