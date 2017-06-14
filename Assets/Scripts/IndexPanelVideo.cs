using System;
using UnityEngine;
using UnityEngine.UI;

public class IndexPanelVideo : MonoBehaviour 
{
	public Text titleText;
	public Text authorText;
	public Text sizeText;
	public Text lengthText;
	public Text timestampText;
	public Image thumbnailImage;

	private WWW imageDownload;

	public void SetData(string title = "", string author = "", string thumbnailURL = "", int sizeBytes = 0, int lengthSeconds = 0, DateTime timestamp = default(DateTime))
	{
		if (title != "")
		{
			titleText.text = title;
		}
		if (author != "")
		{
			authorText.text = author;
		}
		if (thumbnailURL != "")
		{
			imageDownload = new WWW(Web.thumbnailUrl);
		}
		if (sizeBytes > 0)
		{
			titleText.text = title;
		}
		if (lengthSeconds > 0)
		{
			titleText.text = title;
		}
		if (timestamp != default(DateTime))
		{
			timestampText.text = FormatTimestampToTimeAgo(timestamp);
		}
	}

	public void Update()
	{
		if (imageDownload != null)
		{
			if (imageDownload.isDone)
			{
				thumbnailImage.sprite = Sprite.Create(imageDownload.texture, new Rect(0, 0, imageDownload.texture.width, imageDownload.texture.height), new Vector2(0.5f, 0.5f));
				imageDownload.Dispose();
				imageDownload = null;
			}
		}
	}

	public string FormatTimestampToTimeAgo(DateTime timestamp)
	{
		var elapsed = DateTime.Now - timestamp;
		if (elapsed.Days > 365)
		{
			return String.Format("{0} years ago", elapsed.Days / 365);
		}
		if (elapsed.Days > 31)
		{
			return String.Format("{0} months ago", elapsed.Days / 31);
		}
		if (elapsed.Days > 7)
		{
			return String.Format("{0} weeks ago", elapsed.Days / 7);
		}
		if (elapsed.Days > 1)
		{
			return String.Format("{0} days ago", elapsed.Days);
		}
		if (elapsed.Hours > 1)
		{
			return String.Format("{0} hours ago", elapsed.Hours);
		}
		if (elapsed.Minutes > 1)
		{
			return String.Format("{0} minutes ago", elapsed.Minutes);
		}
	
		return "Just now";
		
	}
}
