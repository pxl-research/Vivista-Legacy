using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

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

	public void Init(string newTitle, string newImageURL, bool loadImageImmediately)
	{
		title.text = newTitle;
		imageURL = newImageURL;
		if (loadImageImmediately)
		{
			www = new WWW(imageURL);
			neverOpened = false;
			downloading = true;
		}
		else
		{
			neverOpened = true;
		}
	}

	public void Move(Vector3 position)
	{
		var newPos = position;
		newPos.y += 0.015f;
		canvas.GetComponent<RectTransform>().position = position;
	}

	public void Start()
	{
		//NOTE(Kristof): Initial rotation towards the camera
		canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y);
		if (!XRSettings.enabled)
		{
			canvas.transform.localScale = new Vector3(0.2f, 0.2f, 0.2f);
		}
	}

	public void Update()
	{
		if (downloading && www.isDone)
		{
			var texture = www.texture;
			image.texture = texture;

			//NOTE(Simon): Title + Triangle + bottomMargin
			const float extraHeight = 40 + 16 + 10;
			//NOTE(Simon): LeftMargin + RightMargin;
			const float extraWidth = 10 + 10;

			float newWidth = (Screen.width / 2f);
			float newHeight = (Screen.height / 2f);
			float imageRatio = newWidth / newHeight;

			//NOTE(Simon): Portrait
			if (imageRatio <= 1)
			{
				float ratio = (texture.width + extraWidth) / newWidth;
				newHeight = (texture.height + extraHeight) / ratio;
			}
			//NOTE(Simon): Landscape
			else
			{
				float ratio = (texture.height + extraHeight) / newHeight;
				newWidth = (texture.width + extraWidth) / ratio;
			}

			canvas.GetComponent<RectTransform>().sizeDelta = new Vector2(newWidth, newHeight);
			downloading = false;
		}

		// NOTE(Kristof): Turning every frame only needs to happen in Editor
		if (SceneManager.GetActiveScene().Equals(SceneManager.GetSceneByName("Editor")))
		{
			canvas.transform.eulerAngles = new Vector3(Camera.main.transform.eulerAngles.x, Camera.main.transform.eulerAngles.y, Camera.main.transform.eulerAngles.z);
		}

	}

	public void OnEnable()
	{
		if (neverOpened)
		{
			www = new WWW(imageURL);
			neverOpened = false;
			downloading = true;
		}
	}
}
