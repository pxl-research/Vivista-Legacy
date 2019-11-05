using System.Collections;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
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
	
	private UnityWebRequest imageDownload;
	private GameObject indexPanel;
	private float time;
	private const float refreshTime = 1.0f;

	void Update()
	{
		time += Time.deltaTime;
		if (time > refreshTime)
		{
			Refresh();
			time = 0;
		}
	}

	public IEnumerator Init(VideoSerialize videoToDownload, GameObject indexPanel, bool isLocal)
	{
		this.indexPanel = indexPanel;
		//NOTE(Simon): Move offscreen. We can't disable it just yet. It's still running _this_ coroutine. Disable at end of function
		var indexPanelPos = this.indexPanel.transform.localPosition;
		this.indexPanel.transform.localPosition = new Vector2(10000, 10000);

		video = videoToDownload;

		videoLength.text = MathHelper.FormatSeconds(video.length);
		title.text = video.title;
		description.text = video.description;
		author.text = video.username;
		timestamp.text = MathHelper.FormatTimestampToTimeAgo(video.realTimestamp);
		downloadSize.text = MathHelper.FormatBytes(video.downloadsize);

		if (isLocal)
		{
			imageDownload = UnityWebRequest.Get("file:///" + Path.Combine(Application.persistentDataPath, video.id, SaveFile.thumbFilename));
		}
		else
		{
			imageDownload = UnityWebRequest.Get(Web.thumbnailUrl + "/" + video.id);
		}

		yield return imageDownload.SendWebRequest();

		if (imageDownload.isNetworkError)
		{
			Debug.Log("Failed to download thumbnail: " + imageDownload.error);
			imageDownload.Dispose();
			imageDownload = null;
		}
		else if (imageDownload.isDone || imageDownload.downloadProgress >= 1f)
		{
			var texture = new Texture2D(1, 1);
			texture.LoadImage(imageDownload.downloadHandler.data);
			thumb.sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f));
			imageDownload.Dispose();
			imageDownload = null;
			thumb.color = Color.white;
		}

		this.indexPanel.SetActive(false);
		this.indexPanel.transform.localPosition = indexPanelPos;
		Refresh();
	}
	
	public void Refresh()
	{
		bool downloaded = Directory.Exists(Path.Combine(Application.persistentDataPath, video.id));
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
		answerVideoId = video.id;
		indexPanel.SetActive(true);
	}

	public void Delete()
	{
		string path = Path.Combine(Application.persistentDataPath, video.id);
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
