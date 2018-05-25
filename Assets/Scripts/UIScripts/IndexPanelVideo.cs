using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class IndexPanelVideo : MonoBehaviour
{
	public GameObject error;

	public Text titleText;
	public Text authorText;
	public Text sizeText;
	public Text lengthText;
	public Text timestampText;
	public Text DownloadedText;
	public Image thumbnailImage;

	private WWW imageDownload;
	private string uuid;

	private bool isLocal;

	public void SetData(VideoSerialize video, bool local)
	{
		titleText.text = video.title;
		authorText.text = video.username;
		sizeText.text = MathHelper.FormatBytes(video.downloadsize);
		lengthText.text = MathHelper.FormatSeconds(video.length);
		timestampText.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		uuid = video.uuid;
		isLocal = local;

		//TODO(Kristof): Prevent being able to open it without VR (will be fixed if we use raycasts from mouse instead of mouse events)
		if (!video.compatibleVersion)
		{
			gameObject.GetComponent<Hittable>().enabled = false;
			error.transform.parent.gameObject.SetActive(true);
			error.GetComponent<Text>().text =
				string.Format("This project uses a version that's higher than the player's. Please update the player. [{0}]",
					uuid);
		}
		else
		{
			gameObject.GetComponent<Hittable>().enabled = true;
			error.transform.parent.gameObject.SetActive(false);
		}

		if (isLocal)
		{
			imageDownload = new WWW("file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(video.uuid, SaveFile.thumbFilename)));
		}
		else
		{
			imageDownload = new WWW(Web.thumbnailUrl + "/" + uuid);
		}

		Refresh();
	}

	public void Refresh()
	{
		DownloadedText.enabled = Directory.Exists(Path.Combine(Application.persistentDataPath, uuid));
	}

	public void Update()
	{
		if (imageDownload != null)
		{
			if (imageDownload.error != null)
			{
				Debug.Log("Failed to download thumbnail: " + imageDownload.error);
				imageDownload.Dispose();
				imageDownload = null;
			}
			else if (imageDownload.isDone)
			{
				thumbnailImage.sprite = Sprite.Create(imageDownload.texture, new Rect(0, 0, imageDownload.texture.width, imageDownload.texture.height), new Vector2(0.5f, 0.5f));
				imageDownload.Dispose();
				imageDownload = null;
				thumbnailImage.color = Color.white;
			}
		}
	}

	public void OnHit()
	{
		var indexPanel = Canvass.main.GetComponentInChildren<IndexPanel>();
		if (indexPanel != null)
		{
			indexPanel.answered = true;
			indexPanel.answerVideoId = uuid;
		}

		var canvas = transform.root.GetComponentInChildren<Canvas>().transform;
		StartCoroutine(Player.FadevideoCanvasOut(canvas));
	}

	public void OnHoverStart()
	{
		GetComponent<Image>().color = new Color(0, 0, 0, 0.3f);
	}

	public void OnHoverExit()
	{
		GetComponent<Image>().color = new Color(0, 0, 0, 0f);
	}
}
