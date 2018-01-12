using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour
{
	public Text title;
	public RawImage image;
	public string imageURL;
	public Canvas canvas;
	public GameObject interactionPoint;
	
	private bool downloading;
	private bool neverOpened;
	private WWW www;

	public void Init(Vector3 position, string newTitle, string newImageURL)
	{
		title.text = newTitle;
		imageURL = newImageURL;
		Move(position);
		neverOpened = true;
	}

	public void Move(Vector3 position)
	{
		Vector3 newPos;

		if (!Camera.main.orthographic)
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.3f);
			newPos.y += 0.01f;
		}
		else
		{
			newPos = Vector3.Lerp(position, Camera.main.transform.position, 0.001f);
			newPos.y += 0.015f;
		}

		canvas.GetComponent<RectTransform>().position = newPos;
		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	public void Update()
	{
		if (downloading && www.isDone)
		{
			var texture = www.texture;
			image.texture = texture;

			float newWidth = image.rectTransform.rect.width;
			float newHeight = image.rectTransform.rect.height;
			float imageRatio = newWidth / newHeight;

			if (imageRatio <= 1)
			{
				float width = image.rectTransform.rect.width;
				float ratio = texture.width / width;
				newHeight = texture.height / ratio;
			}
			else
			{
				float height = image.rectTransform.rect.height;
				float ratio = texture.height / height;
				newWidth = texture.width / ratio;
			}

			

			//NOTE(Simon): Title + Triangle + bottomMargin
			const float extraHeight = 40 + 16 + 10;
			//NOTE(Simon): LeftMargin + RightMargin;
			const float extraWidth = 10 + 10;
			
			canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth + extraWidth, newHeight + extraHeight);
			downloading = false;
		}

		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	public void OnEnable ()
	{
		if (neverOpened)
		{
			www = new WWW(imageURL);
			neverOpened = false;
			downloading = true;
		}
	}
}
