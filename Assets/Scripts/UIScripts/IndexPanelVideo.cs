using System.IO;
using UnityEngine;
using UnityEngine.UI;

public class IndexPanelVideo : MonoBehaviour 
{
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
}
