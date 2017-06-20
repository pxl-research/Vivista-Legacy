using UnityEngine;
using UnityEngine.UI;

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
	
	private WWW imageDownload;

	void Update()
	{
		if (imageDownload != null)
		{
			if (imageDownload.isDone)
			{
				thumb.sprite = Sprite.Create(imageDownload.texture, new Rect(0, 0, imageDownload.texture.width, imageDownload.texture.height), new Vector2(0.5f, 0.5f));
				imageDownload.Dispose();
				imageDownload = null;
			}
		}
	}

	public void Init(VideoSerialize video)
	{
		videoLength.text = MathHelper.FormatSeconds(video.length);
		title.text = video.title;
		description.text = video.description;
		author.text = video.username;
		timestamp.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		downloadSize.text = MathHelper.FormatBytes(video.downloadsize);

		imageDownload = new WWW(Web.thumbnailUrl);
	}

	public void Back()
	{
		shouldClose = true;
	}


	public void Download()
	{
		
	}
}
