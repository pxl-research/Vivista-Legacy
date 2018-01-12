using UnityEngine;
using UnityEngine.UI;

public class ImagePanel : MonoBehaviour
{
	public Text title;
	public RawImage image;
	public string imageURL;
	public Canvas canvas;
	public GameObject interactionPoint;
	
	private bool downloading = false;
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

			//NOTE(Simon): Title + Triangle + bottomMargin
			const float extraHeight = 40 + 16 + 10;
			//NOTE(Simon): LeftMargin + RightMargin;
			const float extraWidth = 10 + 10;
			
			canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(width + extraWidth, height + extraHeight);
			downloading = false;
		}

		canvas.transform.rotation = Camera.main.transform.rotation;
	}

	public void OnEnable ()
	{
		if (neverOpened)
		{
			www = new WWW(imageURL);
			neverOpened = true;
			downloading = true;
		}
	}
}
