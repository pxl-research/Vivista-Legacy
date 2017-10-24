using UnityEngine;
using UnityEngine.UI;
using System.Text;
using System;

public class DetailPanel : MonoBehaviour
{
	public bool shouldClose;

	public Text videoLength;
	public Image thumb;
	public Text title;
	public Text description;
	public Text author;
	public Text timestamp;
	public Text downloadSize;
	public VideoSerialize video;
	
	private WWW imageDownload;

	void Update()
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
				thumb.sprite = Sprite.Create(imageDownload.texture, new Rect(0, 0, imageDownload.texture.width, imageDownload.texture.height), new Vector2(0.5f, 0.5f));
				imageDownload.Dispose();
				imageDownload = null;
				thumb.color = Color.white;
			}
		}
	}

	public void Init(VideoSerialize videoToDownload)
	{
		video = videoToDownload;

		videoLength.text = MathHelper.FormatSeconds(video.length);
		title.text = video.title;
		description.text = video.description;
		author.text = video.username;
		timestamp.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		downloadSize.text = MathHelper.FormatBytes(video.downloadsize);

		imageDownload = new WWW(Web.thumbnailUrl + "/" + Encoding.UTF8.GetString(Convert.FromBase64String(video.uuid)) + ".jpg");
	}

	public void Back()
	{
		shouldClose = true;
	}


	public void Download()
	{
		VideoDownloadManager.AddDownload(video);
		Back();
	}
}
