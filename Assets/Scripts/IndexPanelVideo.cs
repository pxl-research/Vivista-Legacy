using System;
using System.IO;
using System.Text;
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

	public void SetData(VideoSerialize video)
	{
		titleText.text = video.title;
		authorText.text = video.username;
		sizeText.text = MathHelper.FormatBytes(video.downloadsize);
		lengthText.text = MathHelper.FormatSeconds(video.length);
		timestampText.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		uuid = video.uuid;

		imageDownload = new WWW(Web.thumbnailUrl + "/" + Encoding.UTF8.GetString(Convert.FromBase64String(uuid)) + ".jpg");
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
