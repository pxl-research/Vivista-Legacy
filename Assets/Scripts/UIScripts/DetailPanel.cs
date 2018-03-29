using System.IO;
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
	public VideoSerialize video;
	public Button playButton;
	public Button downloadButton;
	public Button deleteButton;

	public bool answered;
	public string answerVideoId;
	
	private WWW imageDownload;
	private GameObject indexPanel;
	private float time;
	private const float refreshTime = 1.0f;

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

		time += Time.deltaTime;
		if (time > refreshTime)
		{
			Refresh();
			time = 0;
		}
	}

	public void Init(VideoSerialize videoToDownload, GameObject indexPanel, bool isLocal)
	{
		this.indexPanel = indexPanel;
		this.indexPanel.SetActive(false);

		video = videoToDownload;

		videoLength.text = MathHelper.FormatSeconds(video.length);
		title.text = video.title;
		description.text = video.description;
		author.text = video.username;
		timestamp.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		downloadSize.text = MathHelper.FormatBytes(video.downloadsize);
		if (isLocal)
		{
			imageDownload = new WWW("file:///" + Path.Combine(Application.persistentDataPath, Path.Combine(video.uuid, SaveFile.thumbFilename)));
		}
		else
		{
			imageDownload = new WWW(Web.thumbnailUrl + "/" + video.uuid);
		}

		Refresh();
	}
	
	public void Refresh()
	{
		bool downloaded = Directory.Exists(Path.Combine(Application.persistentDataPath, video.uuid));
		downloadButton.gameObject.SetActive(!downloaded);
		playButton.gameObject.SetActive(downloaded);
		deleteButton.gameObject.SetActive(downloaded);
	}

	public void Back()
	{
		indexPanel.SetActive(true);
		shouldClose = true;
	}

	public void Play()
	{
		answered = true;
		answerVideoId = video.uuid;
		indexPanel.SetActive(true);
	}

	public void Delete()
	{
		string path = Path.Combine(Application.persistentDataPath, video.uuid);
		bool downloaded = Directory.Exists(path);
		if (downloaded)
		{
			Directory.Delete(path, true);
		}
		Refresh();
	}

	public void Download()
	{
		VideoDownloadManager.Main.AddDownload(video);
	}
}
