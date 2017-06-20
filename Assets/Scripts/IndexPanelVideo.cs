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

	public void SetData(VideoSerialize video)
	{
		titleText.text = video.title;
		authorText.text = video.username;
		sizeText.text = MathHelper.FormatBytes(video.downloadsize);
		lengthText.text = MathHelper.FormatSeconds(video.length);
		timestampText.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);

		imageDownload = new WWW(Web.thumbnailUrl + "/" + video.uuid);
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

}
